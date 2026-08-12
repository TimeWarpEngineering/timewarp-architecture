#region Purpose
// Server-side handler for the CompletePasskeyRegistration command: verifies the browser's
// attestation response and mints a Principal + Credential on success.
#endregion

#region Design
// RP-ID selection (task 104-031) runs FIRST, before the ceremony: the relying party is chosen per
// request from the request host via WebAuthnRelyingPartySelection.Select, so a host outside the
// allowlist returns 400 "Host not allowed" without consuming (and wasting) the ceremony's challenge.
// Ceremony preamble (decode → consume → verify → handle-exists) lives in PasskeyRegistrationCeremony;
// ordering / replay-safety rationale is owned there. This handler's post-verify path:
//   Principal.Create → AddPrincipalAsync → Credential.Create → AddCredentialAsync (try/catch race)
//   → TryClaimFirstAdministratorAsync (empty deployment: first human passkey becomes admin)
//   → BrowserSessionService.IssueAsync.
// First admin: product rule — when no stored Administrator exists yet, this create claims
// Administrator+Member via IPrincipalRoleStore. Atomic at the store (in-mem lock / EF Serializable).
// No kill-switch: empty DB is not protected value; redeploy if a stray first create happened.
// Later passkey creates stay default effective Member. Agent-key registration does NOT claim.
// BootstrapAdministratorPrincipalIds remains break-glass only.
// FindCredentialByHandleAsync (in the ceremony) runs BEFORE Principal.Create/AddPrincipalAsync, so
// the sequential duplicate-handle case never leaves an orphan Principal: it 409s before either Add
// call. Residual race: two concurrent ceremonies for the SAME credential handle (distinct challenges,
// so one-time challenge consume does not prevent it) can both pass Find, both AddPrincipalAsync, and
// race on AddCredentialAsync — the store enforces handle uniqueness atomically there (throwing
// InvalidOperationException on collision). That throw is caught and translated to the same 409
// CredentialAlreadyRegistered the sequential path returns. What is NOT closed: the loser's
// already-persisted Principal from AddPrincipalAsync is NOT compensated/removed — IPrincipalStore
// has no delete method (removal is out of this task's scope; see 104-005). That orphan is an inert
// Provisional-tier principal with zero credentials — it cannot authenticate and is otherwise
// harmless until 104-005's store lifecycle work can add removal.
// Account resolution is credential-handle-based, never by the WebAuthn user.id/userHandle minted in
// StartPasskeyRegistration — that handle is opaque and discarded; the Principal is minted HERE.
// Concurrency note (104-028): zero Update* calls. AddCredentialAsync's first-credential rule
// auto-promotes the STORED principal Provisional -> Keyed; this handler's in-hand `principal` local
// is deliberately left stale afterward.
#endregion

namespace TimeWarp.Architecture.Features.Identity.Application;

using static TimeWarp.Architecture.Features.Identity.CompletePasskeyRegistration;

public sealed partial class CompletePasskeyRegistration
{
  public class Handler : IRequestHandler<Command, OneOf<Response, SharedProblemDetails>>
  {
    private readonly IPrincipalStore PrincipalStore;
    private readonly IPrincipalRoleStore PrincipalRoleStore;
    private readonly IWebAuthnChallengeStore ChallengeStore;
    private readonly IBrowserSessionService BrowserSessionService;
    private readonly IRequestHostAccessor RequestHostAccessor;
    private readonly IOptions<WebAuthnOptions> Options;

    public Handler
    (
      IPrincipalStore principalStore,
      IPrincipalRoleStore principalRoleStore,
      IWebAuthnChallengeStore challengeStore,
      IBrowserSessionService browserSessionService,
      IRequestHostAccessor requestHostAccessor,
      IOptions<WebAuthnOptions> options
    )
    {
      PrincipalStore = principalStore;
      PrincipalRoleStore = principalRoleStore;
      ChallengeStore = challengeStore;
      BrowserSessionService = browserSessionService;
      RequestHostAccessor = requestHostAccessor;
      Options = options;
    }

    public async Task<OneOf<Response, SharedProblemDetails>> Handle(Command command, CancellationToken cancellationToken)
    {
      // Select the RP ID FIRST — before decode/consume — so a disallowed host never burns a
      // challenge (task 104-031).
      OneOf<WebAuthnRelyingParty, SharedProblemDetails> relyingPartyResult =
        WebAuthnRelyingPartySelection.Select(RequestHostAccessor.GetRequestHost(), Options.Value);
      if (relyingPartyResult.IsT1)
      {
        return relyingPartyResult.AsT1;
      }

      WebAuthnRelyingParty relyingParty = relyingPartyResult.AsT0;

      OneOf<PasskeyRegistrationCeremony.Materials, SharedProblemDetails> ceremonyResult =
        await PasskeyRegistrationCeremony.TryCompleteAsync(
          command.CredentialId,
          command.ClientDataJson,
          command.AttestationObject,
          relyingParty,
          ChallengeStore,
          PrincipalStore,
          cancellationToken);
      if (ceremonyResult.IsT1)
      {
        return ceremonyResult.AsT1;
      }

      PasskeyRegistrationCeremony.Materials materials = ceremonyResult.AsT0;

      var principal = Principal.Create(PrincipalKind.Human);
      await PrincipalStore.AddPrincipalAsync(principal, cancellationToken);

      // Label from AAGUID → provider map (task 168) so Settings shows "Proton Pass" / "1Password"
      // like passkeys.io — not a free-form user name.
      var credential = Credential.Create(
        principal.Id,
        CredentialType.Passkey,
        materials.CredentialId,
        materials.CosePublicKey,
        materials.ProviderLabel);
      try
      {
        await PrincipalStore.AddCredentialAsync(credential, cancellationToken);
      }
      catch (InvalidOperationException)
      {
        // Lost a concurrent race for this credential handle against another ceremony — see Design
        // region. The just-added Principal is left as an orphan (no delete capability exists yet);
        // report the same 409 the sequential check-then-act path returns.
        return IdentityProblems.CredentialAlreadyRegistered("passkey");
      }

      // Empty deployment: first successful human passkey create claims Administrator.
      // Claims transform on the next request (and IssueAsync session) sees store-backed roles.
      _ = await PrincipalRoleStore.TryClaimFirstAdministratorAsync(principal.Id, cancellationToken);

      await BrowserSessionService.IssueAsync(principal.Id, displayName: null, cancellationToken);

      return new Response(principal.Id);
    }
  }
}

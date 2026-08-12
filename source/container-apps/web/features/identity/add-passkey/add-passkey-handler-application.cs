#region Purpose
// Server-side handler for the AddPasskey command: verifies the browser's attestation response and
// attaches a new Credential to the CALLER's EXISTING principal (never mints a new one).
#endregion

#region Design
// Auth first (ICurrentPrincipalAccessor) before RP select and before PasskeyRegistrationCeremony —
// an unauthenticated caller must never burn a challenge. RP select next (task 104-031) so a
// disallowed host never burns a challenge either. Ceremony preamble (decode → consume → verify →
// handle-exists) lives in PasskeyRegistrationCeremony; ordering rationale is owned there.
// Differs from CompletePasskeyRegistration: principal id comes from the authenticated caller
// (never Principal.Create), and there is no BrowserSessionService.IssueAsync — the caller already
// HAS a session; minting a new one would silently replace the session's identity claim.
// The orphan-Principal residual on CompletePasskeyRegistration does NOT apply here: this handler
// never calls AddPrincipalAsync, so a lost same-handle race has nothing left over to compensate —
// the InvalidOperationException catch maps straight to 409.
// Response 409 does not disclose whether a colliding passkey belongs to the caller's principal or
// someone else's. Zero Update* calls (Add* only) — no concurrency retry loop.
// Round-1 M5: same WebAuthnCeremonyType.Registration as Complete/Start (see ceremony Design).
#endregion

namespace TimeWarp.Architecture.Features.Identity.Application;

using static TimeWarp.Architecture.Features.Identity.AddPasskey;

public sealed partial class AddPasskey
{
  public class Handler : IRequestHandler<Command, OneOf<Response, SharedProblemDetails>>
  {
    private readonly IPrincipalStore PrincipalStore;
    private readonly IWebAuthnChallengeStore ChallengeStore;
    private readonly ICurrentPrincipalAccessor CurrentPrincipalAccessor;
    private readonly IRequestHostAccessor RequestHostAccessor;
    private readonly IOptions<WebAuthnOptions> Options;

    public Handler
    (
      IPrincipalStore principalStore,
      IWebAuthnChallengeStore challengeStore,
      ICurrentPrincipalAccessor currentPrincipalAccessor,
      IRequestHostAccessor requestHostAccessor,
      IOptions<WebAuthnOptions> options
    )
    {
      PrincipalStore = principalStore;
      ChallengeStore = challengeStore;
      CurrentPrincipalAccessor = currentPrincipalAccessor;
      RequestHostAccessor = requestHostAccessor;
      Options = options;
    }

    public async Task<OneOf<Response, SharedProblemDetails>> Handle(Command command, CancellationToken cancellationToken)
    {
      PrincipalId? callerId = await CurrentPrincipalAccessor.GetCurrentPrincipalIdAsync(cancellationToken);
      if (callerId is null)
      {
        return IdentityProblems.Unauthenticated();
      }

      // Select the RP ID before touching any ceremony state — a disallowed host never burns a
      // challenge (task 104-031). Runs after the auth guard so an unauthenticated caller still 401s
      // first (preserving the auth-first invariant this handler's Design region relies on).
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

      // Prefer caller-supplied Label; else AAGUID provider name (task 168).
      string? label = string.IsNullOrWhiteSpace(command.Label) ? materials.ProviderLabel : command.Label;
      var credential = Credential.Create(callerId.Value, CredentialType.Passkey, materials.CredentialId, materials.CosePublicKey, label);
      try
      {
        await PrincipalStore.AddCredentialAsync(credential, cancellationToken);
      }
      catch (InvalidOperationException)
      {
        // Lost a concurrent race for this credential handle — see Design region. Unlike
        // CompletePasskeyRegistration.Handler, there is no orphan Principal to worry about: the
        // caller's principal already existed before this call.
        return IdentityProblems.CredentialAlreadyRegistered("passkey");
      }

      return new Response(credential.Id);
    }
  }
}

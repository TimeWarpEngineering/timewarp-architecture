#region Purpose
// Server-side handler for the CompletePasskeyRegistration command: verifies the browser's
// attestation response and mints a Principal + Credential on success.
#endregion

#region Design
// RP-ID selection (task 104-031) runs FIRST, before decode/consume: the relying party is chosen per
// request from the request host via WebAuthnRelyingPartySelection.Select, so a host outside the
// allowlist returns 400 "Host not allowed" without consuming (and wasting) the ceremony's challenge.
// The selected relying party (its Id being the request host, canonical casing) is what
// WebAuthnRegistration.Verify then binds rpIdHash and origin against.
// Order is otherwise deliberate and replay-safety-critical: decode -> consume the challenge ->
// verify -> check for an existing credential -> mint. The challenge is consumed (one-time) BEFORE
// WebAuthnRegistration.Verify runs, not after — even a payload that later fails verification has
// already burned its challenge, so a retry with a corrected payload must start a brand-new ceremony
// (StartPasskeyRegistration) rather than resubmitting a tampered version of an already-answered one.
// FindCredentialByHandleAsync runs BEFORE Principal.Create/AddPrincipalAsync, so the sequential
// duplicate-handle case (the common one — resubmitting an already-registered credential) never
// leaves an orphan Principal: it 409s before either Add call. A round-1 review caught the residual
// race this check-then-act does NOT close: two concurrent ceremonies for the SAME credential handle
// (distinct challenges, so the one-time challenge consume does not prevent it) can both pass the
// Find check, both call AddPrincipalAsync, and then race on AddCredentialAsync — the store enforces
// handle uniqueness atomically there (InMemoryPrincipalStore.HandleIndex.TryAdd under WriteLock,
// throwing InvalidOperationException on collision), so the loser's AddCredentialAsync throws. That
// throw is now caught below and translated to the same 409 CredentialAlreadyRegistered() the
// sequential path returns, so callers never see a raw 500. What is NOT closed: the loser's
// already-persisted Principal from AddPrincipalAsync is NOT compensated/removed — IPrincipalStore
// has no delete method (removal is out of this task's scope; see 104-005). That orphan is an inert
// Provisional-tier principal with zero credentials — it cannot authenticate (no credential exists to
// present) and is otherwise harmless, just a stray store row until 104-005's store lifecycle work
// can add removal.
// Account resolution is credential-handle-based (FindCredentialByHandleAsync), never by the
// WebAuthn user.id/userHandle minted in StartPasskeyRegistration's options — that handle is opaque
// and discarded once the ceremony completes; the Principal is minted HERE, at verify time, so an
// abandoned ceremony (browser never completes, or completion fails) creates nothing.
// Concurrency note (104-028): this handler makes zero Update* calls. AddPrincipalAsync/
// AddCredentialAsync are both Add* (no version check); AddCredentialAsync's first-credential rule
// auto-promotes the STORED principal Provisional -> Keyed internally — this handler's in-hand
// `principal` local is deliberately left stale afterward (snapshot-on-get semantics mean nothing
// here observes or needs the promoted tier). The first real catch-ConcurrencyConflictException-
// reload-retry-once policy lands with 104-005's revoke handler, not here.
#endregion

namespace TimeWarp.Architecture.Features.Identity.Application;

using static TimeWarp.Architecture.Features.Identity.CompletePasskeyRegistration;

public sealed partial class CompletePasskeyRegistration
{
  public class Handler : IRequestHandler<Command, OneOf<Response, SharedProblemDetails>>
  {
    private readonly IPrincipalStore PrincipalStore;
    private readonly IWebAuthnChallengeStore ChallengeStore;
    private readonly IBrowserSessionService BrowserSessionService;
    private readonly IRequestHostAccessor RequestHostAccessor;
    private readonly IOptions<WebAuthnOptions> Options;

    public Handler
    (
      IPrincipalStore principalStore,
      IWebAuthnChallengeStore challengeStore,
      IBrowserSessionService browserSessionService,
      IRequestHostAccessor requestHostAccessor,
      IOptions<WebAuthnOptions> options
    )
    {
      PrincipalStore = principalStore;
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

      if (!WebAuthnPayloadDecoder.TryDecode(command.CredentialId, out byte[] credentialIdBytes)
        || !WebAuthnPayloadDecoder.TryDecode(command.ClientDataJson, out byte[] clientDataJsonBytes)
        || !WebAuthnPayloadDecoder.TryDecode(command.AttestationObject, out byte[] attestationObjectBytes))
      {
        return MalformedPayload();
      }

      if (!WebAuthnChallengeReader.TryReadChallenge(clientDataJsonBytes, out byte[] challenge)
        || !ChallengeStore.TryConsume(WebAuthnCeremonyType.Registration, challenge))
      {
        return ChallengeInvalid();
      }

      WebAuthnRegistrationResult verifyResult =
        WebAuthnRegistration.Verify(relyingParty, challenge, clientDataJsonBytes, attestationObjectBytes, credentialIdBytes);

      if (!verifyResult.IsValid)
      {
        return VerificationFailed(verifyResult.FailureReason);
      }

      Credential? existing = await PrincipalStore.FindCredentialByHandleAsync(CredentialType.Passkey, verifyResult.CredentialId, cancellationToken);
      if (existing is not null)
      {
        return CredentialAlreadyRegistered();
      }

      var principal = Principal.Create(PrincipalKind.Human);
      await PrincipalStore.AddPrincipalAsync(principal, cancellationToken);

      var credential = Credential.Create(principal.Id, CredentialType.Passkey, verifyResult.CredentialId, verifyResult.CosePublicKey);
      try
      {
        await PrincipalStore.AddCredentialAsync(credential, cancellationToken);
      }
      catch (InvalidOperationException)
      {
        // Lost a concurrent race for this credential handle against another ceremony — see Design
        // region. The just-added Principal is left as an orphan (no delete capability exists yet);
        // report the same 409 the sequential check-then-act path returns.
        return CredentialAlreadyRegistered();
      }

      await BrowserSessionService.IssueAsync(principal.Id, displayName: null, cancellationToken);

      return new Response(principal.Id);
    }

    private static SharedProblemDetails MalformedPayload() => new()
    {
      Title = "Malformed request",
      Status = 400,
      Detail = "CredentialId, ClientDataJson, and AttestationObject must be valid base64url."
    };

    private static SharedProblemDetails ChallengeInvalid() => new()
    {
      Title = "Challenge invalid",
      Status = 400,
      Detail = "The registration challenge is unknown, expired, or already used."
    };

    private static SharedProblemDetails VerificationFailed(WebAuthnFailureReason reason) => new()
    {
      Title = "Passkey registration verification failed",
      Status = 400,
      Detail = $"Verification failed: {reason}."
    };

    private static SharedProblemDetails CredentialAlreadyRegistered() => new()
    {
      Title = "Credential already registered",
      Status = 409,
      Detail = "This passkey is already registered to an account."
    };
  }
}

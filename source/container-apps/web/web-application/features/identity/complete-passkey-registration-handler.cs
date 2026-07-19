#region Purpose
// Server-side handler for the CompletePasskeyRegistration command: verifies the browser's
// attestation response and mints a Principal + Credential on success.
#endregion

#region Design
// Order is deliberate and replay-safety-critical: decode -> consume the challenge -> verify ->
// check for an existing credential -> mint. The challenge is consumed (one-time) BEFORE
// WebAuthnRegistration.Verify runs, not after — even a payload that later fails verification has
// already burned its challenge, so a retry with a corrected payload must start a brand-new ceremony
// (StartPasskeyRegistration) rather than resubmitting a tampered version of an already-answered one.
// FindCredentialByHandleAsync runs BEFORE Principal.Create/AddPrincipalAsync so a duplicate-handle
// rejection never leaves an orphan Principal with no credential.
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
    private readonly IOptions<WebAuthnOptions> Options;

    public Handler
    (
      IPrincipalStore principalStore,
      IWebAuthnChallengeStore challengeStore,
      IBrowserSessionService browserSessionService,
      IOptions<WebAuthnOptions> options
    )
    {
      PrincipalStore = principalStore;
      ChallengeStore = challengeStore;
      BrowserSessionService = browserSessionService;
      Options = options;
    }

    public async Task<OneOf<Response, SharedProblemDetails>> Handle(Command command, CancellationToken cancellationToken)
    {
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

      WebAuthnOptions webAuthnOptions = Options.Value;
      var relyingParty = new WebAuthnRelyingParty(webAuthnOptions.RpId, webAuthnOptions.RpName, webAuthnOptions.AllowedOrigins);

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
      await PrincipalStore.AddCredentialAsync(credential, cancellationToken);

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

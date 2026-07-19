#region Purpose
// Server-side handler for the CompletePasskeyAuthentication command: verifies the browser's
// assertion response and issues a browser session on success.
#endregion

#region Design
// Order is deliberate and replay-safety-critical, same as CompletePasskeyRegistration: decode ->
// consume the challenge -> look up the credential/principal -> verify. Consuming the challenge
// BEFORE verification means a tampered/replayed payload can never retry the same challenge.
// No-enumeration-oracle posture: an unknown CredentialId and a revoked one both return the SAME
// generic 400 "Authentication failed" — an attacker probing the endpoint cannot distinguish
// "this credential was never registered" from "this credential exists but is revoked." A
// quarantined account (IsActive false) is the one deliberate exception: it returns 403, a distinct
// signal, because at that point the caller has already cryptographically proven possession of a
// real credential — quarantine is an administrative state on an authenticated identity, not a
// "did this credential ever exist" question, so hiding it behind a generic 400 would not close any
// meaningful oracle while making legitimate quarantined users' error messages misleading.
// Concurrency note (104-028): this handler makes zero Update* calls — no sign-count persisted
// (Credential has no such field; see authenticator-data.cs), so nothing here needs to write back to
// the store at all. The first real catch-ConcurrencyConflictException-reload-retry-once policy
// lands with 104-005's revoke handler, not here.
#endregion

namespace TimeWarp.Architecture.Features.Identity.Application;

using static TimeWarp.Architecture.Features.Identity.CompletePasskeyAuthentication;

public sealed partial class CompletePasskeyAuthentication
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
        || !WebAuthnPayloadDecoder.TryDecode(command.AuthenticatorData, out byte[] authenticatorDataBytes)
        || !WebAuthnPayloadDecoder.TryDecode(command.Signature, out byte[] signatureBytes))
      {
        return MalformedPayload();
      }

      if (!WebAuthnChallengeReader.TryReadChallenge(clientDataJsonBytes, out byte[] challenge)
        || !ChallengeStore.TryConsume(WebAuthnCeremonyType.Authentication, challenge))
      {
        return ChallengeInvalid();
      }

      Credential? credential = await PrincipalStore.FindCredentialByHandleAsync(CredentialType.Passkey, credentialIdBytes, cancellationToken);
      if (credential is null || credential.IsRevoked)
      {
        return AuthenticationFailed();
      }

      Principal? principal = await PrincipalStore.GetPrincipalAsync(credential.PrincipalId, cancellationToken);
      if (principal is null)
      {
        return AuthenticationFailed();
      }

      if (!principal.IsActive)
      {
        return Quarantined();
      }

      WebAuthnOptions webAuthnOptions = Options.Value;
      var relyingParty = new WebAuthnRelyingParty(webAuthnOptions.RpId, webAuthnOptions.RpName, webAuthnOptions.AllowedOrigins);

      WebAuthnAssertionResult verifyResult =
        WebAuthnAuthentication.Verify(relyingParty, challenge, credential.PublicMaterial, clientDataJsonBytes, authenticatorDataBytes, signatureBytes);

      if (!verifyResult.IsValid)
      {
        return AuthenticationFailed();
      }

      await BrowserSessionService.IssueAsync(principal.Id, principal.DisplayName, cancellationToken);

      return new Response(principal.Id);
    }

    private static SharedProblemDetails MalformedPayload() => new()
    {
      Title = "Malformed request",
      Status = 400,
      Detail = "CredentialId, ClientDataJson, AuthenticatorData, and Signature must be valid base64url."
    };

    private static SharedProblemDetails ChallengeInvalid() => new()
    {
      Title = "Challenge invalid",
      Status = 400,
      Detail = "The authentication challenge is unknown, expired, or already used."
    };

    private static SharedProblemDetails AuthenticationFailed() => new()
    {
      Title = "Authentication failed",
      Status = 400,
      Detail = "The passkey could not be verified."
    };

    private static SharedProblemDetails Quarantined() => new()
    {
      Title = "Account quarantined",
      Status = 403,
      Detail = "This account is currently restricted."
    };
  }
}

#region Purpose
// Server-side handler for the AddPasskey command: verifies the browser's attestation response and
// attaches a new Credential to the CALLER's EXISTING principal (never mints a new one).
#endregion

#region Design
// Order mirrors CompletePasskeyRegistration.Handler exactly (decode -> consume the challenge ->
// verify -> check for an existing credential -> attach), with two differences: the principal id comes
// from ICurrentPrincipalAccessor (resolved FIRST, before touching any ceremony state — an
// unauthenticated caller should never even burn a challenge) instead of Principal.Create, and there
// is no BrowserSessionService.IssueAsync call — the caller already HAS a session (that is how they
// got here); minting a new one would be pointless and, worse, would silently replace the session's
// identity claim (the request's own session should not change as a side effect of adding a second
// credential to it).
// RP-ID selection (task 104-031): the relying party is chosen per request from the request host via
// WebAuthnRelyingPartySelection.Select, run after the auth guard but still BEFORE any challenge
// consume (a disallowed host never burns a challenge). Auth stays first so an unauthenticated caller
// 401s regardless of host; a host outside the allowlist then returns 400 "Host not allowed".
// The orphan-Principal residual documented on CompletePasskeyRegistration.Handler's Design region
// does NOT apply here: this handler never calls AddPrincipalAsync (the principal already exists,
// proven by the caller being authenticated), so there is no principal to orphan if AddCredentialAsync
// loses a same-handle race — the InvalidOperationException catch below translates that race straight
// to 409 with nothing left over to compensate.
// FindCredentialByHandleAsync runs BEFORE Credential.Create/AddCredentialAsync for the same
// sequential-duplicate reason as CompletePasskeyRegistration.Handler; the response is the SAME 409
// regardless of which principal (if any) already owns the matching handle — this endpoint does not
// disclose whether a colliding passkey belongs to the caller's own principal or someone else's.
// Zero Update* calls (Add* only) — no concurrency retry loop needed here, unlike RevokeCredential.
// Round-1 review (M5, security-confirmed no risk): this handler consumes the SAME
// WebAuthnCeremonyType.Registration challenge type StartPasskeyRegistration/
// CompletePasskeyRegistration use, with no separate "add" ceremony type. This is safe because the
// challenge is an intent-agnostic one-time liveness proof — it only proves "a real authenticator
// answered a fresh server-issued nonce," nothing about WHOSE principal the resulting credential
// should attach to. The new-principal-vs-add-to-current-principal distinction is enforced entirely
// by this endpoint's own [EndpointAuthorize(Policy="credential-management")] boundary (only a
// caller who is ALREADY authenticated can reach this handler at all) and by sourcing the target
// principal id from ICurrentPrincipalAccessor rather than the ceremony — never by which challenge
// type was consumed. A confused-deputy substitution (tricking this handler into treating an "add"
// ceremony as a "register new principal" one, or vice versa) is therefore not possible: there is no
// registration-only capability the challenge type itself grants.
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
        return Unauthenticated();
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

      var credential = Credential.Create(callerId.Value, CredentialType.Passkey, verifyResult.CredentialId, verifyResult.CosePublicKey, command.Label);
      try
      {
        await PrincipalStore.AddCredentialAsync(credential, cancellationToken);
      }
      catch (InvalidOperationException)
      {
        // Lost a concurrent race for this credential handle — see Design region. Unlike
        // CompletePasskeyRegistration.Handler, there is no orphan Principal to worry about: the
        // caller's principal already existed before this call.
        return CredentialAlreadyRegistered();
      }

      return new Response(credential.Id);
    }

    private static SharedProblemDetails Unauthenticated() => new()
    {
      Title = "Unauthenticated",
      Status = 401,
      Detail = "No authenticated principal."
    };

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

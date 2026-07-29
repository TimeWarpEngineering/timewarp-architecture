#region Purpose
// Shared SharedProblemDetails factories for identity application handlers — pure problem data only.
#endregion

#region Design
// Extracted from private static factories duplicated across identity handlers (task 131-002 /
// parent 131 F-006). Title/Status/Detail strings are copied VERBATIM from the prior per-handler
// factories — no wording "improvements." Parameterized only where intentional variants already
// existed (MalformedPayload field list, ChallengeInvalid ceremony label, CredentialAlreadyRegistered
// credential kind, VerificationFailed title prefix + reason). Unique single-consumer problems
// (AuthenticationFailed, IssuanceFailed, revoke set, GetAgentIdentity Unauthorized) live here too
// so every identity handler returns problems from one place.
// internal static: slice-local; not a public API surface for other product slices (TWA0009).
#endregion

namespace TimeWarp.Architecture.Features.Identity.Application;

internal static class IdentityProblems
{
  public static SharedProblemDetails Unauthenticated() => new()
  {
    Title = "Unauthenticated",
    Status = 401,
    Detail = "No authenticated principal."
  };

  public static SharedProblemDetails Unauthorized() => new()
  {
    Title = "Unauthorized",
    Status = 401,
    Detail = "A valid agent bearer token is required."
  };

  public static SharedProblemDetails MalformedPayload(string fields) => new()
  {
    Title = "Malformed request",
    Status = 400,
    Detail = $"{fields} must be valid base64url."
  };

  public static SharedProblemDetails ChallengeInvalid(string ceremonyLabel) => new()
  {
    Title = "Challenge invalid",
    Status = 400,
    Detail = $"The {ceremonyLabel} challenge is unknown, expired, or already used."
  };

  public static SharedProblemDetails CredentialAlreadyRegistered(string kind) => new()
  {
    Title = "Credential already registered",
    Status = 409,
    Detail = $"This {kind} is already registered to an account."
  };

  public static SharedProblemDetails PasskeyRegistrationVerificationFailed(WebAuthnFailureReason reason) => new()
  {
    Title = "Passkey registration verification failed",
    Status = 400,
    Detail = $"Verification failed: {reason}."
  };

  public static SharedProblemDetails AgentKeyRegistrationVerificationFailed(AgentKeyFailureReason reason) => new()
  {
    Title = "Agent key registration verification failed",
    Status = 400,
    Detail = $"Verification failed: {reason}."
  };

  public static SharedProblemDetails InvalidPublicKey() => new()
  {
    Title = "Invalid public key",
    Status = 400,
    Detail = "PublicKey must be a well-formed ECDSA P-256 SubjectPublicKeyInfo (DER)."
  };

  public static SharedProblemDetails AuthenticationFailed() => new()
  {
    Title = "Authentication failed",
    Status = 400,
    Detail = "The passkey could not be verified."
  };

  public static SharedProblemDetails Quarantined() => new()
  {
    Title = "Account quarantined",
    Status = 403,
    Detail = "This account is currently restricted."
  };

  public static SharedProblemDetails InvalidScope(IReadOnlyCollection<string> unknownScopes) => new()
  {
    Title = "invalid_scope",
    Status = 400,
    Detail = $"Unknown scope(s): {string.Join(", ", unknownScopes)}."
  };

  public static SharedProblemDetails IssuanceFailed() => new()
  {
    Title = "Token issuance failed",
    Status = 400,
    Detail = "The agent key could not be verified."
  };

  public static SharedProblemDetails NotFound() => new()
  {
    Title = "Credential not found",
    Status = 404,
    Detail = "No such credential."
  };

  public static SharedProblemDetails AlreadyRevoked() => new()
  {
    Title = "Credential already revoked",
    Status = 409,
    Detail = "This credential has already been revoked."
  };

  public static SharedProblemDetails LastCredential() => new()
  {
    Title = "Cannot revoke last credential",
    Status = 409,
    Detail = "Revoking this credential would leave the account with no way to authenticate."
  };

  public static SharedProblemDetails TooMuchContention() => new()
  {
    Title = "Too much contention",
    Status = 409,
    Detail = "The credential could not be revoked due to concurrent updates. Try again."
  };
}

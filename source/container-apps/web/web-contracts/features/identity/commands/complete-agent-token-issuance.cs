#region Purpose
// Endpoint-centric contract for completing an agent access-token issuance ceremony: the agent's
// answer to StartAgentTokenIssuance's challenge, naming which registered key it is proving
// possession of and which scopes it wants on the resulting token.
#endregion

#region Design
// KeyId (base64url SHA-256 of the registered SPKI DER, echoed back from
// CompleteAgentKeyRegistration.Response) is how the server finds the credential — account
// resolution here is credential-handle-based, mirroring CompletePasskeyAuthentication's posture:
// this template never persists a separate "who is asking" identifier, the KeyId IS the lookup key
// (IPrincipalStore.FindCredentialByHandleAsync(CredentialType.AgentKey, ...)).
// Scopes are the CALLER'S request for what the issued token should be able to do — validated against
// AgentScopes.IsKnown by the handler (unknown scope -> 400 invalid_scope, listing the offending
// entries) — this contract's own Validator only enforces coarse shape (non-empty list, a small count
// ceiling, and a length ceiling per entry), not membership in the known-scope set: format validation
// belongs here, semantic/domain validation (is this a real scope) belongs in the handler/library
// (matches the passkey contracts' documented split for base64url well-formedness vs the handler's
// structural/crypto checks).
// No refresh token: refresh IS re-running this ceremony with a fresh challenge and the same key —
// see IAgentTokenStore's Design region.
// Every field carries a MaximumLength ceiling from day one (see CompleteAgentKeyRegistration's
// Design region for the round-1-lesson rationale): KeyId 256 (a base64url SHA-256 digest is ~43
// chars; 256 is generous headroom, matching StartAgentKeyRegistration's Challenge cap), Challenge
// 256, Signature 1KB, Scopes capped at 16 entries of at most 64 characters each (this task defines
// exactly two known scopes — see AgentScopes — so 16 is deliberately generous headroom for future
// scopes, not a tight bound).
// No GetMockResponseFactory — see StartAgentKeyRegistration's Design region.
#endregion

namespace TimeWarp.Architecture.Features.Identity;

public static partial class CompleteAgentTokenIssuance
{
  [ApiRoute("api/identity/agent/token", HttpVerb.Post)]
  public sealed partial class Command : IApiRequest, IRequest<OneOf<Response, SharedProblemDetails>>
  {
    public string KeyId { get; set; } = null!;
    public string Challenge { get; set; } = null!;
    public string Signature { get; set; } = null!;
    public List<string> Scopes { get; set; } = [];
  }

  public sealed class Validator : AbstractValidator<Command>
  {
    public Validator()
    {
      RuleFor(x => x.KeyId).NotEmpty().MaximumLength(256);
      RuleFor(x => x.Challenge).NotEmpty().MaximumLength(256);
      RuleFor(x => x.Signature).NotEmpty().MaximumLength(1024);
      RuleFor(x => x.Scopes).NotEmpty().Must(scopes => scopes.Count <= 16)
        .WithMessage("Scopes must contain between 1 and 16 entries.");
      RuleForEach(x => x.Scopes).NotEmpty().MaximumLength(64);
    }
  }

  public sealed class Response
  {
    public string AccessToken { get; }
    public string TokenType { get; }
    public int ExpiresInSeconds { get; }
    public IReadOnlyList<string> Scopes { get; }
    public PrincipalId PrincipalId { get; }

    public Response(string accessToken, int expiresInSeconds, IReadOnlyList<string> scopes, PrincipalId principalId)
    {
      AccessToken = Guard.Against.NullOrEmpty(accessToken);
      TokenType = "Bearer";
      ExpiresInSeconds = Guard.Against.NegativeOrZero(expiresInSeconds);
      Scopes = Guard.Against.Null(scopes);

      if (principalId.IsEmpty)
      {
        throw new ArgumentException("PrincipalId cannot be empty.", nameof(principalId));
      }

      PrincipalId = principalId;
    }
  }
}

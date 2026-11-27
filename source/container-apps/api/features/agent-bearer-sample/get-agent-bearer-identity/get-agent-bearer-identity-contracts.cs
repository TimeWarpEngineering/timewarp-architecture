#region Purpose
// Protected sample contract: authenticated agent reads its principal id, kind, trust tier, and
// token scopes on api-server — proves agent bearer validation + PascalCase string-enum wire.
#endregion

#region Design
// Teaching/capability sample (task 104-030), NOT a dual of web's GET api/identity/agent/me.
// Ceremonies (register key, issue token) stay on web-server; this route only needs a valid
// bearer already present in THIS host's IAgentTokenStore (in-memory = host-local until Redis).
// Response carries PrincipalKind + TrustTier so the first enum-bearing api-server FastEndpoint
// can assert task 108's string-enum shape through FE (not integers).
// Route deliberately under api/agent/bearer/* so host-split docs can list web vs api surfaces
// without path collision.
// No GetMockResponseFactory — SPA mock mode does not exercise this api-server sample.
// AuthenticationSchemes = agent-token (task 161): api-server's IdentityReadPolicy already lists
// that scheme via AddAuthenticationSchemes, which Combine copies onto the FastEndpoints
// Policies-only path, so this declaration is redundant with the policy today. It is still set
// so a future policy-registration change cannot silently drop the only non-default scheme
// (web permission policies already have no scheme list — see AuthenticationSchemeNames).
#endregion

namespace TimeWarp.Architecture.Features.AgentBearerSamples;

using TimeWarp.Identity;

[ApiEndpoint]
[EndpointAuthorize
(
  Policy = "agent-scope:identity:read", // matches AgentTokenDefaults.IdentityReadPolicy
  AuthenticationSchemes = "agent-token" // matches AgentTokenDefaults.Scheme — task 161
)]
public static partial class GetAgentBearerIdentity
{
  [ApiRoute("api/agent/bearer/me", HttpVerb.Get)]
  [Operation("agent-bearer-identity.get", Description = "Read the calling agent's principal id, kind, trust tier, and token scopes.", Group = "agent-bearer-sample")]
  public sealed partial class Query : IApiRequest, IQuery<OneOf<Response, SharedProblemDetails>>;

  public sealed class Validator : AbstractValidator<Query>;

  public sealed class Response
  {
    /// <summary>Id of the principal.</summary>
    public PrincipalId PrincipalId { get; }
    /// <summary>Whether the principal is a human or an agent.</summary>
    public PrincipalKind Kind { get; }
    /// <summary>How the principal proved its identity.</summary>
    public TrustTier TrustTier { get; }
    /// <summary>Scopes granted on the bearer token.</summary>
    public IReadOnlyList<string> Scopes { get; }

    public Response(PrincipalId principalId, PrincipalKind kind, TrustTier trustTier, IReadOnlyList<string> scopes)
    {
      if (principalId.IsEmpty)
      {
        throw new ArgumentException("PrincipalId cannot be empty.", nameof(principalId));
      }

      PrincipalId = principalId;
      Kind = kind;
      TrustTier = trustTier;
      Scopes = Guard.Against.Null(scopes);
    }
  }
}

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
#endregion

namespace TimeWarp.Architecture.Features.AgentBearerSamples;

using TimeWarp.Identity;

[ApiEndpoint]
[EndpointAuthorize(Policy = "agent-scope:identity:read")] // matches AgentTokenDefaults.IdentityReadPolicy
public static partial class GetAgentBearerIdentity
{
  [ApiRoute("api/agent/bearer/me", HttpVerb.Get)]
  public sealed partial class Query : IApiRequest, IRequest<OneOf<Response, SharedProblemDetails>>;

  public sealed class Validator : AbstractValidator<Query>;

  public sealed class Response
  {
    public PrincipalId PrincipalId { get; }
    public PrincipalKind Kind { get; }
    public TrustTier TrustTier { get; }
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

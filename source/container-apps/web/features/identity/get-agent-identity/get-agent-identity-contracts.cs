#region Purpose
// Endpoint-centric contract for an authenticated agent to read its own identity (principal id, kind,
// trust tier, and the scopes carried by the presented bearer token) — the bearer-token analog of
// GetCurrentSession.
#endregion

#region Design
// Route-only, empty body: identity comes from the request's Authorization: Bearer token (validated
// by the agent-token authentication scheme — features/identity/agent-token-authentication-scheme-server.cs),
// never from a client-supplied id. This endpoint is the ONE protected resource this task ships
// ([EndpointAuthorize] policy agent-scope:identity:read = AgentTokenDefaults.IdentityReadPolicy) — it exists to
// prove end-to-end that bearer validation and scope enforcement actually work, not because agents
// need a self-lookup for its own sake.
// Scopes on the Response are the scopes carried by the CURRENT token (from its claims), not every
// scope the principal could ever hold — a principal with two registered keys, each token issued with
// different scopes, would report different Scopes here depending on which token is presented.
// No GetMockResponseFactory — see StartAgentKeyRegistration's Design region.
#endregion

namespace TimeWarp.Architecture.Features.Identity;

[ApiEndpoint]
[EndpointAuthorize(Policy = "agent-scope:identity:read")] // matches AgentTokenDefaults.IdentityReadPolicy
public static partial class GetAgentIdentity
{
  [ApiRoute("api/identity/agent/me", HttpVerb.Get)]
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

#region Purpose
// Cookie-free IAgentCallerContext implementation: reads the agent-token scheme's claims off the
// current HttpContext.User, populated earlier in the pipeline by AgentTokenAuthenticationHandler.
#endregion

#region Design
// Scoped (per-request IHttpContextAccessor), matching CookieBrowserSessionService's lifetime.
// AuthenticationType is checked defensively (not merely IsAuthenticated) even though structural
// scheme isolation already guarantees this: get-agent-identity-endpoint.cs's
// Agent-only contracts restrict AuthenticationSchemes to agent-token (e.g. GetAgentIdentity), so
// ASP.NET Core's authorization middleware never asks the cookie scheme to authenticate for those
// endpoints and reassigns HttpContext.User to the agent-token principal on success — the
// AuthenticationType check costs one property read and protects against a future endpoint reusing
// this context without the same scheme restriction. Also used by PermissionEvaluator (182-006).
#endregion

namespace TimeWarp.Architecture.Services;

using System.Security.Claims;

public sealed class AgentCallerContext : IAgentCallerContext
{
  private readonly IHttpContextAccessor HttpContextAccessor;

  public AgentCallerContext(IHttpContextAccessor httpContextAccessor)
  {
    HttpContextAccessor = httpContextAccessor;
  }

  public AgentCaller? GetCurrentCaller()
  {
    ClaimsPrincipal? user = HttpContextAccessor.HttpContext?.User;
    if (user?.Identity is not { IsAuthenticated: true } identity
      || !string.Equals(identity.AuthenticationType, AgentTokenDefaults.Scheme, StringComparison.Ordinal))
    {
      return null;
    }

    string? principalIdClaim = user.FindFirstValue(IdentitySessionDefaults.PrincipalIdClaimType);
    if (!Guid.TryParse(principalIdClaim, out Guid guid) || guid == Guid.Empty)
    {
      return null;
    }

    var scopes = user.FindAll(AgentTokenDefaults.ScopeClaimType).Select(claim => claim.Value).ToList();
    return new AgentCaller(PrincipalId.From(guid), scopes);
  }
}

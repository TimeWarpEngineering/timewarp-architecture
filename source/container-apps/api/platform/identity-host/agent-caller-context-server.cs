#region Purpose
// Cookie-free IAgentCallerContext implementation: reads the agent-token scheme's claims off the
// current HttpContext.User, populated earlier by AgentTokenAuthenticationHandler.
#endregion

#region Design
// Scoped (per-request IHttpContextAccessor). AuthenticationType is checked defensively even though
// IdentityReadPolicy restricts AuthenticationSchemes to ONLY "agent-token".
// Claim type constants come from api AgentTokenDefaults (PrincipalIdClaimType matches web's
// IdentitySessionDefaults.PrincipalIdClaimType).
#endregion

namespace TimeWarp.Architecture.Services;

using System.Security.Claims;
using TimeWarp.Architecture.Abstractions;
using TimeWarp.Architecture.Configuration;
using TimeWarp.Identity;

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

    string? principalIdClaim = user.FindFirstValue(AgentTokenDefaults.PrincipalIdClaimType);
    if (!Guid.TryParse(principalIdClaim, out Guid guid) || guid == Guid.Empty)
    {
      return null;
    }

    var scopes = user.FindAll(AgentTokenDefaults.ScopeClaimType).Select(claim => claim.Value).ToList();
    return new AgentCaller(PrincipalId.From(guid), scopes);
  }
}

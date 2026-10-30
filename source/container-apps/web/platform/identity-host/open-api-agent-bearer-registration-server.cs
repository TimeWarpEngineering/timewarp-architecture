#region Purpose
// Builds the web host's OpenAPI agent-bearer map from the scope seed and the agent-token scheme constant.
#endregion

#region Design
// AgentScopePermissionSeed is scope → permissions. OpenAPI needs the inverse: the policy
// name on the endpoint (the permission id) → the scope that grants it. One call site so a
// new seed pair is documented without a second table. The scheme name is
// AuthenticationSchemeNames.AgentToken, the same constant contracts pass to AuthSchemes.
#endregion

namespace TimeWarp.Architecture.Web.Server;

using TimeWarp.Architecture.Authorization;
using TimeWarp.Architecture.Features;
using TimeWarp.Foundation;

/// <summary>Web host registration for <see cref="OpenApiAgentBearer"/>.</summary>
internal static class OpenApiAgentBearerRegistration
{
  internal static OpenApiAgentBearer Create()
  {
    return new OpenApiAgentBearer
    (
      AuthenticationSchemeNames.AgentToken,
      OpenApiAgentBearer.PolicyScopesFromGrants(AgentScopePermissionSeed.DefaultGrants)
    );
  }
}

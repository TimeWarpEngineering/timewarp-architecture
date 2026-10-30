#region Purpose
// The api host's OpenAPI agent-bearer map, from the same policy and scope constants AddPolicy uses.
#endregion

#region Design
// TWA0024 only sees an AddPolicy name that is a compile-time constant, so program.cs registers
// AgentTokenDefaults.IdentityReadPolicy and DemoInvokePolicy directly. This map uses those same
// constants and AgentScopes, which is what each policy's RequireClaim demands. A rename of either
// constant updates authorization and the document together. foundation-server never sees the strings.
#endregion

namespace TimeWarp.Architecture.Api.Server;

using TimeWarp.Architecture.Configuration;
using TimeWarp.Foundation;
using TimeWarp.Identity;

/// <summary>OpenAPI policy-to-scope map for api-server agent bearer routes.</summary>
internal static class AgentBearerPolicies
{
  internal static OpenApiAgentBearer OpenApiBearer()
  {
    Dictionary<string, IReadOnlyList<string>> scopeGrants = new(StringComparer.Ordinal)
    {
      [AgentScopes.IdentityRead] = [AgentTokenDefaults.IdentityReadPolicy],
      [AgentScopes.DemoInvoke] = [AgentTokenDefaults.DemoInvokePolicy],
    };

    return new OpenApiAgentBearer
    (
      AgentTokenDefaults.Scheme,
      OpenApiAgentBearer.PolicyScopesFromGrants(scopeGrants)
    );
  }
}

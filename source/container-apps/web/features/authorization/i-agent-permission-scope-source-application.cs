#region Purpose
// Web-only port: held agent-token scopes for permission expansion (avoids dual-host type collision).
#endregion

#region Design
// Task 182-006 smoke fix: PermissionEvaluator must not depend on IAgentCallerContext in Features
// tests compiled under JARIBU_MULTI (web-jaribu-tests + timewarp-testing references both
// web-application and api-application, each defining IAgentCallerContext / AgentCaller in
// TimeWarp.Architecture.Abstractions → CS0433). This interface lives only in the web Features
// substrate (compiled into web-application only). Host adapter
// AgentCallerPermissionScopeSource maps IAgentCallerContext → this port. Host-free tests
// implement fakes without referencing the dual-host types.
// Null return = no ambient agent caller or PrincipalId mismatch (fail-closed for evaluator).
#endregion

namespace TimeWarp.Architecture.Features;

using TimeWarp.Identity;

/// <summary>Provides held agent-token scopes for the ambient request when principal matches.</summary>
public interface IAgentPermissionScopeSource
{
  /// <summary>
  /// Scopes for <paramref name="principalId"/> when ambient agent-token caller matches;
  /// otherwise <see langword="null"/>.
  /// </summary>
  IReadOnlyList<string>? GetHeldScopesFor(PrincipalId principalId);
}

#region Purpose
// Shared authentication scheme name strings for web-contracts [EndpointAuthorize(AuthenticationSchemes)].
#endregion

#region Design
// Task 158: contracts assemblies cannot reference server-layer scheme constants
// (IdentitySessionDefaults.Scheme / MockIdentityPrincipalHandler.SchemeName /
// AgentTokenDefaults.Scheme live in the web-server / identity-host layer). Features substrate
// (bare …Features namespace, same pattern as RoleIds / PermissionIds) so
// both the Admin.Roles/Admin.Principals slice and the Identity slice can reference one shared
// constant without TWA0009 cross-slice coupling.
// Literal string values MUST stay in lockstep with the server-side scheme constants they mirror —
// each member documents its server-side counterpart; there is no compile-time link (the two
// projects don't reference each other), so a rename on either side needs the matching edit here.
// Scheme SSOT (task 161 / ADR-0010): PermissionIds policies registered via AddPermissionPolicies
// carry PermissionRequirement only — no AddAuthenticationSchemes. ASP.NET Core 10's
// PolicyEvaluator.AuthenticateAsync is a no-op when the combined policy's scheme list is empty
// (only UseAuthentication's default scheme ran: identity-session). Non-default schemes
// (mock-identity-session, agent-token) run only when they appear on the combined policy, which
// FastEndpoints fills from AuthSchemes(...) (copied onto IAuthorizeData) and/or from a named
// policy's own AddAuthenticationSchemes (copied by Combine). Product permission policies have
// the latter empty, so hosted [EndpointAuthorize] MUST set AuthenticationSchemes. Do not put
// scheme lists back on permission policies (dual SSOT). IdentitySessionDefaults.AuthenticatedPolicy
// and api-server agent-scope policies still list schemes at policy level; declaring them on the
// contract as well is belt-and-suspenders against a future policy-registration change.
// mock-identity-session is always safe to list here: the scheme is unconditionally registered
// (program.cs .AddScheme<AuthenticationSchemeOptions, MockIdentityPrincipalHandler>(...)) so
// listing it never throws InvalidOperationException for an unregistered scheme; the HANDLER itself
// (MockIdentityPrincipalHandler.HandleAuthenticateAsync) is what's fail-closed — it returns
// AuthenticateResult.NoResult() unless Development/Testing + Authentication:UseMock + the mock
// header are all present, so Production is unaffected regardless of which endpoints list the scheme.
#endregion

namespace TimeWarp.Architecture.Features;

/// <summary>Authentication scheme names shared by web-contracts <c>[EndpointAuthorize]</c> declarations.</summary>
public static class AuthenticationSchemeNames
{
  /// <summary>Matches <c>IdentitySessionDefaults.Scheme</c> (browser session cookie).</summary>
  public const string IdentitySession = "identity-session";

  /// <summary>
  /// Matches <c>MockIdentityPrincipalHandler.SchemeName</c> — always registered, fail-closed inside
  /// the handler itself (Development/Testing + Authentication:UseMock + header); safe to list
  /// unconditionally.
  /// </summary>
  public const string MockIdentitySession = "mock-identity-session";

  /// <summary>Matches <c>AgentTokenDefaults.Scheme</c> (bearer token).</summary>
  public const string AgentToken = "agent-token";
}

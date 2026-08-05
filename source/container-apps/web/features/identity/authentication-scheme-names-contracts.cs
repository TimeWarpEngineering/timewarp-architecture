#region Purpose
// Shared authentication scheme name strings for web-contracts [EndpointAuthorize(AuthenticationSchemes)].
#endregion

#region Design
// Task 158: contracts assemblies cannot reference server-layer scheme constants
// (IdentitySessionDefaults.Scheme / MockIdentityPrincipalHandler.SchemeName /
// AgentTokenDefaults.Scheme live in the web-server / identity-host layer). Features substrate
// (bare …Features namespace, same pattern as AuthorizationPolicyNames / RoleIds / ModuleIds) so
// both the Admin.Roles/Admin.Principals slice and the Identity slice can reference one shared
// constant without TWA0009 cross-slice coupling.
// Literal string values MUST stay in lockstep with the server-side scheme constants they mirror —
// each member documents its server-side counterpart; there is no compile-time link (the two
// projects don't reference each other), so a rename on either side needs the matching edit here.
// Root cause (task 158): the generated FastEndpoint's Configure() emitted only Policies(...), never
// AuthSchemes(...); AuthorizationMiddleware then never invoked the mock-identity-session (or, for
// dual-scheme policies, non-default) authentication handler for that route, so an otherwise-valid
// mock/bearer principal was never attached and the request fell through as anonymous (401 instead
// of 403). Fix: emit AuthSchemes(...) from [EndpointAuthorize(AuthenticationSchemes = ...)] using
// this class's constants, mirroring exactly the scheme list already declared on the matching
// server-side AddPolicy(...).AddAuthenticationSchemes(...) call (web-server/program.cs).
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

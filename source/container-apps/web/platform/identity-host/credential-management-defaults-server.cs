#region Purpose
// Policy name for the credential-management surface (list/add/revoke credentials on the caller's own
// principal) — the one policy in this template that deliberately accepts EITHER authentication
// scheme.
#endregion

#region Design
// Task 104-005. Every other policy in this template is scheme-restricted to exactly one scheme
// (IdentitySessionDefaults.AuthenticatedPolicy → identity-session only; AgentTokenDefaults.
// IdentityReadPolicy → agent-token only) — credential-management is the first policy that must admit
// both, because both a signed-in human (cookie) and an authenticated agent (bearer) legitimately
// manage their OWN credentials (list, add a new key/passkey, revoke an old one; agent key rotation is
// add-then-revoke under this same policy).
// Shape (program.cs's AddAuthorizationBuilder call):
//   .AddAuthenticationSchemes(IdentitySessionDefaults.Scheme, AgentTokenDefaults.Scheme)
//   .RequireAuthenticatedUser()
//   .RequireAssertion(ctx =>
//     ctx.User.Identity?.AuthenticationType == IdentitySessionDefaults.Scheme       // cookie: full self-control
//     || ctx.User.HasClaim(AgentTokenDefaults.ScopeClaimType, AgentScopes.CredentialManage)) // agent: least privilege
// RequireAssertion, not RequireClaim: a cookie-authenticated ClaimsPrincipal carries NO scope claim
// at all (scopes are an agent-bearer-token concept — see AgentTokenDefaults' Design region), so a
// claim-based rule alone cannot express "cookie principals always pass, agent principals need this
// specific scope." The assertion's two arms are deliberately asymmetric: a cookie principal proved
// full session-level control of the account (the same trust level Roles CRUD grants), so it gets
// blanket credential-management rights over its own principal with no scope concept to check; an
// agent principal proved only possession of ONE registered key, so it additionally needs the
// credential:manage scope on the presented token — an identity:read-only token (GetAgentIdentity's
// own policy) must NOT be able to list, add, or revoke credentials just because it happens to be a
// validly authenticated agent-token principal. See AgentScopes' Design region for why
// credential:manage is a distinct scope from identity:read, not a superset relationship.
// AuthenticationType check (not merely "did the identity-session scheme's handler run"): mirrors
// AgentCallerContext's own defensive AuthenticationType check — cheap, and protects against a future
// endpoint reusing this policy in a context where scheme attribution could otherwise be ambiguous.
// Handlers behind this policy resolve "whose credentials" via ICurrentPrincipalAccessor, which reads
// HttpContext.User directly rather than re-deriving which scheme won (see that port's Design
// region) — this policy's job is authorization (may this request proceed at all), the accessor's job
// is identity (proceed AS WHOM); the two are deliberately separate concerns.
#endregion

namespace TimeWarp.Architecture.Configuration;

public static class CredentialManagementDefaults
{
  public const string Policy = "credential-management";
}

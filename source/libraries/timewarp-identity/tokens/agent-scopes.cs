#region Purpose
// Canonical set of scope strings an agent access token can carry, and validity checking for
// caller-supplied scope lists.
#endregion

#region Design
// Plain string constants (not an enum) — scopes travel over the wire as strings in both the request
// (CompleteAgentTokenIssuance.Command.Scopes) and the response/claims, and a string is what
// RequireClaim/HasClaim compare against; an enum would need a string<->enum mapping layer for zero
// benefit at Wave-1 scale (three scopes).
// IdentityRead is this task's own protected endpoint (GetAgentIdentity, policy
// AgentTokenDefaults.IdentityReadPolicy) — a real, exercised scope. DemoInvoke is reserved: no
// endpoint checks it yet (its consumer is 104-011), but it is declared here now so
// CompleteAgentTokenIssuance can accept it as a known scope and Agent_Protected_Endpoint_Tests can
// exercise a genuine 403 insufficient-scope case (a token minted with ONLY demo:invoke, presented
// against the identity:read-gated endpoint) rather than a synthetic/unknown scope string. 104-013
// gates demo:invoke USAGE by credit balance at call time, not by removing the scope from a token —
// recorded here, revisitable if that policy changes.
// CredentialManage (task 104-005): a DISTINCT write scope for the credential-management surface
// (list/add/revoke credentials on the caller's own principal) — deliberately NOT gated behind
// IdentityRead. IdentityRead is a read-only scope (GetAgentIdentity, a self-lookup); credential
// management is destructive (revoke can lock the principal out of authentication entirely) and
// mutates the principal's own security material (add mints new authentication material). Letting an
// identity:read-only token perform credential writes would be a privilege escalation baked into the
// scope model itself — a token minted for "let me read my own identity" would silently also be able
// to revoke every credential the principal has. One scope covers list+add+revoke rather than three
// finer-grained ones: an agent that needs to rotate its own key (add new + revoke old) needs all
// three operations together anyway, and Wave-1 has no consumer that needs, say, list-only agent
// access without add/revoke. A credential-management-scoped token CAN list; an identity:read-only
// token CANNOT (see CredentialManagementDefaults' Design region for how the policy enforces this) —
// intentional least-privilege, not an oversight.
#endregion

namespace TimeWarp.Identity;

public static class AgentScopes
{
  public const string IdentityRead = "identity:read";
  public const string DemoInvoke = "demo:invoke";
  public const string CredentialManage = "credential:manage";

  public static IReadOnlyList<string> All { get; } = [IdentityRead, DemoInvoke, CredentialManage];

  public static bool IsKnown(string scope) => All.Contains(scope, StringComparer.Ordinal);
}

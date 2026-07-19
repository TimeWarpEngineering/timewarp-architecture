#region Purpose
// Canonical set of scope strings an agent access token can carry, and validity checking for
// caller-supplied scope lists.
#endregion

#region Design
// Plain string constants (not an enum) — scopes travel over the wire as strings in both the request
// (CompleteAgentTokenIssuance.Command.Scopes) and the response/claims, and a string is what
// RequireClaim/HasClaim compare against; an enum would need a string<->enum mapping layer for zero
// benefit at Wave-1 scale (two scopes).
// IdentityRead is this task's own protected endpoint (GetAgentIdentity, policy
// AgentTokenDefaults.IdentityReadPolicy) — a real, exercised scope. DemoInvoke is reserved: no
// endpoint checks it yet (its consumer is 104-011), but it is declared here now so
// CompleteAgentTokenIssuance can accept it as a known scope and Agent_Protected_Endpoint_Tests can
// exercise a genuine 403 insufficient-scope case (a token minted with ONLY demo:invoke, presented
// against the identity:read-gated endpoint) rather than a synthetic/unknown scope string. 104-013
// gates demo:invoke USAGE by credit balance at call time, not by removing the scope from a token —
// recorded here, revisitable if that policy changes.
#endregion

namespace TimeWarp.Identity;

public static class AgentScopes
{
  public const string IdentityRead = "identity:read";
  public const string DemoInvoke = "demo:invoke";

  public static IReadOnlyList<string> All { get; } = [IdentityRead, DemoInvoke];

  public static bool IsKnown(string scope) => All.Contains(scope, StringComparer.Ordinal);
}

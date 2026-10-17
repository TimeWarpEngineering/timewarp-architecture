#region Purpose
// Canonical set of scope strings an agent access token can carry, and validity checking for
// caller-supplied scope lists.
#endregion

#region Design
// Plain string constants (not an enum) — scopes travel over the wire as strings in both the request
// (CompleteAgentTokenIssuance.Command.Scopes) and the response/claims, and a string is what
// RequireClaim/HasClaim compare against; an enum would need a string<->enum mapping layer for zero
// benefit. Feedback scopes (task 378) are one permission each: read, note, severity, status.
// They are not admin.* and do not include a reply to the filer.
// IdentityRead is this task's own protected endpoint (GetAgentIdentity, policy
// AgentTokenDefaults.IdentityReadPolicy) — a real, exercised scope. DemoInvoke gates the metered
// pay-for-capability demo (104-011, AgentTokenDefaults.DemoInvokePolicy); Agent_Protected_Endpoint_Tests
// still uses a token minted with ONLY demo:invoke against the identity:read-gated endpoint for a
// genuine 403 insufficient-scope case. 104-013 may further gate demo:invoke USAGE by credit balance /
// Funded tier at call time, not by removing the scope from a token — recorded here, revisitable if
// that policy changes.
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

/// <summary>
/// Canonical agent access-token scope strings and membership checking for caller-supplied lists.
/// </summary>
public static class AgentScopes
{
  /// <summary>Read-only self-identity lookup (for example GetAgentIdentity).</summary>
  public const string IdentityRead = "identity:read";

  /// <summary>Invoke the metered pay-for-capability demo endpoint.</summary>
  public const string DemoInvoke = "demo:invoke";

  /// <summary>List, add, and revoke credentials on the caller's own principal (write scope).</summary>
  public const string CredentialManage = "credential:manage";

  /// <summary>List and open every feedback item, including internal notes and attachments.</summary>
  public const string FeedbackRead = "feedback:read";

  /// <summary>Append an internal note on a feedback item.</summary>
  public const string FeedbackNote = "feedback:note";

  /// <summary>Set a feedback item's review severity.</summary>
  public const string FeedbackSeverity = "feedback:severity";

  /// <summary>Change a feedback item's status.</summary>
  public const string FeedbackStatus = "feedback:status";

  /// <summary>Every known scope string this library recognizes.</summary>
  public static IReadOnlyList<string> All { get; } =
  [
    IdentityRead,
    DemoInvoke,
    CredentialManage,
    FeedbackRead,
    FeedbackNote,
    FeedbackSeverity,
    FeedbackStatus,
  ];

  /// <summary>True when <paramref name="scope"/> is one of <see cref="All"/> (ordinal).</summary>
  public static bool IsKnown(string scope) => All.Contains(scope, StringComparer.Ordinal);
}

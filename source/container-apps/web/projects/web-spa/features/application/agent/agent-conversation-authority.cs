#region Purpose
// Decides whether a conversation credential may run one action for the current principal.
#endregion

#region Design
// A null credential is allowed: existing sessions have no conversation credential and still run
// under the signed-in principal. When a credential is present it must be unexpired, belong to
// the same NameIdentifier when the user has one, and include every required permission id.
// Listing tools does not consult this type. Both drivers call it after approval and before Execute.
// The three error strings are the parity contract: chat and WebMCP return the same text.
#endregion

namespace TimeWarp.Architecture.Features.Applications;

/// <summary>Shared refusal rules for a conversation credential.</summary>
public static class AgentConversationAuthority
{
  public const string ExpiredError = "This conversation's credential has expired.";

  public const string ScopeError = "This conversation's credential does not include that action.";

  public const string PrincipalError = "This conversation's credential belongs to a different account.";

  public static string? Denial
  (
    AgentConversationCredential? credential,
    ClaimsPrincipal? user,
    IEnumerable<string>? requiredPermissions,
    DateTimeOffset? utcNow = null
  )
  {
    if (credential is null)
    {
      return null;
    }

    DateTimeOffset now = utcNow ?? DateTimeOffset.UtcNow;
    if (credential.ExpiresAt <= now)
    {
      return ExpiredError;
    }

    if (user is not null)
    {
      string? idText = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
      if (Guid.TryParse(idText, out Guid current) && current != credential.PrincipalId)
      {
        return PrincipalError;
      }
    }

    if (requiredPermissions is null)
    {
      return null;
    }

    foreach (string required in requiredPermissions)
    {
      if (string.IsNullOrEmpty(required))
      {
        continue;
      }

      bool held = false;
      foreach (string scope in credential.Scopes)
      {
        if (string.Equals(scope, required, StringComparison.Ordinal))
        {
          held = true;
          break;
        }
      }

      if (!held)
      {
        return ScopeError;
      }
    }

    return null;
  }
}

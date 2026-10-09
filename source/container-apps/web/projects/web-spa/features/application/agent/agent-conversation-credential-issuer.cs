#region Purpose
// Mints a conversation credential that expires at the end of the UTC day or sooner.
#endregion

#region Design
// Lifetime defaults to twelve hours and never crosses the next UTC midnight, so a conversation
// opened late in the day cannot outlive that day. The scope list is copied. The display name
// is the conversation title, truncated so a long first prompt cannot dominate the header.
#endregion

namespace TimeWarp.Architecture.Features.Applications;

/// <summary>Creates <see cref="AgentConversationCredential"/> values.</summary>
public static class AgentConversationCredentialIssuer
{
  public const int MaxDisplayNameLength = 80;

  public const string DefaultDisplayName = "Ask AI conversation";

  public static readonly TimeSpan DefaultLifetime = TimeSpan.FromHours(12);

  public static AgentConversationCredential Issue
  (
    Guid principalId,
    IReadOnlyList<string> scopes,
    DateTimeOffset utcNow,
    TimeSpan? lifetime = null,
    string? displayName = null
  )
  {
    ArgumentNullException.ThrowIfNull(scopes);
    TimeSpan span = lifetime ?? DefaultLifetime;
    if (span < TimeSpan.Zero)
    {
      span = TimeSpan.Zero;
    }

    DateTimeOffset utc = utcNow.ToUniversalTime();
    DateTimeOffset endOfUtcDay = new(utc.UtcDateTime.Date.AddDays(1), TimeSpan.Zero);
    DateTimeOffset expires = utc + span;
    if (expires > endOfUtcDay)
    {
      expires = endOfUtcDay;
    }

    string name = string.IsNullOrWhiteSpace(displayName) ? DefaultDisplayName : displayName.Trim();
    if (name.Length > MaxDisplayNameLength)
    {
      name = name[..MaxDisplayNameLength];
    }

    string[] copied = new string[scopes.Count];
    for (int index = 0; index < scopes.Count; index++)
    {
      copied[index] = scopes[index];
    }

    return new AgentConversationCredential(Guid.CreateVersion7(), principalId, copied, name, expires);
  }
}

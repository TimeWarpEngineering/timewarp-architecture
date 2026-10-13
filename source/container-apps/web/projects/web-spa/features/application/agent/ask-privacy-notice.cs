#region Purpose
// Decides when the Ask panel shows the chat-recording notice.
#endregion

#region Design
// Recording is a template configuration flag, not a hardcoded banner. Showing the notice while
// chats are not recorded would be false. A dismissed notice stays dismissed in this browser
// (AgentSurfaceState persists the flag to local storage under DismissedStorageKey).
#endregion

namespace TimeWarp.Architecture.Features.Applications;

/// <summary>Visibility rule for the Ask privacy notice.</summary>
public static class AskPrivacyNotice
{
  /// <summary>Browser local-storage key that remembers the dismissal.</summary>
  public const string DismissedStorageKey = "twe.ask.privacy-notice-dismissed";

  public static bool ShouldShow(bool recordChats, bool dismissed, string? notice) =>
    recordChats && !dismissed && !string.IsNullOrWhiteSpace(notice);
}

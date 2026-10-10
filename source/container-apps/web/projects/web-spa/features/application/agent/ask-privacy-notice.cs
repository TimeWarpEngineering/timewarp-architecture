#region Purpose
// Decides when the Ask panel shows the chat-recording notice.
#endregion

#region Design
// Recording is a template configuration flag, not a hardcoded banner. Showing the notice while
// chats are not recorded would be false. A dismissed notice stays dismissed for the shell session.
#endregion

namespace TimeWarp.Architecture.Features.Applications;

/// <summary>Visibility rule for the Ask privacy notice.</summary>
public static class AskPrivacyNotice
{
  public static bool ShouldShow(bool recordChats, bool dismissed, string? notice) =>
    recordChats && !dismissed && !string.IsNullOrWhiteSpace(notice);
}

#region Purpose
// Screenshot state: the latest capture of the app's own page and the one waiting for Ask.
#endregion

#region Design
// Task 303-001. CaptureScreenshot, CaptureForAsk (Ask's camera button) and CaptureForFeedback
// (Feedback's Capture this page button) share one capture path, CaptureAsync, so every capture
// becomes Latest. Latest is the newest capture, with the route it was taken on; Feedback
// offers it as "the page behind it" when it came from another page, and page_context.screenshot
// describes it (metadata only) on its own route. AskAttached is the capture the Ask panel's
// Screenshot button (CaptureForAsk) put on the composer for conversation AskGeneration. AskScreenshotChatClient
// sends it with the next message as image content, then MarkAskSent clears it and records
// AskSentId. A new conversation or a removed thumbnail drops it. Nothing here uploads:
// Feedback uploads through its own UploadFeedbackAttachment action when the person clicks attach.
// ForFeedback is the capture Feedback's button took; only CaptureForFeedback sets it, so an agent
// capture (which moves Latest) never replaces it.
// MaxBytes matches FeedbackAttachmentRules.MaxBytes (a test in web-spa-integration-tests keeps
// them equal); Applications is the platform tier, so it cannot read the Feedback rule itself.
// Initialize clears every field; the store reset on sign-out therefore drops the pictures.
#endregion

namespace TimeWarp.Architecture.Features.Applications;

[StateAccess]
public sealed partial class ScreenshotState : State<ScreenshotState>
{
  public const string ContentType = "image/png";

  public const int MaxBytes = 5 * 1024 * 1024;

  public const int SelectorCap = 200;

  /// <summary>The newest capture, or null before the first one.</summary>
  public ScreenshotCapture? Latest { get; private set; }

  /// <summary>The capture waiting on Ask's composer for conversation <see cref="AskGeneration"/>.</summary>
  public ScreenshotCapture? AskAttached { get; private set; }

  /// <summary>The capture Feedback's Capture this page button took last.</summary>
  public ScreenshotCapture? ForFeedback { get; private set; }

  public int AskGeneration { get; private set; }

  /// <summary>Id of the last capture Ask sent in conversation <see cref="AskGeneration"/>.</summary>
  public int? AskSentId { get; private set; }

  public int NextId { get; private set; }

  /// <summary>Why the last capture failed, or null when it succeeded.</summary>
  public string? LastError { get; private set; }

  public ScreenshotState() { }

  public ScreenshotCapture? AskAttachedFor(int conversationGeneration) =>
    AskGeneration == conversationGeneration ? AskAttached : null;

  public bool AskSentFor(int conversationGeneration, int captureId) =>
    AskGeneration == conversationGeneration && AskSentId == captureId;

  public override void Initialize()
  {
    Latest = null;
    AskAttached = null;
    ForFeedback = null;
    AskGeneration = 0;
    AskSentId = null;
    NextId = 0;
    LastError = null;
  }

  /// <summary>Drops the Ask attachment of an older conversation.</summary>
  private void Rebase(int conversationGeneration)
  {
    if (AskGeneration == conversationGeneration)
    {
      return;
    }

    AskGeneration = conversationGeneration;
    AskAttached = null;
    AskSentId = null;
  }
}

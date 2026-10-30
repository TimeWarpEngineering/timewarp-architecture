#region Purpose
// CopyMessageBar action: writes a page-local message bar's text to the clipboard and confirms it.
#endregion

#region Design
// Task 445. Shell rows copy through CopyMessage(id), which reads the text from the row. A
// page-local bar (static Info/Warning guidance, TWA0025) is not in NotificationState, so the
// shared MessageBar control passes its own Title and Body. BarId is the control instance's id;
// it shares CopiedMessageId with shell rows, so one "Copied" confirmation shows at a time and
// the shell host's ExpireMessages timer ends it after CopiedConfirmationInterval.
// Format, confirmation, and the failure Warning are CopyMessage's (WriteAndConfirmAsync).
// The bar's own buttons are rendered actions, not text, so they are never copied. Body is a
// plain string (empty when the bar has none) because the generated dispatch method's
// parameters are not nullable; ClipboardText treats a blank body as no body.
#endregion

namespace TimeWarp.Architecture.Features;

partial class NotificationState
{
  // Named ...ActionSet so the generator emits `NotificationState.CopyMessageBar(barId, title, body)`.
  public static class CopyMessageBarActionSet
  {
    [CommandPaletteOptOut("Copies the text of a message bar on screen; that bar's Copy button runs it.")]
    public sealed class Action : IBaseAction
    {
      public string BarId { get; }
      public string Title { get; }
      /// <summary>Body text; empty when the bar has none.</summary>
      public string Body { get; }

      public Action(string barId, string title, string body)
      {
        BarId = barId ?? throw new ArgumentNullException(nameof(barId));
        Title = title ?? throw new ArgumentNullException(nameof(title));
        Body = body ?? string.Empty;
      }
    }

    internal sealed class Handler : BaseHandler<Action>
    {
      private readonly IJSRuntime JsRuntime;

      public Handler(IStore store, IJSRuntime jsRuntime) : base(store)
      {
        JsRuntime = jsRuntime;
      }

      public override ValueTask Handle
      (
        Action action,
        CancellationToken cancellationToken
      ) =>
        WriteAndConfirmAsync(NotificationState, JsRuntime, action.BarId, ClipboardText(action.Title, action.Body), cancellationToken);
    }
  }
}

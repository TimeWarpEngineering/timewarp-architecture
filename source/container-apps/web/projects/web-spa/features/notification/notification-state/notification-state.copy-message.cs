#region Purpose
// CopyMessage action: writes one shell message bar's text to the clipboard and confirms it.
#endregion

#region Design
// Task 313. The MessageBars Copy button only dispatches; this handler does the clipboard
// interop through ClipboardJsModule (TWA0026: no IJSRuntime in components). Copied text is
// ClipboardText: Title, plus a blank line and Body when there is a body.
// Confirmation is an icon swap on the row (Copy → Checkmark, label "Copied"), not a Success
// bar: a Success bar would push the oldest bar behind "+N more" and could hide the very error
// being copied. MarkCopied stamps CopiedUntil, which NextExpiry includes, so the host's existing
// ExpireMessages timer swaps the icon back — no second timer.
// A clipboard rejection (permission denied, insecure context) is a JSException and becomes a
// Warning bar; nothing throws. A JS-interop timeout (OperationCanceledException while the
// caller's token is live, e.g. a stalled permission prompt on Server) is the same failure. A
// cancelled caller token means the host was disposed, so it is swallowed. ClipboardJsModule
// already swallows a dropped Server circuit. An unknown id (the bar was dismissed or expired
// before the click landed) is a no-op.
#endregion

namespace TimeWarp.Architecture.Features;

using TimeWarp.Architecture.Services;

partial class NotificationState
{
  public const string CopyFailedTitle = "Couldn't copy the message.";
  public const string CopyFailedBody = "The browser blocked clipboard access. Select the text and copy it instead.";

  /// <summary>Plain text the Copy button writes: Title, plus a blank line and Body when present.</summary>
  public static string ClipboardText(NotificationMessage message)
  {
    ArgumentNullException.ThrowIfNull(message);
    return message.Body is null ? message.Title : $"{message.Title}\n\n{message.Body}";
  }

  // Named ...ActionSet so the generator emits `NotificationState.CopyMessage(id)`.
  public static class CopyMessageActionSet
  {
    public sealed class Action : IBaseAction
    {
      public string Id { get; }

      public Action(string id)
      {
        Id = id;
      }
    }

    internal sealed class Handler : BaseHandler<Action>
    {
      private readonly IJSRuntime JsRuntime;

      public Handler(IStore store, IJSRuntime jsRuntime) : base(store)
      {
        JsRuntime = jsRuntime;
      }

      public override async ValueTask Handle
      (
        Action action,
        CancellationToken cancellationToken
      )
      {
        NotificationMessage? message = NotificationState.Messages.FirstOrDefault(row => row.Id == action.Id);
        if (message is null)
        {
          return;
        }

        try
        {
          await ClipboardJsModule.WriteTextAsync(JsRuntime, ClipboardText(message), cancellationToken);
          NotificationState.MarkCopied(message.Id, DateTimeOffset.UtcNow);
        }
        catch (Exception exception) when
          (exception is JSException || (exception is OperationCanceledException && !cancellationToken.IsCancellationRequested))
        {
          NotificationState.AddMessage(MessageBarIntent.Warning, CopyFailedTitle, CopyFailedBody, DateTimeOffset.UtcNow);
        }
        catch (OperationCanceledException)
        {
          // The host was disposed mid-copy; there is no bar left to confirm or warn on.
        }
      }
    }
  }
}

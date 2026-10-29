#region Purpose
// CopyAssemblyInfoActionSet: writes the version dialog's text to the clipboard and confirms it.
#endregion

#region Design
// Task 450. The dialog's Copy button only dispatches (TWA0026); this handler calls
// ClipboardJsModule. The action takes the text the dialog built from AssemblyInfoText, so the
// clipboard matches the screen. Success sets IsAssemblyInfoCopied for the button's Checkmark;
// SetActiveModal clears it, so a reopened dialog starts at Copy. A clipboard rejection
// (JSException) or a JS-interop timeout becomes a Warning bar through OutcomeNotification; the
// handler does not dispatch another action. A cancelled caller token means the host was
// disposed, so it is swallowed. It is dialog plumbing: the Copy button is the only caller.
#endregion

namespace TimeWarp.Architecture.Features.Applications;

using TimeWarp.Architecture.Services;

partial class ApplicationState
{
  public const string AssemblyInfoCopyFailedTitle = "Couldn't copy the version info.";
  public const string AssemblyInfoCopyFailedBody = "The browser blocked clipboard access. Select the text and copy it instead.";

  /// <summary>Whether the version dialog's Copy last succeeded since a modal opened.</summary>
  public bool IsAssemblyInfoCopied { get; private set; }

  public static class CopyAssemblyInfoActionSet
  {
    [CommandPaletteOptOut("Copies the text the version dialog already shows; the Copy button in that dialog runs it.")]
    public sealed class Action : IBaseAction
    {
      public string Text { get; }

      public Action(string text)
      {
        Text = text ?? throw new ArgumentNullException(nameof(text));
      }
    }

    internal sealed class Handler : BaseHandler<Action>
    {
      private readonly IJSRuntime JsRuntime;
      private readonly IPublisher<ClientPipeline> Publisher;

      public Handler(IStore store, IJSRuntime jsRuntime, IPublisher<ClientPipeline> publisher) : base(store)
      {
        JsRuntime = jsRuntime;
        Publisher = publisher;
      }

      public override async ValueTask Handle(Action action, CancellationToken cancellationToken)
      {
        try
        {
          await ClipboardJsModule.WriteTextAsync(JsRuntime, action.Text, cancellationToken);
          ApplicationState.IsAssemblyInfoCopied = true;
        }
        catch (Exception exception) when
          (exception is JSException || (exception is OperationCanceledException && !cancellationToken.IsCancellationRequested))
        {
          ApplicationState.IsAssemblyInfoCopied = false;
          await Publisher.Publish
          (
            new OutcomeNotification(MessageBarIntent.Warning, AssemblyInfoCopyFailedTitle, AssemblyInfoCopyFailedBody),
            cancellationToken
          );
        }
        catch (OperationCanceledException)
        {
          // The host was disposed mid-copy.
        }
      }
    }
  }
}

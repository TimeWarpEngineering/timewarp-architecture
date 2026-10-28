#region Purpose
// CopyOnboardingActionSet: writes the account id or the agent prompt to the clipboard.
#endregion

#region Design
// The Agent links buttons only dispatch (TWA0026). This handler calls ClipboardJsModule.
// CopiedSlot names which button shows "Copied". A clipboard rejection becomes a Warning bar
// through OutcomeNotification; the handler does not dispatch another action. The action takes
// the text the page already rendered, so the clipboard matches the screen. It is palette
// plumbing: the page is the only caller.
#endregion

namespace TimeWarp.Architecture.Features.AgentLinks;

using TimeWarp.Architecture.Services;

partial class AgentLinksState
{
  public const string AccountIdSlot = "account-id";
  public const string PromptSlot = "prompt";
  public const string CopyFailedTitle = "Couldn't copy.";
  public const string CopyFailedBody = "The browser blocked clipboard access. Select the text and copy it instead.";

  /// <summary>Which onboarding copy button last succeeded; null after a failed copy.</summary>
  public string? CopiedSlot { get; private set; }

  public static class CopyOnboardingActionSet
  {
    [CommandPaletteOptOut("Copies text the Agent links page already shows; the buttons on that page run it.")]
    public sealed class Action : IBaseAction
    {
      public string Slot { get; }
      public string Text { get; }

      public Action(string slot, string text)
      {
        Slot = slot ?? throw new ArgumentNullException(nameof(slot));
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
          AgentLinksState.CopiedSlot = action.Slot;
        }
        catch (Exception exception) when
          (exception is JSException || (exception is OperationCanceledException && !cancellationToken.IsCancellationRequested))
        {
          AgentLinksState.CopiedSlot = null;
          await Publisher.Publish(
            new OutcomeNotification(MessageBarIntent.Warning, CopyFailedTitle, CopyFailedBody),
            cancellationToken);
        }
        catch (OperationCanceledException)
        {
          // The host was disposed mid-copy.
        }
      }
    }
  }
}

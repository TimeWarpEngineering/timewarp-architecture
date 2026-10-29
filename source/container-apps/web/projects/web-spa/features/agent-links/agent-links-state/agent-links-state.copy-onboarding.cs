#region Purpose
// CopyOnboardingActionSet: writes the account id or the agent prompt to the clipboard.
#endregion

#region Design
// The Agent links buttons only dispatch (TWA0026). This handler calls ClipboardJsModule.
// CopiedSlot names which button shows "Copied". A Denied write (ClipboardJsModule.TryWriteTextAsync)
// becomes a Warning bar with ClipboardJsModule.DeniedBody through OutcomeNotification; the handler does not dispatch another action. The action takes
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
        switch (await ClipboardJsModule.TryWriteTextAsync(JsRuntime, action.Text, cancellationToken))
        {
          case ClipboardJsModule.WriteResult.Written:
            AgentLinksState.CopiedSlot = action.Slot;
            break;
          case ClipboardJsModule.WriteResult.Denied:
            AgentLinksState.CopiedSlot = null;
            await Publisher.Publish(
              new OutcomeNotification(MessageBarIntent.Warning, CopyFailedTitle, ClipboardJsModule.DeniedBody),
              cancellationToken);
            break;
          case ClipboardJsModule.WriteResult.Cancelled:
            // The host was disposed mid-copy.
            break;
        }
      }
    }
  }
}

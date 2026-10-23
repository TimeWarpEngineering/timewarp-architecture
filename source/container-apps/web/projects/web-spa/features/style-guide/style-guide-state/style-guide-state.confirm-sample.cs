#region Purpose
// ConfirmSample: the style-guide opener. Asks, then posts a note. Stores nothing.
#endregion

#region Design
// Not an API action, so it does not sit on ApiHandler. It calls IConfirmationService, which is
// what an ApiHandler.ConfirmAsync override calls (ClearXaiApiKey). The page only dispatches.
// Opted out of the palette: the Confirmations card is the opener. Yes publishes
// OutcomeNotification. No returns with no note.
#endregion

namespace TimeWarp.Architecture.Features.StyleGuide;

partial class StyleGuideState
{
  /// <summary>Opens the sample confirmation.</summary>
  public static class ConfirmSampleActionSet
  {
    /// <summary>No arguments.</summary>
    [CommandPaletteOptOut("Style guide sample. The Confirmations card is the opener.")]
    public sealed class Action : IBaseAction;

    internal sealed class Handler(IStore store, IConfirmationService confirmation, IPublisher<ClientPipeline> publisher)
      : BaseHandler<Action>(store)
    {
      internal const string ConfirmTitle = "Confirm this sample?";
      internal const string ConfirmMessage = "Confirming posts a note in the notification region. Nothing else changes.";
      internal const string ConfirmLabel = "Confirm";
      internal const string CancelLabel = "Cancel";

      /// <inheritdoc />
      public override async ValueTask Handle(Action action, CancellationToken cancellationToken)
      {
        _ = action;
        bool confirmed = await confirmation.ConfirmAsync(
          ConfirmTitle,
          ConfirmMessage,
          ConfirmLabel,
          CancelLabel,
          cancellationToken);
        if (!confirmed)
        {
          return;
        }

        await publisher.Publish(
          new OutcomeNotification(MessageBarIntent.Success, "Sample confirmed."),
          cancellationToken);
      }
    }
  }
}

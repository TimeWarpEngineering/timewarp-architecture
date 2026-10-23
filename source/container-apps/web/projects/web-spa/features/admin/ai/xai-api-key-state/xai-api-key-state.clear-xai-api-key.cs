#region Purpose
// ClearXaiApiKey: removes the stored site key and refreshes the status.
#endregion

#region Design
// No key in the action. Opted out of the palette so a keystroke cannot clear the site key.
// The handler overrides ApiHandler.ConfirmAsync, which runs before the per-state semaphore.
// No skips the delete: no request and no error notification. It sets ClearDeclined so the
// Ask reload does not treat the skip as a successful clear. Yes clears that flag and sends
// the delete. SaveFailed covers a host-managed refusal. Any dispatcher (page, palette, agent)
// hits the same question.
#endregion

namespace TimeWarp.Architecture.Features.Admin.Ai;

using static ClearXaiApiKey;

partial class XaiApiKeyState
{
  /// <summary>Clears the stored site key.</summary>
  public static class ClearXaiApiKeyActionSet
  {
    /// <summary>No arguments.</summary>
    [TrackAction]
    [CommandPaletteOptOut("Clears the site xAI API key from Admin → AI.")]
    public sealed class Action : IBaseAction;

    internal sealed class Handler : DefaultApiHandler<Action, Command, Response>
    {
      internal const string ConfirmTitle = "Clear the xAI API key?";
      internal const string ConfirmMessage = "Ask stops working until a new key is saved.";
      internal const string ConfirmLabel = "Clear key";
      internal const string CancelLabel = "Cancel";

      private readonly IConfirmationService Confirmation;

      public Handler(
        IStore store,
        IWebServerApiService webServerApiService,
        ILogger<Handler> logger,
        IPublisher<ClientPipeline> publisher,
        IConfirmationService confirmation,
        IValidator<Command>? validator = null,
        AuthenticationStateProvider? authenticationStateProvider = null)
        : base(store, webServerApiService, logger, publisher, validator, authenticationStateProvider)
      {
        Confirmation = confirmation;
      }

      /// <inheritdoc />
      protected override async Task<bool> ConfirmAsync(Action action, CancellationToken cancellationToken)
      {
        _ = action;
        bool confirmed = await Confirmation.ConfirmAsync(
          ConfirmTitle,
          ConfirmMessage,
          ConfirmLabel,
          CancelLabel,
          cancellationToken);
        XaiApiKeyState.ClearDeclined = !confirmed;
        return confirmed;
      }

      /// <inheritdoc />
      protected override Task<Command?> GetRequest(Action action, CancellationToken cancellationToken)
      {
        _ = action;
        _ = cancellationToken;
        return Task.FromResult<Command?>(new Command());
      }

      /// <inheritdoc />
      protected override Task HandleSuccess(Response response, CancellationToken cancellationToken)
      {
        XaiApiKeyState.Apply(response);
        return Publisher.Publish(
          new OutcomeNotification(MessageBarIntent.Success, "xAI API key cleared."),
          cancellationToken);
      }

      /// <inheritdoc />
      protected override Task HandleError(SharedProblemDetails problem, CancellationToken cancellationToken)
      {
        XaiApiKeyState.SaveFailed = true;
        return base.HandleError(problem, cancellationToken);
      }
    }
  }
}

#region Purpose
// ClearXaiApiKey: removes the stored site key and refreshes the status.
#endregion

#region Design
// No key in the action. Opted out of the palette so a keystroke cannot clear the site key.
// SaveFailed covers a host-managed refusal.
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
      public Handler(
        IStore store,
        IWebServerApiService webServerApiService,
        ILogger<Handler> logger,
        IPublisher<ClientPipeline> publisher,
        IValidator<Command>? validator = null,
        AuthenticationStateProvider? authenticationStateProvider = null)
        : base(store, webServerApiService, logger, publisher, validator, authenticationStateProvider)
      {
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

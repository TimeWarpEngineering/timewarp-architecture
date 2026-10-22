#region Purpose
// SetXaiApiKey: sends a new key and stores only the returned status.
#endregion

#region Design
// The action holds the key for the request. HandleSuccess does not copy it into state. The
// palette is opted out so the secret is not a command argument. SaveFailed keeps the page from
// clearing the masked input after a problem.
#endregion

namespace TimeWarp.Architecture.Features.Admin.Ai;

using static SetXaiApiKey;

partial class XaiApiKeyState
{
  /// <summary>Sets or replaces the site key.</summary>
  public static class SetXaiApiKeyActionSet
  {
    /// <summary>The key to store. It is not kept on the state.</summary>
    [TrackAction]
    [CommandPaletteOptOut("Writes a secret from Admin → AI. The palette must not take the key.")]
    public sealed class Action : IBaseAction
    {
      /// <summary>Creates the action.</summary>
      public Action(string apiKey)
      {
        ApiKey = apiKey;
      }

      /// <summary>The key to send. Not stored after the response.</summary>
      public string ApiKey { get; }
    }

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
        _ = cancellationToken;
        return Task.FromResult<Command?>(new Command { ApiKey = action.ApiKey });
      }

      /// <inheritdoc />
      protected override Task HandleSuccess(Response response, CancellationToken cancellationToken)
      {
        XaiApiKeyState.Apply(response);
        return Publisher.Publish(
          new OutcomeNotification(MessageBarIntent.Success, "xAI API key saved."),
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

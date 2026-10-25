#region Purpose
// FetchUserXaiApiKeyStatus: loads the write-only personal key status into UserXaiApiKeyState.
#endregion

#region Design
// Page load. Not a palette command. Errors toast through DefaultApiHandler.
#endregion

namespace TimeWarp.Architecture.Features.Profiles;

using static GetUserXaiApiKeyStatus;

partial class UserXaiApiKeyState
{
  /// <summary>Loads the personal key status.</summary>
  public static class FetchUserXaiApiKeyStatusActionSet
  {
    /// <summary>No arguments.</summary>
    [TrackAction]
    [CommandPaletteOptOut("Page load for Profile. The status is not a command.")]
    public sealed class Action : IBaseAction;

    internal sealed class Handler : DefaultApiHandler<Action, Query, Response>
    {
      public Handler(
        IStore store,
        IWebServerApiService webServerApiService,
        ILogger<Handler> logger,
        IPublisher<ClientPipeline> publisher,
        IValidator<Query>? validator = null,
        AuthenticationStateProvider? authenticationStateProvider = null)
        : base(store, webServerApiService, logger, publisher, validator, authenticationStateProvider)
      {
      }

      /// <inheritdoc />
      protected override Task<Query?> GetRequest(Action action, CancellationToken cancellationToken)
      {
        _ = action;
        _ = cancellationToken;
        return Task.FromResult<Query?>(new Query());
      }

      /// <inheritdoc />
      protected override Task HandleSuccess(Response response, CancellationToken cancellationToken)
      {
        _ = cancellationToken;
        UserXaiApiKeyState.Apply(response);
        return Task.CompletedTask;
      }
    }
  }
}

#region Purpose
// FetchXaiApiKeyStatus: loads the write-only key status into XaiApiKeyState.
#endregion

#region Design
// Page load. Not a palette command. Errors toast through DefaultApiHandler.
#endregion

namespace TimeWarp.Architecture.Features.Admin.Ai;

using static GetXaiApiKeyStatus;

partial class XaiApiKeyState
{
  /// <summary>Loads the key status.</summary>
  public static class FetchXaiApiKeyStatusActionSet
  {
    /// <summary>No arguments.</summary>
    [TrackAction]
    [CommandPaletteOptOut("Page load for Admin → AI. The status is not a command.")]
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
        XaiApiKeyState.Apply(response);
        return Task.CompletedTask;
      }
    }
  }
}

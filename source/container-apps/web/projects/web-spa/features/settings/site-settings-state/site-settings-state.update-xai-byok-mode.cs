#region Purpose
// UpdateXaiByokMode: persists whose xAI key Ask uses and refreshes the cached mode and version.
#endregion

#region Design
// The action carries the Command (mode and Version). Every problem, including a 409, goes to the
// shell region via DefaultApiHandler's ProblemDetailsNotification. XaiByokModeSaveFailed is the
// page-facing flag so the AI page keeps the draft instead of reloading it. It is separate from
// SaveFailed, which belongs to the authentication policy save, and from AssistantNameSaveFailed.
// Success publishes "xAI key policy saved."
#endregion

namespace TimeWarp.Architecture.Features.Settings;

using static UpdateXaiByokMode;

partial class SiteSettingsState
{
  public static class UpdateXaiByokModeActionSet
  {
    [CatalogAction
    (
      Description = "Choose whose xAI API key Ask uses when the host has not set one.",
      Permissions = [PermissionIds.SettingsWrite],
      Visibility = ActionVisibility.Agent
    )]
    [TrackAction]
    public sealed class Action : IBaseAction
    {
      public Action(Command command)
      {
        Command = command;
      }

      public Command Command { get; }
    }

    internal sealed class Handler : DefaultApiHandler<Action, Command, Response>
    {
      public Handler
      (
        IStore store,
        IWebServerApiService webServerApiService,
        ILogger<Handler> logger,
        IPublisher<ClientPipeline> publisher
      ) : base(store, webServerApiService, logger, publisher)
      {
      }

      protected override Task<Command?> GetRequest(Action action, CancellationToken cancellationToken)
      {
        return Task.FromResult<Command?>(action.Command);
      }

      protected override Task HandleSuccess(Response response, CancellationToken cancellationToken)
      {
        SiteSettingsState.XaiByokMode = response.XaiByokMode;
        SiteSettingsState.Version = response.Version;
        SiteSettingsState.XaiByokModeSaveFailed = false;
        return Publisher.Publish
        (
          new OutcomeNotification(MessageBarIntent.Success, "xAI key policy saved."),
          cancellationToken
        );
      }

      protected override Task HandleError(SharedProblemDetails problem, CancellationToken cancellationToken)
      {
        SiteSettingsState.XaiByokModeSaveFailed = true;
        return base.HandleError(problem, cancellationToken);
      }
    }
  }
}

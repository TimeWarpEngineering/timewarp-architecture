#region Purpose
// UpdateAssistantName: persists the assistant name and refreshes the cached name and version.
#endregion

#region Design
// The action carries the Command (name and Version). Every problem, including a 409, goes to the
// shell region via DefaultApiHandler's ProblemDetailsNotification. AssistantNameSaveFailed is the
// page-facing flag so the AI page keeps the draft instead of reloading it. It is separate from
// SaveFailed, which belongs to the authentication policy save. Success publishes
// "Assistant name saved." The identity reload is AssistantIdentityRefreshHandler, not a second
// dispatch from this handler.
#endregion

namespace TimeWarp.Architecture.Features.Settings;

using static UpdateAssistantName;

partial class SiteSettingsState
{
  public static class UpdateAssistantNameActionSet
  {
    [CatalogAction
    (
      Description = "Rename the in-app assistant shown on the top-bar button, Ctrl-K, the panel and its greeting.",
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
        SiteSettingsState.AssistantName = response.AssistantName;
        SiteSettingsState.Version = response.Version;
        SiteSettingsState.AssistantNameSaveFailed = false;
        return Publisher.Publish
        (
          new OutcomeNotification(MessageBarIntent.Success, "Assistant name saved."),
          cancellationToken
        );
      }

      protected override Task HandleError(SharedProblemDetails problem, CancellationToken cancellationToken)
      {
        SiteSettingsState.AssistantNameSaveFailed = true;
        return base.HandleError(problem, cancellationToken);
      }
    }
  }
}

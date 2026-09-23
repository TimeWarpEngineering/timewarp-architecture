#region Purpose
// UpdateSiteSettings: persists site authentication policy via UpdateSiteSettings and refreshes state.
#endregion

#region Design
// Action carries the Command (Version included). Every problem (409 concurrency included)
// goes to the shell region via DefaultApiHandler's ProblemDetailsNotification; SaveFailed is
// the page-facing flag so the editor keeps the draft instead of reloading it. Success
// publishes "Authentication settings saved." (task 247).
#endregion

namespace TimeWarp.Architecture.Features.Settings;

using static UpdateSiteSettings;

partial class SiteSettingsState
{
  public static class UpdateSiteSettingsActionSet
  {
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
        SiteSettingsState.EntraSignInEnabled = response.EntraSignInEnabled;
        SiteSettingsState.EntraAllowBootstrap = response.EntraAllowBootstrap;
        SiteSettingsState.PasskeyPromptMode = response.PasskeyPromptMode;
        SiteSettingsState.Version = response.Version;
        SiteSettingsState.SaveFailed = false;
        return Publisher.Publish
        (
          new OutcomeNotification(MessageBarIntent.Success, "Authentication settings saved."),
          cancellationToken
        );
      }

      protected override Task HandleError(SharedProblemDetails problem, CancellationToken cancellationToken)
      {
        SiteSettingsState.SaveFailed = true;
        return base.HandleError(problem, cancellationToken);
      }
    }
  }
}

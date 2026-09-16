#region Purpose
// UpdateSiteSettings: persists site authentication policy via UpdateSiteSettings and refreshes state.
#endregion

#region Design
// Action carries the Command (Version included). 409 surfaces as SaveError so the editor can
// tell the admin to reload. Other problems toast via DefaultApiHandler.
#endregion

namespace TimeWarp.Architecture.Features.Settings;

using static UpdateSiteSettings;

partial class SiteSettingsState
{
  internal static class UpdateSiteSettingsActionSet
  {
    [TrackAction]
    internal sealed class Action : IBaseAction
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
        ILogger<Handler> logger
      ) : base(store, webServerApiService, logger)
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
        SiteSettingsState.EntraTrustedTenants = [.. response.EntraTrustedTenants];
        SiteSettingsState.PasskeyPromptMode = response.PasskeyPromptMode;
        SiteSettingsState.Version = response.Version;
        SiteSettingsState.SaveError = null;
        return Task.CompletedTask;
      }

      protected override Task HandleError(SharedProblemDetails problem, CancellationToken cancellationToken)
      {
        if (problem.Status == 409)
        {
          SiteSettingsState.SaveError = problem.Detail ?? problem.Title ?? "Concurrency conflict.";
          return Task.CompletedTask;
        }

        return base.HandleError(problem, cancellationToken);
      }
    }
  }
}

#region Purpose
// FetchSiteSettings: loads GetSiteSettings into SiteSettingsState.
#endregion

#region Design
// DefaultApiHandler owns transport + toast-on-error. Empty Query. AuthenticationStateListener
// fetches on sign-in so Required passkey mode is known without visiting Settings.
#endregion

namespace TimeWarp.Architecture.Features.Settings;

using static GetSiteSettings;

partial class SiteSettingsState
{
  internal static class FetchSiteSettingsActionSet
  {
    [TrackAction]
    internal sealed class Action : IBaseAction;

    internal sealed class Handler : DefaultApiHandler<Action, Query, Response>
    {
      public Handler
      (
        IStore store,
        IWebServerApiService webServerApiService,
        ILogger<Handler> logger,
        IValidator<Query>? validator = null,
        AuthenticationStateProvider? authenticationStateProvider = null
      ) : base(store, webServerApiService, logger, validator, authenticationStateProvider)
      {
      }

      protected override Task<Query?> GetRequest(Action action, CancellationToken cancellationToken)
      {
        return Task.FromResult<Query?>(new Query());
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
    }
  }
}

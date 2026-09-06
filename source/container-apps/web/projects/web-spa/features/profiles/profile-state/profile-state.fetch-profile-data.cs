#region Purpose
// FetchProfileDataActionSet: loads the current user's profile via the GetProfile API.
#endregion

#region Design
// Built on DefaultApiHandler so validation, auth token handling, and problem-details
// error reporting follow the shared API-call path; only HandleSuccess touches state,
// leaving failures to the common error pipeline.
// [TrackAction] exposes in-flight status so the UI can render a loading indicator while
// the fetch is pending.
// Task 148 D7 / 205: maps Alias/Email/Avatar plus Language/Region/Theme/Notifications from Response.
#endregion

namespace TimeWarp.Architecture.Features.Profiles;

using static GetProfile;

partial class ProfileState
{
  internal static class FetchProfileDataActionSet
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
      ) : base(store, webServerApiService, logger, validator, authenticationStateProvider) {}

      protected override Task<Query?> GetRequest(Action action, CancellationToken cancellationToken)
      {
        return Task.FromResult<Query?>(new Query());
      }
      protected override Task HandleSuccess(Response response, CancellationToken cancellationToken)
      {
        ProfileState.Alias = response.Alias;
        ProfileState.Email = response.Email;
        ProfileState.Avatar = response.Avatar;
        ProfileState.Language = response.Language;
        ProfileState.Region = response.Region;
        ProfileState.Theme = response.Theme;
        ProfileState.Notifications = response.Notifications;
        return Task.CompletedTask;
      }
    }
  }
}

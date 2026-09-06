#region Purpose
// UpdateProfileActionSet: persists progressive profile fields via the UpdateProfile API.
#endregion

#region Design
// Action ctor takes UpdateProfile.Command so the generator emits ProfileState.UpdateProfile(Command).
// ProfilePage binds IProfileDetails (the Command) and submits via that method (COPIC, TWA0022).
// On success the submitted fields are copied into state; Avatar is unchanged (GetProfile-only).
#endregion

namespace TimeWarp.Architecture.Features.Profiles;

using static UpdateProfile;

partial class ProfileState
{
  internal static class UpdateProfileActionSet
  {
    [TrackAction]
    internal sealed class Action : IBaseAction
    {
      public Command Command { get; }

      public Action(Command command)
      {
        Command = command;
      }
    }

    internal sealed class Handler
    (
      IStore store,
      IWebServerApiService webServerApiService,
      ILogger<Handler> logger
    ) : DefaultApiHandler<Action, Command, Response>(store, webServerApiService, logger)
    {
      protected override Task<Command?> GetRequest(Action action, CancellationToken cancellationToken) =>
        Task.FromResult<Command?>(action.Command);

      protected override Task HandleSuccess(Response response, CancellationToken cancellationToken)
      {
        ProfileState.Alias = response.Alias;
        ProfileState.Email = response.Email;
        ProfileState.Language = response.Language;
        ProfileState.Region = response.Region;
        ProfileState.Theme = response.Theme;
        ProfileState.Notifications = response.Notifications;
        return Task.CompletedTask;
      }
    }
  }
}

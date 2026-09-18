#region Purpose
// UpdateProfileActionSet: persists progressive profile fields via the UpdateProfile API.
#endregion

#region Design
// Action ctor takes UpdateProfile.Command so the generator emits ProfileState.UpdateProfile(Command).
// ProfilePage binds IProfileDetails (the Command) and submits via that method (COPIC, TWA0022).
// On success the submitted fields are copied into state; Avatar is unchanged (GetProfile-only).
// 401 (empty cookie challenge or unsigned-in Save): toast via DefaultApiHandler then
// /Login?returnUrl=/Profile — GET AllowAnonymous must not hide that Save needs a session.
// 403 stays a toast only (insufficient permission is not "sign in again").
#endregion

namespace TimeWarp.Architecture.Features.Profiles;

using Microsoft.AspNetCore.Components;
using static UpdateProfile;

partial class ProfileState
{
  public static class UpdateProfileActionSet
  {
    [TrackAction]
    public sealed class Action : IBaseAction
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
      ILogger<Handler> logger,
      IPublisher<ClientPipeline> publisher,
      NavigationManager navigationManager
    ) : DefaultApiHandler<Action, Command, Response>(store, webServerApiService, logger, publisher)
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

      protected override async Task HandleError(SharedProblemDetails problemDetails, CancellationToken cancellationToken)
      {
        await base.HandleError(problemDetails, cancellationToken);
        if (problemDetails.Status == 401)
        {
          navigationManager.NavigateTo($"/Login?returnUrl={Uri.EscapeDataString("/Profile")}");
        }
      }
    }
  }
}

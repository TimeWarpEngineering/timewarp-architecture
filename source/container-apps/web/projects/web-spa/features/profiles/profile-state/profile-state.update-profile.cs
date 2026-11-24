#region Purpose
// UpdateProfileActionSet: persists progressive profile fields via the UpdateProfile API.
#endregion

#region Design
// Action ctor takes UpdateProfile.Command so the generator emits ProfileState.UpdateProfile(Command).
// The Command implements IProfileDetails and ProfilePage lists this action, so FormPrefill
// derives the Prefill tool; ProfilePage wraps its form in FormPrefillScope for this action.
// ProfilePage binds IProfileDetails (the Command) and submits via that method (TWA0022).
// On success the stored snapshot becomes a GetProfile.Response. UpdateProfile.Response has no
// avatar, so the previous snapshot's avatar is kept. With no snapshot, FallbackAvatar is used.
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
    [CatalogAction
    (
      Description = "Save the signed-in user's profile fields.",
      Permissions = [PermissionIds.ProfileWrite],
      Visibility = ActionVisibility.Agent
    )]
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
        string avatar = ProfileState.Profile?.Avatar ?? ProfileState.FallbackAvatar;
        ProfileState.Profile = new GetProfile.Response(
          response.DisplayName,
          response.Email,
          response.Language,
          response.Region,
          response.Theme,
          response.Notifications,
          avatar);
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

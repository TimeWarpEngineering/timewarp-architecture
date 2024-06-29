namespace TimeWarp.Architecture.Features.Authorization;
using static GetCurrentUser;

internal partial class AuthorizationState
{
  public static class FetchCurrentUser
  {
    [TrackAction]
    public sealed class Action : IBaseAction;

    public sealed class Handler : DefaultApiHandler<Action,Query, Response>
    {
      public Handler
      (
        IStore store,
        IWebServerApiService webServerApiService,
        ISender sender
      ) : base(store, webServerApiService, sender) {}

      protected override Task<Query?> GetRequest(Action action, CancellationToken cancellationToken)
      {
        // Some logic to determine if the request should be skipped (use current state/cache)

        // return UseCache
        return AuthorizationState.RolesList == null
          ? Task.FromResult<Query?>(new Query())
          : Task.FromResult<Query?>(null);
      }
      protected override Task HandleSuccess(Response response, CancellationToken cancellationToken)
      {
        AuthorizationState.ModulesList = response.Modules;
        AuthorizationState.RolesList = response.Roles;
        return Task.CompletedTask;
      }
    }
  }
}

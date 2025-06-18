namespace TimeWarp.Architecture.Features.Authorization;
using static GetCurrentUser;

partial class AuthorizationState
{
  internal static class FetchCurrentUserActionSet
  {
    [TrackAction]
    internal sealed class Action : IBaseAction;

    internal sealed class Handler : DefaultApiHandler<Action, Query, Response>
    {
      private string? CacheKey { get; set; }
      public Handler
      (
        IStore store,
        IWebServerApiService webServerApiService,
        ISender sender,
        ILogger<Handler> logger
      ) : base(store, webServerApiService, sender, logger) {}

      protected override Task<Query?> GetRequest(Action action, CancellationToken cancellationToken)
      {
        CacheKey = GenerateCacheKey(action);

        // return UseCache
        return AuthorizationState.IsCacheValid(CacheKey)
          ? Task.FromResult<Query?>(null)
          : Task.FromResult<Query?>(new Query());
      }
      protected override Task HandleSuccess(Response response, CancellationToken cancellationToken)
      {
        AuthorizationState.ModulesList = response.Modules;
        AuthorizationState.RolesList = response.Roles;
        AuthorizationState.UpdateCacheKey(CacheKey!);
        return Task.CompletedTask;
      }
    }
  }
}

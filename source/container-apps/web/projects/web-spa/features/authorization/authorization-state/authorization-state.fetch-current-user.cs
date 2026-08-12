#region Purpose
// AuthorizationState action that loads the current user's role and permission grants (mock/Entra).
#endregion

#region Design
// Uses the DefaultApiHandler pipeline; returning a null Query from GetRequest is the cache
// short-circuit — no HTTP call while AuthorizationState's cache key is still valid.
// The cache key is updated only in HandleSuccess so failed fetches never extend validity.
// Invoked from AccountClaimsPrincipalFactoryWithRoles at Entra sign-in so permission claims
// exist before authorization policies evaluate (task 182-003). Identity-session path uses
// GetCurrentSession instead and does not need this action.
#endregion

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

        return AuthorizationState.IsCacheValid(CacheKey)
          ? Task.FromResult<Query?>(null)
          : Task.FromResult<Query?>(new Query());
      }
      protected override Task HandleSuccess(Response response, CancellationToken cancellationToken)
      {
        AuthorizationState.RolesList = response.Roles;
        AuthorizationState.PermissionsList = response.Permissions;
        AuthorizationState.UpdateCacheKey(CacheKey!);
        return Task.CompletedTask;
      }
    }
  }
}

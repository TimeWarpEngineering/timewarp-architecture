#region Purpose
// FetchRoles action: loads GetRoles into RoleState for the admin list page.
#endregion

#region Design
// DefaultApiHandler pattern (WeatherForecasts): only Query mapping + success mutation live here.
// Seeds DraftPermissionIds from each RoleDto.PermissionIds so the membership matrix matches
// the server (task 182-004).
#endregion

namespace TimeWarp.Architecture.Features.Admin.Roles;

using static GetRoles;

partial class RoleState
{
  public static class FetchRolesActionSet
  {
    [TrackAction]
    internal sealed class Action : IBaseAction;

    internal class Handler
    (
      IStore store,
      IWebServerApiService webServerApiService,
      ILogger<Handler> logger
    ) : DefaultApiHandler<Action, Query, Response>(store, webServerApiService, logger)
    {
      protected override Task<Query?> GetRequest(Action action, CancellationToken cancellationToken) =>
        Task.FromResult<Query?>(new Query { UserId = Guid.NewGuid() });

      protected override Task HandleSuccess(Response response, CancellationToken cancellationToken)
      {
        RoleState.RolesList = [.. response.Items];
        RoleState.DraftPermissionIds = response.Items.ToDictionary(
          static dto => dto.RoleId,
          static dto => new HashSet<string>(dto.PermissionIds ?? [], StringComparer.Ordinal));
        return Task.CompletedTask;
      }
    }
  }
}

#region Purpose
// FetchRoles action: loads GetRoles into RoleState for the admin list page.
#endregion

#region Design
// DefaultApiHandler pattern (WeatherForecasts): only Query mapping + success mutation live here.
// Seeds DraftPermissionIds from each RoleDto.Permissions so the detail editor matches
// the server (task 182-004 / 206). DraftAgentScopes is seeded the same way (task 466).
#endregion

namespace TimeWarp.Architecture.Features.Admin.Roles;

using static GetRoles;

partial class RoleState
{
  public static class FetchRolesActionSet
  {
    [TrackAction]
    [CommandPaletteOptOut("Page/startup data load; the owning page or shell runs it, a person never needs to.")]
    public sealed class Action : IBaseAction;

    internal class Handler
    (
      IStore store,
      IWebServerApiService webServerApiService,
      ILogger<Handler> logger,
      IPublisher<ClientPipeline> publisher
    ) : DefaultApiHandler<Action, Query, Response>(store, webServerApiService, logger, publisher)
    {
      protected override Task<Query?> GetRequest(Action action, CancellationToken cancellationToken) =>
        Task.FromResult<Query?>(new Query { UserId = Guid.NewGuid() });

      protected override Task HandleSuccess(Response response, CancellationToken cancellationToken)
      {
        RoleState.RolesList = [.. response.Items];
        RoleState.DraftPermissionIds = response.Items.ToDictionary(
          static dto => dto.RoleId,
          static dto => new HashSet<string>(dto.Permissions ?? [], StringComparer.Ordinal));
        RoleState.DraftAgentScopes = response.Items.ToDictionary(
          static dto => dto.RoleId,
          static dto => new HashSet<string>(dto.AgentScopes ?? [], StringComparer.Ordinal));
        return Task.CompletedTask;
      }
    }
  }
}

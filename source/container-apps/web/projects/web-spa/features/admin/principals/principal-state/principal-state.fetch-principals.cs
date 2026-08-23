#region Purpose
// FetchPrincipals action: loads ListPrincipals into PrincipalState and seeds role drafts.
#endregion

namespace TimeWarp.Architecture.Features.Admin.Principals;

using static ListPrincipals;

partial class PrincipalState
{
  public static class FetchPrincipalsActionSet
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
        PrincipalState.PrincipalsList = [.. response.Items];
        PrincipalState.DraftRoleIds = response.Items.ToDictionary(
          static item => item.PrincipalId.Value,
          static item => item.RoleIds.ToHashSet());
        return Task.CompletedTask;
      }
    }
  }
}

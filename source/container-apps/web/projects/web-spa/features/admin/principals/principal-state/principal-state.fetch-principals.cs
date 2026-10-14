#region Purpose
// FetchPrincipals action: loads ListPrincipals into PrincipalState using the current search and sort.
#endregion

#region Design
// The request reads Search, SortBy, and SortDescending from state, so the action carries no
// parameters. [TrackAction] drives the grid's Loading state. A failed fetch (including the 403
// for a profile search without admin.profiles.read) clears the rows and sets ListLoadError, then
// still publishes the problem through the base handler.
#endregion

namespace TimeWarp.Architecture.Features.Admin.Principals;

using static ListPrincipals;

partial class PrincipalState
{
  public static class FetchPrincipalsActionSet
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
        Task.FromResult<Query?>(new Query
        {
          UserId = Guid.NewGuid(),
          Search = PrincipalState.Search,
          SortBy = PrincipalState.SortBy,
          SortDescending = PrincipalState.SortDescending,
        });

      protected override Task HandleSuccess(Response response, CancellationToken cancellationToken)
      {
        PrincipalState.PrincipalsList = [.. response.Items];
        PrincipalState.ListLoadError = null;
        return Task.CompletedTask;
      }

      protected override async Task HandleError(SharedProblemDetails problemDetails, CancellationToken cancellationToken)
      {
        PrincipalState.PrincipalsList = null;
        PrincipalState.ListLoadError = "The principals list could not be loaded.";
        await base.HandleError(problemDetails, cancellationToken);
      }
    }
  }
}

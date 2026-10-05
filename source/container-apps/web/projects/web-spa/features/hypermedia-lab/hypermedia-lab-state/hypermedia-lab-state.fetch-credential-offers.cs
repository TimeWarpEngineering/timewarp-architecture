#region Purpose
// FetchCredentialOffers: loads approach B's payload (credentials + offered catalog actions) into HypermediaLabState.
#endregion

#region Design
// DefaultApiHandler owns transport and problem → shell notification. Cataloged (Visibility Agent,
// parameterless) for one reason: it is the FollowUpTarget of every B contextual row, so the runner
// that executed an offered action can refresh the payload through the catalog without a handler
// dispatching another action (TWS0002). Agent visibility keeps it out of the static Ctrl-K roster;
// an agent may call it to read the same offers (task 271).
#endregion

namespace TimeWarp.Architecture.Features.HypermediaLab;

using static GetCredentialOffers;

partial class HypermediaLabState
{
  public static class FetchCredentialOffersActionSet
  {
    public const string CatalogName = "HypermediaLab.FetchCredentialOffers";

    [CatalogAction
    (
      Description = "Refresh the hypermedia lab's server-offered credential actions (approach B).",
      Permissions = [PermissionIds.CredentialManageSelf],
      Visibility = ActionVisibility.Agent
    )]
    [TrackAction]
    public sealed class Action : IBaseAction;

    internal sealed class Handler : DefaultApiHandler<Action, Query, Response>
    {
      public Handler
      (
        IStore store,
        TimeWarp.Architecture.Services.IWebServerApiService webServerApiService,
        ILogger<Handler> logger,
        IPublisher<ClientPipeline> publisher,
        AuthenticationStateProvider authenticationStateProvider
      ) : base(store, webServerApiService, logger, publisher, authenticationStateProvider: authenticationStateProvider) { }

      protected override Task<Query?> GetRequest(Action action, CancellationToken cancellationToken) =>
        Task.FromResult<Query?>(new Query());

      protected override Task HandleSuccess(Response response, CancellationToken cancellationToken)
      {
        HypermediaLabState.Offers = response;
        return Task.CompletedTask;
      }
    }
  }
}

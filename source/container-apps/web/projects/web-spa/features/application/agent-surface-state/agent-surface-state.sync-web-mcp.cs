#region Purpose
// Republishes the current page's WebMCP tools.
#endregion

#region Design
// The shell dispatches this on the first interactive render and on location changes.
// The publisher performs the tool-list JS call. This handler then asks PageSurfaceJsModule for
// the page-body summary and stores it with the route it was read on (PageAgentRoute). A missing
// document or a disconnected circuit clears the summary; page_context still has title and purpose.
// On a location change the new page may not have rendered yet, so this copy can lag; the
// page_context tool walks the page again when it is called and only falls back to this copy
// when that walk returns nothing. A component
// does not call either JS module (TWA0026). Cancelling a pending WebMCP call on navigation is
// not done here: the dispatcher listens to LocationChanged for the life of its own wait.
#endregion

namespace TimeWarp.Architecture.Features.Applications;

partial class AgentSurfaceState
{
  public static class SyncWebMcpActionSet
  {
    public sealed class Action : IBaseAction;
    internal sealed class Handler
    (
      IStore store,
      WebMcpPublisher publisher,
      IJSRuntime jsRuntime,
      PageAgentRoute route,
      NavigationManager navigation
    ) : BaseHandler<Action>(store)
    {
      public override async ValueTask Handle(Action action, CancellationToken cancellationToken)
      {
        await publisher.PublishAsync(cancellationToken);
        string? json = await PageSurfaceJsModule.TrySummarizeAsync(jsRuntime, cancellationToken);
        AgentSurfaceState.PageSurfacePath = json is null ? null : route.PathOr(navigation);
        AgentSurfaceState.PageSurfaceJson = json;
      }
    }
  }
}

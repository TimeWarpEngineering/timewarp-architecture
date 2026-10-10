#region Purpose
// Republishes the current page's WebMCP tools.
#endregion

#region Design
// The shell dispatches this on the first interactive render and on location changes.
// The publisher performs the tool-list JS call. This handler then asks PageSurfaceJsModule for
// the page-body summary and stores it. A missing document or a disconnected circuit leaves the
// previous summary unset for this call; page_context still has title and purpose. A component
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
      IJSRuntime jsRuntime
    ) : BaseHandler<Action>(store)
    {
      public override async ValueTask Handle(Action action, CancellationToken cancellationToken)
      {
        await publisher.PublishAsync(cancellationToken);
        try
        {
          string? json = await PageSurfaceJsModule.SummarizeAsync(jsRuntime, cancellationToken);
          AgentSurfaceState.PageSurfaceJson = json;
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
          // The test host and a disconnected circuit have no document. Title and purpose remain.
        }
      }
    }
  }
}

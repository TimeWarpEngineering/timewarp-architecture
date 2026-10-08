#region Purpose
// Republishes the current page's WebMCP tools.
#endregion

#region Design
// The shell dispatches this on the first interactive render and on location changes.
// The publisher performs the JS call. A component does not.
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
      WebMcpPublisher publisher
    ) : BaseHandler<Action>(store)
    {
      public override async ValueTask Handle(Action action, CancellationToken cancellationToken)
      {
        await publisher.PublishAsync(cancellationToken);
      }
    }
  }
}

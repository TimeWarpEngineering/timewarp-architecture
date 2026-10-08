#region Purpose
// Answers the pending WebMCP confirmation and lets the waiting tool call finish.
#endregion

#region Design
// The gate continuation is asynchronous, so a following Execute is not nested inside this handler.
#endregion

namespace TimeWarp.Architecture.Features.Applications;

partial class AgentSurfaceState
{
  public static class ResolveApprovalActionSet
  {
    public sealed class Action : IBaseAction
    {
      public bool Approved { get; }

      public Action(bool approved)
      {
        Approved = approved;
      }
    }

    internal sealed class Handler
    (
      IStore store,
      WebMcpApprovalGate gate
    ) : BaseHandler<Action>(store)
    {
      public override ValueTask Handle(Action action, CancellationToken cancellationToken)
      {
        AgentSurfaceState.PendingToolName = null;
        AgentSurfaceState.PendingArgumentsJson = null;
        gate.Complete(action.Approved);
        return default;
      }
    }
  }
}

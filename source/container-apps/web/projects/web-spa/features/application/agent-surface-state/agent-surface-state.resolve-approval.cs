#region Purpose
// Answers one pending WebMCP confirmation, by call id, and lets that tool call finish.
#endregion

#region Design
// The id is the one the banner rendered, so a click answers the call the person saw. A stale id
// (the call was already answered or cancelled by navigation) changes nothing. The dispatcher also
// sends ResolveApproval(id, false) after every wait; that is a no-op unless navigation cancelled
// the call while its banner was still showing, in which case it clears the banner.
// The gate continuation is asynchronous, so a following Execute is not nested inside this handler.
#endregion

namespace TimeWarp.Architecture.Features.Applications;

partial class AgentSurfaceState
{
  public static class ResolveApprovalActionSet
  {
    public sealed class Action : IBaseAction
    {
      public Guid CallId { get; }

      public bool Approved { get; }

      public Action(Guid callId, bool approved)
      {
        CallId = callId;
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
        if (AgentSurfaceState.PendingCallId == action.CallId)
        {
          AgentSurfaceState.PendingCallId = null;
          AgentSurfaceState.PendingToolName = null;
          AgentSurfaceState.PendingArgumentsJson = null;
        }

        gate.Complete(action.CallId, action.Approved);
        return default;
      }
    }
  }
}

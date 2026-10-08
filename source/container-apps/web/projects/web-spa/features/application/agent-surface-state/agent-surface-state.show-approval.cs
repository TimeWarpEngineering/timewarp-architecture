#region Purpose
// Records a WebMCP tool call that is waiting for the person to confirm it.
#endregion

#region Design
// ArgumentsJson is the dispatcher's canonical rendering of the already-bound parameter values,
// not the caller's raw JSON, so the banner shows exactly what Execute will receive.
#endregion

namespace TimeWarp.Architecture.Features.Applications;

partial class AgentSurfaceState
{
  public static class ShowApprovalActionSet
  {
    public sealed class Action : IBaseAction
    {
      public Guid CallId { get; }

      public string ToolName { get; }

      public string ArgumentsJson { get; }

      public Action(Guid callId, string toolName, string argumentsJson)
      {
        CallId = callId;
        ToolName = toolName;
        ArgumentsJson = argumentsJson;
      }
    }

    internal sealed class Handler
    (
      IStore store
    ) : BaseHandler<Action>(store)
    {
      public override ValueTask Handle(Action action, CancellationToken cancellationToken)
      {
        AgentSurfaceState.PendingCallId = action.CallId;
        AgentSurfaceState.PendingToolName = action.ToolName;
        AgentSurfaceState.PendingArgumentsJson = action.ArgumentsJson;
        return default;
      }
    }
  }
}

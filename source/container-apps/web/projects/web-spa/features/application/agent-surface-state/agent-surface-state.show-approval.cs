#region Purpose
// Records a WebMCP tool call that is waiting for the person to confirm it.
#endregion

namespace TimeWarp.Architecture.Features.Applications;

partial class AgentSurfaceState
{
  public static class ShowApprovalActionSet
  {
    public sealed class Action : IBaseAction
    {
      public string ToolName { get; }

      public string ArgumentsJson { get; }

      public Action(string toolName, string argumentsJson)
      {
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
        AgentSurfaceState.PendingToolName = action.ToolName;
        AgentSurfaceState.PendingArgumentsJson = action.ArgumentsJson;
        return default;
      }
    }
  }
}

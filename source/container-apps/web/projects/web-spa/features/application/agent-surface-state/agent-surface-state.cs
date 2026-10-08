#region Purpose
// Shell state for a WebMCP confirmation the person has not answered yet.
#endregion

#region Design
// One pending call at a time, identified by PendingCallId (the WebMcpApprovalGate id). The banner
// shows the tool and the bound argument values the dispatcher will execute, and answers with the
// id it rendered. ResolveApproval clears the state only for the matching id and releases that call
// in the gate. SyncWebMcp republishes the page's tools after navigation; the dispatcher itself
// cancels a pending call when the location changes.
#endregion

namespace TimeWarp.Architecture.Features.Applications;

[StateAccess]
public sealed partial class AgentSurfaceState : State<AgentSurfaceState>
{
  public Guid? PendingCallId { get; private set; }

  public string? PendingToolName { get; private set; }

  public string? PendingArgumentsJson { get; private set; }

  public bool HasPendingApproval => PendingCallId is not null;

  public AgentSurfaceState() { }

  public override void Initialize()
  {
    PendingCallId = null;
    PendingToolName = null;
    PendingArgumentsJson = null;
  }
}

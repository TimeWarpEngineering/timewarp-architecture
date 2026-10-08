#region Purpose
// Shell state for a WebMCP confirmation the person has not answered yet.
#endregion

#region Design
// One pending tool at a time. The banner reads this state. ResolveApproval clears it and
// releases the dispatcher. SyncWebMcp republishes the page's tools after navigation.
#endregion

namespace TimeWarp.Architecture.Features.Applications;

[StateAccess]
public sealed partial class AgentSurfaceState : State<AgentSurfaceState>
{
  public string? PendingToolName { get; private set; }

  public string? PendingArgumentsJson { get; private set; }

  public bool HasPendingApproval => PendingToolName is not null;

  public AgentSurfaceState() { }

  public override void Initialize()
  {
    PendingToolName = null;
    PendingArgumentsJson = null;
  }
}

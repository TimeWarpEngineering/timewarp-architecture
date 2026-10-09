#region Purpose
// Shell state for a WebMCP confirmation and for whether Ask can call a model.
#endregion

#region Design
// One pending call at a time, identified by PendingCallId (the WebMcpApprovalGate id). The banner
// shows the tool and the bound argument values the dispatcher will execute, and answers with the
// id it rendered. ResolveApproval clears the state only for the matching id and releases that call
// in the gate. SyncWebMcp republishes the page's tools after navigation; the dispatcher itself
// cancels a pending call when the location changes.
// ChatReadiness is the task 289 probe. It starts Unknown. LoadChatConfiguration sets Configured
// or NotConfigured and stores the setup command. Ask renders from this, not from IChatClient.
#endregion

namespace TimeWarp.Architecture.Features.Applications;

[StateAccess]
public sealed partial class AgentSurfaceState : State<AgentSurfaceState>
{
  public Guid? PendingCallId { get; private set; }

  public string? PendingToolName { get; private set; }

  public string? PendingArgumentsJson { get; private set; }

  public bool HasPendingApproval => PendingCallId is not null;

  public CatalogAgentReadiness ChatReadiness { get; private set; }

  public string ChatSetupCommand { get; private set; } = XaiChatDefaults.SetupCommand;

  public string? ChatModel { get; private set; }

  public bool ChatProbeCompleted { get; private set; }

  public AgentSurfaceState() { }

  public override void Initialize()
  {
    PendingCallId = null;
    PendingToolName = null;
    PendingArgumentsJson = null;
    ChatReadiness = CatalogAgentReadiness.Unknown;
    ChatSetupCommand = XaiChatDefaults.SetupCommand;
    ChatModel = null;
    ChatProbeCompleted = false;
  }
}

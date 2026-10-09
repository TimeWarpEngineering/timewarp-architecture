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
// The docked panel, its edit mode, and its conversation credential live here because TimeWarpPage
// remounts on navigation. The transcript does not: it is AskConversationThreads, keyed by
// ConversationGeneration. EditMode is shared by the in-app chat and WebMCP; closing the panel
// resets it to AskBeforeEditing. Listing tools does not read the credential. New conversation
// clears it and bumps ConversationGeneration so the panel rebuilds on an empty thread.
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

  public bool IsPanelOpen { get; private set; }

  public bool IsPanelExpanded { get; private set; }

  public AgentEditMode EditMode { get; private set; }

  public Guid? ConversationId { get; private set; }

  public Guid? ConversationPrincipalId { get; private set; }

  public string[] ConversationScopes { get; private set; } = [];

  public DateTimeOffset? ConversationExpiresAt { get; private set; }

  public string? ConversationDisplayName { get; private set; }

  public int ConversationGeneration { get; private set; }

  public bool RecordChats { get; private set; }

  public string? PrivacyNotice { get; private set; } = XaiChatDefaults.PrivacyNotice;

  [SuppressMessage(
    "Design",
    "CA1056:URI-like properties should not be strings",
    Justification = "SupportUrl is a relative app path or an absolute href, copied as text into the anchor.")]
  public string SupportUrl { get; private set; } = XaiChatDefaults.SupportUrl;

  public int CredentialLifetimeMinutes { get; private set; } = XaiChatDefaults.CredentialLifetimeMinutes;

  public bool PrivacyNoticeDismissed { get; private set; }

  /// <summary>The current conversation credential, or null when the session has not minted one.</summary>
  public AgentConversationCredential? Conversation
  {
    get
    {
      if (ConversationId is null || ConversationPrincipalId is null || ConversationExpiresAt is null)
      {
        return null;
      }

      return new AgentConversationCredential(
        ConversationId.Value,
        ConversationPrincipalId.Value,
        ConversationScopes.ToArray(),
        ConversationDisplayName ?? AgentConversationCredentialIssuer.DefaultDisplayName,
        ConversationExpiresAt.Value);
    }
  }

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
    IsPanelOpen = false;
    IsPanelExpanded = false;
    EditMode = AgentEditMode.AskBeforeEditing;
    ConversationId = null;
    ConversationPrincipalId = null;
    ConversationScopes = [];
    ConversationExpiresAt = null;
    ConversationDisplayName = null;
    ConversationGeneration = 0;
    RecordChats = false;
    PrivacyNotice = XaiChatDefaults.PrivacyNotice;
    SupportUrl = XaiChatDefaults.SupportUrl;
    CredentialLifetimeMinutes = XaiChatDefaults.CredentialLifetimeMinutes;
    PrivacyNoticeDismissed = false;
  }
}

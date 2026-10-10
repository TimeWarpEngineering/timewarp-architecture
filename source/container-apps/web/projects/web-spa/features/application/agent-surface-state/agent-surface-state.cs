#region Purpose
// Shell state for a WebMCP confirmation and for whether Ask can call a model.
#endregion

#region Design
// One pending call at a time, identified by PendingCallId (the WebMcpApprovalGate id). The banner
// shows the tool and the bound argument values the dispatcher will execute, and answers with the
// id it rendered. ResolveApproval clears the state only for the matching id and releases that call
// in the gate. SyncWebMcp republishes the page's tools after navigation and stores the page-body
// summary in PageSurfaceJson with the route it describes in PageSurfacePath. The dispatcher cancels a pending page-bound call when the location
// changes. PageSurfaceJson stays null until that walk succeeds.
// ChatReadiness is the task 289 probe. It starts Unknown. LoadChatConfiguration maps the outcome
// through ChatReadinessProbe (task 293): Configured, NotConfigured (server says no key),
// Unauthenticated (401) or Error, and ChatProblem keeps the Error text. AuthenticationStateListener
// re-runs the probe on every sign-in and sign-out. Ask renders from this, not from IChatClient.
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

  public string? ChatProblem { get; private set; }

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

  /// <summary>Bounded JSON from the page-body walk, or null when the walk has not run.</summary>
  public string? PageSurfaceJson { get; private set; }

  /// <summary>The normalized route PageSurfaceJson was read on, or null with no summary.</summary>
  public string? PageSurfacePath { get; private set; }

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
    ChatProblem = null;
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
    PageSurfaceJson = null;
    PageSurfacePath = null;
  }
}

#region Purpose
// Owns the live Ask agent for one circuit, so a TimeWarpPage remount does not cancel the turn.
#endregion

#region Design
// AskConversationThreads already outlives the panel. This session is the matching owner for the
// running agent: UIAgent, AgentContext, the tag client (and the screenshot and tool-invoking
// clients it disposes), CatalogAgentFunctions, and the path, edit mode, and generation that set
// was built for. AgentAsk renders that context and subscribes to its status. Dispose of the panel
// only drops that subscription. The panel calls Release when it unmounts because the person closed
// it (IsPanelOpen is already false). A route change leaves the panel open, so the outgoing panel
// does not release, and the page that just mounted attaches to the same AgentContext.
// EnsureAsync rebuilds when the path, edit mode, or conversation generation differs. A turn that
// is streaming or waiting on approval defers a path or edit-mode rebuild until Idle or Error; the
// status callback renders the panel again and EnsureAsync runs. New conversation changes the
// generation and does not wait: the build releases the previous agent. Readiness leaving
// Configured releases it too. Dispose, which the circuit runs when the scope ends, releases it.
// The tool set stays the one captured at the start of the turn. CatalogAgentFunction handles a
// tool that was an off-page offer and, after this turn's navigation, now requires approval: it
// returns ArrivalNeedsApprovalError and does not run. The model is told to finish the turn. The
// idle rebuild then offers that tool wrapped, so the next turn can ask. In Automatically edit the
// re-selected tool does not require approval, so the same unwrapped function runs the prefill.
// BindUi lets the tag and screenshot clients dispatch MarkSent on the renderer while a panel is
// mounted. A gap between panels calls the action directly. The token keeps an old panel from
// clearing a newer panel's binding.
// Development breadcrumbs (agent-built, agent-deferred, status) are recorded here, once per
// distinct reason and status-plus-path, so a remount does not drop the Idle line or repeat a
// build line the outgoing panel already wrote.
#endregion

namespace TimeWarp.Architecture.Features.Applications;

using Microsoft.AspNetCore.Components.AI;
using Microsoft.AspNetCore.Components.Authorization;

/// <summary>The live Ask agent for the current circuit.</summary>
public sealed class AskAgentSession : IDisposable
{
  private readonly IServiceProvider Services;
  private readonly IActionCatalog ActionCatalog;
  private readonly NavigationManager NavigationManager;
  private readonly IJSRuntime JsRuntime;
  private readonly PageAgentRoute PageAgentRoute;
  private readonly IStore Store;
  private readonly AskConversationThreads Threads;
  private readonly IAuthorizationService AuthorizationService;

  private AskTagChatClient? TagClient;
  private CatalogAgentFunctions? Functions;
  private IDisposable? StatusRegistration;
  private bool Building;
  private bool Disposed;
  private readonly List<Action> Listeners = [];
  private int UiToken;
  private Func<Func<Task>, Task>? UiInvoke;
  private string? DeferralKey;
  private ConversationStatus? ReportedStatus;
  private string? ReportedStatusPath;

  public AskAgentSession
  (
    IServiceProvider services,
    IActionCatalog actionCatalog,
    NavigationManager navigationManager,
    IJSRuntime jsRuntime,
    PageAgentRoute pageAgentRoute,
    IStore store,
    AskConversationThreads threads,
    IAuthorizationService authorizationService
  )
  {
    Services = services;
    ActionCatalog = actionCatalog;
    NavigationManager = navigationManager;
    JsRuntime = jsRuntime;
    PageAgentRoute = pageAgentRoute;
    Store = store;
    Threads = threads;
    AuthorizationService = authorizationService;
  }

  /// <summary>Runs <paramref name="onChanged"/> when the live agent is replaced or released.</summary>
  public IDisposable Listen(Action onChanged)
  {
    ArgumentNullException.ThrowIfNull(onChanged);
    Listeners.Add(onChanged);
    return new ListenerRegistration(this, onChanged);
  }

  public UIAgent? Agent { get; private set; }

  public AgentContext? Conversation { get; private set; }

  public AskConversationThread? Thread { get; private set; }

  public string? BuiltPath { get; private set; }

  public AgentEditMode BuiltMode { get; private set; }

  public int BuiltGeneration { get; private set; } = -1;

  public IReadOnlyList<AITool> BuiltTools => Functions?.Tools ?? [];

  public bool TurnInProgress =>
    Conversation?.Status is ConversationStatus.Streaming or ConversationStatus.AwaitingInput;

  /// <summary>Runs <paramref name="invoke"/> for tag and screenshot mark calls while this panel is mounted.</summary>
  public int BindUi(Func<Func<Task>, Task> invoke)
  {
    ArgumentNullException.ThrowIfNull(invoke);
    UiInvoke = invoke;
    return ++UiToken;
  }

  /// <summary>Drops the binding from <see cref="BindUi"/> when <paramref name="token"/> is still current.</summary>
  public void UnbindUi(int token)
  {
    if (token == UiToken)
    {
      UiInvoke = null;
    }
  }

  /// <summary>Builds or keeps the agent for the route, edit mode, and generation now showing.</summary>
  public async Task EnsureAsync()
  {
    if (Disposed)
    {
      return;
    }

    string path = CurrentPath();
    AgentSurfaceState surface = Store.GetState<AgentSurfaceState>();
    AgentEditMode mode = surface.EditMode;
    int generation = surface.ConversationGeneration;
    if (CatalogAgentAvailability.Readiness(surface) != CatalogAgentReadiness.Configured)
    {
      ReportDeferred("Readiness", path, mode, generation);
      if (Agent is not null)
      {
        ReleaseCore();
        NotifyChanged();
      }

      return;
    }

    if (Agent is not null && BuiltPath == path && BuiltMode == mode && BuiltGeneration == generation)
    {
      return;
    }

    // A rebuild disposes the AgentContext, which cancels the stream and orphans an approval card.
    // Wait for the turn to finish; the status callback re-renders and this runs again. New
    // conversation (a generation change) abandons the turn on purpose, so it does not wait.
    if (Building)
    {
      ReportDeferred("Building", path, mode, generation);
      return;
    }

    if (TurnInProgress && BuiltGeneration == generation)
    {
      ReportDeferred("TurnInProgress", path, mode, generation);
      return;
    }

    IChatClient? chatClient = Services.GetService<IChatClient>();
    if (chatClient is null)
    {
      ReportDeferred("Readiness", path, mode, generation);
      return;
    }

    Building = true;
    try
    {
      // Re-read on every rebuild. The session does not hold the provider, so a principal change
      // is visible the next time the tool set is selected.
      AuthenticationStateProvider authenticationStateProvider =
        Services.GetRequiredService<AuthenticationStateProvider>();
      AuthenticationState authentication = await authenticationStateProvider.GetAuthenticationStateAsync();
      if (Disposed)
      {
        return;
      }

      IReadOnlyList<CatalogAgentTool> selected = await CatalogAgentToolSet.SelectAsync
      (
        authentication.User,
        AuthorizationService,
        ActionCatalog.Entries,
        path,
        mode,
        CancellationToken.None
      );
      string siteMap = AgentSiteMap.Summary
      (
        await AgentSiteMap.DestinationsAsync(authentication.User, AuthorizationService, path),
        AgentAskInstructions.SiteMapCap
      );
      if (Disposed)
      {
        return;
      }

      ReleaseCore();
      CatalogAgentFunctions? functions = null;
      FunctionInvokingChatClient? invokingClient = null;
      AskScreenshotChatClient? screenshotClient = null;
      AskTagChatClient? tagClient = null;
      UIAgent? agent = null;
      AgentContext? conversation = null;
      try
      {
        functions = CatalogAgentFunctions.Create(selected);
        List<string> toolNames = [];
        List<string> prefillTools = [];
        foreach (CatalogAgentTool tool in selected)
        {
          toolNames.Add(tool.Name);
          if (tool.Name.StartsWith(FormPrefill.Prefix, StringComparison.Ordinal))
          {
            prefillTools.Add(tool.Name);
          }
        }

        toolNames.Add(PageAgentContext.ToolName);
        // Rebuilt on every request (AskTagChatClient), so page_context.tags names the tags sent so far
        // and the first line names the assistant's current name, even after a rename mid-conversation.
        string RenderInstructions()
        {
          string assistantName = Store.GetState<AgentSurfaceState>().AssistantName;
          return AgentAskInstructions.For
          (
            assistantName,
            siteMap,
            PageAgentContext.Describe
            (
              Store,
              path,
              toolNames,
              liveSurfaceJson: null,
              AgentAskInstructions.ContextCapFor(assistantName, siteMap)
            )
          );
        }

        invokingClient = CatalogAgentSession.CreateInvokingClient(chatClient, Services);
        screenshotClient = new AskScreenshotChatClient
        (
          invokingClient,
          () => Store.GetState<ScreenshotState>().AskAttachedFor(Store.GetState<AgentSurfaceState>().ConversationGeneration),
          id => InvokeUi(async () => await Store.GetState<ScreenshotState>().MarkAskSent(id))
        );
        tagClient = new AskTagChatClient
        (
          screenshotClient,
          () => Store.GetState<AskTagState>().AttachedFor(Store.GetState<AgentSurfaceState>().ConversationGeneration),
          ids => InvokeUi(async () => await Store.GetState<AskTagState>().MarkSent(ids)),
          RenderInstructions
        );
        AskConversationThread thread = Threads.For(generation);
        agent = new UIAgent
        (
          tagClient,
          options =>
          {
            options.ChatOptions = new ChatOptions
            {
              Tools = [.. functions.Tools],
              Instructions = RenderInstructions(),
            };
            options.Thread = thread;
          }
        );
        conversation = new AgentContext(agent);
        // Restore before MessageList attaches: it reads Turns once, when its AgentContext changes.
        await conversation.RestoreAsync();
        if (Disposed)
        {
          return;
        }

        Agent = agent;
        agent = null;
        Conversation = conversation;
        conversation = null;
        Thread = thread;
        TagClient = tagClient;
        tagClient = null;
        // TagClient owns the screenshot client, which owns the invoking client.
        screenshotClient = null;
        invokingClient = null;
        Functions = functions;
        functions = null;
        StatusRegistration = Conversation.RegisterOnStatusChanged(OnStatusChanged);
        BuiltPath = path;
        BuiltMode = mode;
        BuiltGeneration = generation;
        ReportBuilt(path, mode, generation, toolNames.Count, prefillTools);
        NotifyChanged();
      }
      finally
      {
        conversation?.Dispose();
        agent?.Dispose();
        tagClient?.Dispose();
        screenshotClient?.Dispose();
        invokingClient?.Dispose();
        functions?.Dispose();
      }
    }
    finally
    {
      Building = false;
    }
  }

  /// <summary>Drops the live agent. The panel calls this when it unmounts because the person closed it.</summary>
  public void Release()
  {
    bool hadAgent = Agent is not null || Conversation is not null;
    ReleaseCore();
    if (hadAgent)
    {
      NotifyChanged();
    }
  }

  public void Dispose()
  {
    if (Disposed)
    {
      return;
    }

    Disposed = true;
    ReleaseCore();
  }

  private void OnStatusChanged(ConversationStatus status) => NoteStatus(status, CurrentPath());

  private void ReleaseCore()
  {
    StatusRegistration?.Dispose();
    StatusRegistration = null;
    Conversation?.Dispose();
    Conversation = null;
    Thread = null;
    Agent?.Dispose();
    Agent = null;
    // TagClient owns the screenshot client, which owns the invoking client (DelegatingChatClient
    // disposes its inner client).
    TagClient?.Dispose();
    TagClient = null;
    Functions?.Dispose();
    Functions = null;
    ReportedStatus = null;
    ReportedStatusPath = null;
    BuiltPath = null;
  }

  private string CurrentPath()
  {
    PageAgentRoute.Observe(NavigationManager, JsRuntime);
    return PageAgentRoute.PathOr(NavigationManager);
  }

  private Task InvokeUi(Func<Task> work)
  {
    Func<Func<Task>, Task>? invoke = UiInvoke;
    return invoke is null ? work() : invoke(work);
  }

  private void NotifyChanged()
  {
    if (Disposed)
    {
      return;
    }

    Action[] listeners = [.. Listeners];
    foreach (Action listener in listeners)
    {
      listener();
    }
  }

  private sealed class ListenerRegistration : IDisposable
  {
    private AskAgentSession? Session;
    private Action? Listener;

    public ListenerRegistration(AskAgentSession session, Action listener)
    {
      Session = session;
      Listener = listener;
    }

    public void Dispose()
    {
      if (Session is null || Listener is null)
      {
        return;
      }

      Session.Listeners.Remove(Listener);
      Session = null;
      Listener = null;
    }
  }

  private void ReportBuilt(string path, AgentEditMode mode, int generation, int toolCount, IReadOnlyList<string> prefillTools)
  {
    AskDiagnostics diagnostics = Diagnostics();
    if (!diagnostics.IsEnabled)
    {
      return;
    }

    DeferralKey = null;
    diagnostics.AgentBuilt(path, mode.ToString(), generation, toolCount, prefillTools);
  }

  private void ReportDeferred(string reason, string path, AgentEditMode mode, int generation)
  {
    AskDiagnostics diagnostics = Diagnostics();
    if (!diagnostics.IsEnabled)
    {
      return;
    }

    string key = $"{reason}|{path}|{mode}|{generation.ToString(CultureInfo.InvariantCulture)}";
    if (string.Equals(DeferralKey, key, StringComparison.Ordinal))
    {
      return;
    }

    DeferralKey = key;
    diagnostics.AgentDeferred(reason, path, mode.ToString(), generation);
  }

  private void NoteStatus(ConversationStatus status, string path)
  {
    if (status is not (
      ConversationStatus.Streaming
      or ConversationStatus.AwaitingInput
      or ConversationStatus.Idle
      or ConversationStatus.Error))
    {
      return;
    }

    AskDiagnostics diagnostics = Diagnostics();
    if (!diagnostics.IsEnabled)
    {
      return;
    }

    if (ReportedStatus == status && string.Equals(ReportedStatusPath, path, StringComparison.Ordinal))
    {
      return;
    }

    ReportedStatus = status;
    ReportedStatusPath = path;
    diagnostics.Status(status.ToString(), path);
  }

  private AskDiagnostics Diagnostics() =>
    Services.GetService<AskDiagnostics>() ?? AskDiagnostics.Disabled;
}

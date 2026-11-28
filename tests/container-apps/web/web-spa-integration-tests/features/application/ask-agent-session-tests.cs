#region Purpose
// Task 538: a route remount of AgentAsk must not cancel the live Ask turn.
#endregion

#region Design
// In-proc SPA host (no Aspire) with a scripted IChatClient, the same shape as the readiness and
// diagnostics hosts. InteractiveHtmlRenderer stands in for the circuit. The host shows AgentAsk,
// then hides and shows it again while navigate's tool result is already in hand and the model's
// reply is still waiting: that is the TimeWarpPage dispose/remount. The session's AgentContext
// stays the one that was streaming, the thread commits the navigation, and the idle rebuild
// offers Prefill.Role.CreateRole wrapped for Ask before editing. A second turn is handed that
// history. New conversation and disposing the scope still dispose the previous context and the
// catalog functions. This file is a project test, not a standalone runfile: the integration
// project compiles it, and CI runs that project.
#endregion

namespace AskAgentSession_;

using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.AI;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web.HtmlRendering;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using TimeWarp.Architecture.Components;

[TestTag("Integration")]
public partial class AskAgentSession_Should_
{
  private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(15);

  [ModuleInitializer]
  internal static void Register() => RegisterTests<AskAgentSession_Should_>();

  [Proves(typeof(AskAgentSession), nameof(AskAgentSession.EnsureAsync))]
  public static async Task Remount_Mid_Navigate_Should_Commit_The_Turn_And_Rebuild_For_The_New_Role_Page()
  {
    using NavigatingChatClient client = new();
    using SessionHost host = new(client);
    using IServiceScope scope = host.CreateScope();
    IServiceProvider provider = scope.ServiceProvider;
    await PrepareAsync(provider).ConfigureAwait(false);

    AskAgentSession session = provider.GetRequiredService<AskAgentSession>();
    AgentAsk? ask = null;
    Action<AgentAsk?> capture = component => ask = component;
    await using InteractiveHtmlRenderer renderer = new(provider, provider.GetRequiredService<ILoggerFactory>());
    HtmlRootComponent root = await renderer.Dispatcher.InvokeAsync(() =>
      renderer.BeginRenderingComponent(
        typeof(RemountHost),
        ParameterView.FromDictionary(Parameters(capture)))).ConfigureAwait(false);
    await root.QuiescenceTask.ConfigureAwait(false);
    ask.ShouldNotBeNull();
    await InteractiveHtmlRenderer.InvokeAfterRenderAsync(ask, firstRender: true).ConfigureAwait(false);

    session.Agent.ShouldNotBeNull();
    session.BuiltPath.ShouldBe("/");
    session.Conversation.ShouldNotBeNull();
    AgentContext live = session.Conversation;
    string before = provider.GetRequiredService<NavigationManager>().Uri;

    Task turn = Task.CompletedTask;
    void Send() => turn = live.SendMessageAsync("Open the new role form.");
    await renderer.Dispatcher.InvokeAsync(Send).ConfigureAwait(false);

    bool approved = false;
    DateTime deadline = DateTime.UtcNow.Add(Timeout);
    while (!client.ToolResultReached.IsCompleted)
    {
      DateTime.UtcNow.ShouldBeLessThan(deadline, "navigate did not finish");
      if (!approved && live.Status == ConversationStatus.AwaitingInput)
      {
        await renderer.Dispatcher.InvokeAsync(() => ApprovePending(live)).ConfigureAwait(false);
        approved = true;
      }

      await Task.Delay(20).ConfigureAwait(false);
    }

    provider.GetRequiredService<NavigationManager>().Uri.ShouldContain("/Admin/Roles/New");
    provider.GetRequiredService<NavigationManager>().Uri.ShouldNotBe(before);
    live.Status.ShouldBe(ConversationStatus.Streaming);

    await SetShownAsync(provider, renderer, show: false, () => ask).ConfigureAwait(false);
    session.Conversation.ShouldBeSameAs(live);
    live.Status.ShouldBe(ConversationStatus.Streaming);

    await SetShownAsync(provider, renderer, show: true, () => ask).ConfigureAwait(false);
    ask.ShouldNotBeNull();
    await InteractiveHtmlRenderer.InvokeAfterRenderAsync(ask, firstRender: true).ConfigureAwait(false);
    session.Conversation.ShouldBeSameAs(live);
    session.BuiltPath.ShouldBe("/");

    client.Continue();
    await turn.WaitAsync(Timeout).ConfigureAwait(false);

    live.Status.ShouldBe(ConversationStatus.Idle);
    session.Thread.ShouldNotBeNull();
    // The tool round and the reply each commit, so one user send is two thread turns.
    session.Thread.TurnCount.ShouldBe(2);
    IReadOnlyList<ChatResponseUpdate> committed = session.Thread.GetUpdates();
    committed.SelectMany(update => update.Contents).OfType<FunctionCallContent>()
      .Count(call => call.Name == AgentNavigate.ToolName)
      .ShouldBe(1);
    string committedText = string.Join(" ", committed.Select(update => update.Text));
    committedText.ShouldContain("Opened the new role page.");

    await InteractiveHtmlRenderer.InvokeAfterRenderAsync(ask).ConfigureAwait(false);
    session.BuiltPath.ShouldBe("/Admin/Roles/New");
    session.BuiltMode.ShouldBe(AgentEditMode.AskBeforeEditing);
    session.BuiltTools.Single(tool => tool.Name == "Prefill.Role.CreateRole")
      .ShouldBeOfType<ApprovalRequiredAIFunction>();
    session.Conversation.ShouldNotBeSameAs(live);

    Task second = Task.CompletedTask;
    void SendAgain() => second = session.Conversation!.SendMessageAsync("Fill in the role.");
    await renderer.Dispatcher.InvokeAsync(SendAgain).ConfigureAwait(false);
    await second.WaitAsync(Timeout).ConfigureAwait(false);

    client.NavigateOffers.ShouldBe(1);
    client.SecondTurn.ShouldNotBeNull();
    client.SecondTurn.SelectMany(message => message.Contents).OfType<FunctionCallContent>()
      .ShouldContain(call => call.Name == AgentNavigate.ToolName);
    string history = string.Join(" ", client.SecondTurn.Select(message => message.Text));
    history.ShouldContain("Opened the new role page.");
    history.ShouldContain("Fill in the role.");

    await provider.GetRequiredService<AskDiagnostics>().FlushAsync().ConfigureAwait(false);
    string lines = DiagnosticText(host.Runtime);
    lines.ShouldContain("event=tool-start");
    lines.ShouldContain("toolName=navigate");
    lines.ShouldContain("event=tool-outcome");
    lines.ShouldContain("event=status");
    lines.ShouldContain("status=Idle");
    lines.ShouldContain("event=agent-built");
    lines.ShouldContain("path=/Admin/Roles/New");
    lines.ShouldContain("Prefill.Role.CreateRole");
  }

  public static async Task Prefill_After_Navigation_Should_Run_In_Automatic_Mode_And_Hand_Off_When_Approval_Is_Required()
  {
    using SessionHost host = new(new NavigatingChatClient());
    using IServiceScope scope = host.CreateScope();
    IServiceProvider provider = scope.ServiceProvider;
    await PrepareAsync(provider).ConfigureAwait(false);
    NavigationManager navigation = provider.GetRequiredService<NavigationManager>();
    PageAgentRoute route = provider.GetRequiredService<PageAgentRoute>();
    IAuthorizationService authorization = provider.GetRequiredService<IAuthorizationService>();
    AuthenticationState authentication = await provider.GetRequiredService<AuthenticationStateProvider>()
      .GetAuthenticationStateAsync()
      .ConfigureAwait(false);

    await provider.GetRequiredService<ISender<ClientPipeline>>()
      .Send(new AgentSurfaceState.SetEditModeActionSet.Action(AgentEditMode.AutomaticallyEdit))
      .ConfigureAwait(false);
    navigation.NavigateTo("/");
    route.Observe("/");
    using (CatalogAgentFunctions home = CatalogAgentFunctions.Create(await SelectAsync(provider, authentication, authorization, "/")))
    {
      AIFunction prefill = home.Tools.Single(tool => tool.Name == "Prefill.Role.CreateRole")
        .ShouldBeAssignableTo<AIFunction>();
      prefill.ShouldNotBeOfType<ApprovalRequiredAIFunction>();
      navigation.NavigateTo("/Admin/Roles/New");
      route.Observe("/Admin/Roles/New");
      object? drafted = await prefill.InvokeAsync(Arguments(provider)).ConfigureAwait(false);
      string draftedText = drafted.ShouldNotBeNull().ToString() ?? "";
      draftedText.ShouldContain("Drafted = True");
    }

    await provider.GetRequiredService<ISender<ClientPipeline>>()
      .Send(new AgentSurfaceState.SetEditModeActionSet.Action(AgentEditMode.AskBeforeEditing))
      .ConfigureAwait(false);
    navigation.NavigateTo("/");
    route.Observe("/");
    using CatalogAgentFunctions asking = CatalogAgentFunctions.Create(
      await SelectAsync(provider, authentication, authorization, "/", AgentEditMode.AskBeforeEditing));
    AIFunction wrappedOffPage = asking.Tools.Single(tool => tool.Name == "Prefill.Role.CreateRole")
      .ShouldBeAssignableTo<AIFunction>();
    wrappedOffPage.ShouldNotBeOfType<ApprovalRequiredAIFunction>();
    navigation.NavigateTo("/Admin/Roles/New");
    route.Observe("/Admin/Roles/New");
    object? handoff = await wrappedOffPage.InvokeAsync(Arguments(provider)).ConfigureAwait(false);
    string handoffText = handoff.ShouldNotBeNull().ToString()!;
    handoffText.ShouldContain(CatalogAgentFunctions.ArrivalNeedsApprovalError);
    handoffText.ShouldNotContain(CatalogAgentFunctions.ApprovalRequiredError);
  }

  public static async Task New_Conversation_And_Host_Disposal_Should_Dispose_The_Previous_Agent()
  {
    using HeldChatClient client = new();
    using SessionHost host = new(client);
    using IServiceScope scope = host.CreateScope();
    IServiceProvider provider = scope.ServiceProvider;
    await PrepareAsync(provider).ConfigureAwait(false);
    AskAgentSession session = provider.GetRequiredService<AskAgentSession>();
    (InteractiveHtmlRenderer renderer, HtmlRootComponent _, AgentAsk ask) = await MountAsync(provider).ConfigureAwait(false);
    await using (renderer)
    {
      await InteractiveHtmlRenderer.InvokeAfterRenderAsync(ask, firstRender: true).ConfigureAwait(false);
      session.Conversation.ShouldNotBeNull();
      AgentContext first = session.Conversation;
      AIFunction pageContext = session.BuiltTools.Single(tool => tool.Name == PageAgentContext.ToolName)
        .ShouldBeAssignableTo<AIFunction>();

      Task turn = Task.CompletedTask;
      void Send() => turn = first.SendMessageAsync("Hold this turn.");
      await renderer.Dispatcher.InvokeAsync(Send).ConfigureAwait(false);
      await client.Started.Task.WaitAsync(Timeout).ConfigureAwait(false);
      first.Status.ShouldBe(ConversationStatus.Streaming);

      await provider.GetRequiredService<ISender<ClientPipeline>>()
        .Send(new AgentSurfaceState.NewConversationActionSet.Action())
        .ConfigureAwait(false);
      await InteractiveHtmlRenderer.InvokeAfterRenderAsync(ask).ConfigureAwait(false);
      session.Conversation.ShouldNotBeNull();
      session.Conversation.ShouldNotBeSameAs(first);
      session.Thread.ShouldNotBeNull();
      session.Thread.TurnCount.ShouldBe(0);
      string schemaDetail = pageContext.GetType().FullName
        + " same=" + session.BuiltTools.Contains(pageContext);
      Action readSchema = () => _ = pageContext.JsonSchema.GetRawText();
      Should.Throw<ObjectDisposedException>(readSchema, schemaDetail);
      await AwaitCancelledTurnAsync(turn).ConfigureAwait(false);
      client.Cancelled.Task.IsCompleted.ShouldBeTrue();
    }

    using HeldChatClient closing = new();
    using SessionHost closeHost = new(closing);
    using IServiceScope closeScope = closeHost.CreateScope();
    IServiceProvider closeProvider = closeScope.ServiceProvider;
    await PrepareAsync(closeProvider).ConfigureAwait(false);
    AskAgentSession closeSession = closeProvider.GetRequiredService<AskAgentSession>();
    AgentAsk? closed = null;
    Action<AgentAsk?> closeCapture = component => closed = component;
    (InteractiveHtmlRenderer closeRenderer, HtmlRootComponent _, AgentAsk closeAsk) =
      await MountAsync(closeProvider, closeCapture).ConfigureAwait(false);
    await using (closeRenderer)
    {
      await InteractiveHtmlRenderer.InvokeAfterRenderAsync(closeAsk, firstRender: true).ConfigureAwait(false);
      AgentContext streaming = closeSession.Conversation.ShouldNotBeNull();
      Task turn = Task.CompletedTask;
      void Send() => turn = streaming.SendMessageAsync("Still going.");
      await closeRenderer.Dispatcher.InvokeAsync(Send).ConfigureAwait(false);
      await closing.Started.Task.WaitAsync(Timeout).ConfigureAwait(false);

      await closeProvider.GetRequiredService<ISender<ClientPipeline>>()
        .Send(new AgentSurfaceState.CloseAskPanelActionSet.Action())
        .ConfigureAwait(false);
      await SetShownAsync(closeProvider, closeRenderer, show: false, () => closed).ConfigureAwait(false);
      closeSession.Agent.ShouldBeNull();
      await AwaitCancelledTurnAsync(turn).ConfigureAwait(false);
      closing.Cancelled.Task.IsCompleted.ShouldBeTrue();
    }

    using HeldChatClient circuit = new();
    using SessionHost circuitHost = new(circuit);
    using IServiceScope circuitScope = circuitHost.CreateScope();
    IServiceProvider circuitProvider = circuitScope.ServiceProvider;
    await PrepareAsync(circuitProvider).ConfigureAwait(false);
    AskAgentSession circuitSession = circuitProvider.GetRequiredService<AskAgentSession>();
    (InteractiveHtmlRenderer circuitRenderer, HtmlRootComponent _, AgentAsk circuitAsk) =
      await MountAsync(circuitProvider).ConfigureAwait(false);
    AgentContext circuitConversation;
    AIFunction circuitPage;
    Task circuitTurn;
    await using (circuitRenderer)
    {
      await InteractiveHtmlRenderer.InvokeAfterRenderAsync(circuitAsk, firstRender: true).ConfigureAwait(false);
      circuitConversation = circuitSession.Conversation.ShouldNotBeNull();
      circuitPage = circuitSession.BuiltTools.Single(tool => tool.Name == PageAgentContext.ToolName)
        .ShouldBeAssignableTo<AIFunction>();
      circuitTurn = Task.CompletedTask;
      void SendCircuit() => circuitTurn = circuitConversation.SendMessageAsync("Outlive the panel.");
      await circuitRenderer.Dispatcher.InvokeAsync(SendCircuit).ConfigureAwait(false);
      await circuit.Started.Task.WaitAsync(Timeout).ConfigureAwait(false);
      circuitTurn.IsCompleted.ShouldBeFalse();
    }

    circuit.Cancelled.Task.IsCompleted.ShouldBeFalse();
    circuitSession.Conversation.ShouldBeSameAs(circuitConversation);
    circuitScope.Dispose();
    await AwaitCancelledTurnAsync(circuitTurn).ConfigureAwait(false);
    circuit.Cancelled.Task.IsCompleted.ShouldBeTrue();
    circuitSession.Agent.ShouldBeNull();
    circuitSession.BuiltTools.ShouldBeEmpty();
    Action readCircuitSchema = () => _ = circuitPage.JsonSchema.GetRawText();
    Should.Throw<ObjectDisposedException>(readCircuitSchema, circuitPage.GetType().FullName);
  }

  private static async Task PrepareAsync(IServiceProvider provider)
  {
    await provider.GetRequiredService<ISender<ClientPipeline>>()
      .Send(new AgentSurfaceState.LoadChatConfigurationActionSet.Action())
      .ConfigureAwait(false);
    provider.GetRequiredService<IStore>().GetState<AgentSurfaceState>().ChatReadiness
      .ShouldBe(CatalogAgentReadiness.Configured);
    await provider.GetRequiredService<ISender<ClientPipeline>>()
      .Send(new AgentSurfaceState.OpenAskPanelActionSet.Action())
      .ConfigureAwait(false);
  }

  private static async Task<(InteractiveHtmlRenderer Renderer, HtmlRootComponent Root, AgentAsk Ask)> MountAsync(
    IServiceProvider provider,
    Action<AgentAsk?>? capture = null)
  {
    AgentAsk? ask = null;
    Action<AgentAsk?> remember = component =>
    {
      ask = component;
      capture?.Invoke(component);
    };
    InteractiveHtmlRenderer renderer = new(provider, provider.GetRequiredService<ILoggerFactory>());
    HtmlRootComponent root = await renderer.Dispatcher.InvokeAsync(() =>
      renderer.BeginRenderingComponent(
        typeof(RemountHost),
        ParameterView.FromDictionary(Parameters(remember)))).ConfigureAwait(false);
    await root.QuiescenceTask.ConfigureAwait(false);
    ask.ShouldNotBeNull();
    return (renderer, root, ask);
  }

  private static async Task SetShownAsync(
    IServiceProvider provider,
    InteractiveHtmlRenderer renderer,
    bool show,
    Func<AgentAsk?> current)
  {
    AgentAsk? before = current();
    RemountSignal signal = provider.GetRequiredService<RemountSignal>();
    await renderer.Dispatcher.InvokeAsync(() => signal.Set(show)).ConfigureAwait(false);
    DateTime deadline = DateTime.UtcNow.Add(Timeout);
    while (ReferenceEquals(current(), before))
    {
      DateTime.UtcNow.ShouldBeLessThan(deadline, show ? "panel did not remount" : "panel did not unmount");
      await Task.Delay(20).ConfigureAwait(false);
    }
  }

  private static Dictionary<string, object?> Parameters(Action<AgentAsk?> capture) =>
    new()
    {
      [nameof(RemountHost.Capture)] = capture,
    };

  private static string DiagnosticText(RecordingRuntime runtime)
  {
    List<string> lines = [];
    foreach ((string identifier, object?[] arguments) in runtime.Calls)
    {
      if (identifier == AskDiagnostics.HookName
        && arguments.Length == 2
        && arguments[1] is string message)
      {
        lines.Add(message);
      }
    }

    return string.Join('\n', lines);
  }

  private static async Task AwaitCancelledTurnAsync(Task turn)
  {
    try
    {
      await turn.WaitAsync(Timeout).ConfigureAwait(false);
    }
    catch (OperationCanceledException)
    {
    }
  }

  private static void ApprovePending(AgentContext conversation)
  {
    foreach (ConversationTurn turn in conversation.Turns)
    {
      foreach (ContentBlock block in turn.ResponseBlocks)
      {
        if (block is FunctionApprovalBlock approval && approval.Status == ApprovalStatus.Pending)
        {
          approval.Approve();
          return;
        }
      }
    }

    throw new InvalidOperationException("No pending navigate approval.");
  }

  private static Task<IReadOnlyList<CatalogAgentTool>> SelectAsync(
    IServiceProvider provider,
    AuthenticationState authentication,
    IAuthorizationService authorization,
    string path,
    AgentEditMode mode = AgentEditMode.AutomaticallyEdit) =>
    CatalogAgentToolSet.SelectAsync(
      authentication.User,
      authorization,
      provider.GetRequiredService<IActionCatalog>().Entries,
      path,
      mode,
      CancellationToken.None);

  private static AIFunctionArguments Arguments(IServiceProvider provider) =>
    new(new Dictionary<string, object?>
    {
      ["name"] = "Ada Lovelace",
      ["description"] = "Computes.",
    })
    {
      Services = provider,
    };

}

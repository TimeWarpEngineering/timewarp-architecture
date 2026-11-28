#region Purpose
// Task 536-003: wiring proofs for ask-diag lines from the real tool, form scope, and Ask panel.
#endregion

#region Design
// These proofs call CatalogAgentFunction, FormPrefillScope, and AgentAsk.OnAfterRenderAsync
// on an in-proc host with a recording IJSRuntime. Each breadcrumb is printed as "ASK-DIAG "
// plus the line, with no typed values. Clear is synchronous, so the already-handled proof
// puts the same draft id back and renders again. A counting model shows a disabled host
// does not take the dirty snapshot the report helper would. This file is a project test,
// not a standalone runfile: the integration project compiles it, and CI runs that project.
#endregion

namespace AskDiagnostics_;

using System.Reflection;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web.HtmlRendering;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using TimeWarp.Architecture;
using TimeWarp.Architecture.Features;
using TimeWarp.Architecture.Features.Admin.Roles;
using TimeWarp.Architecture.Web.Spa;

public partial class AskDiagnostics_Should_
{
  public static async Task Invoke_Writes_Tool_Lines_Without_Typed_Values()
  {
    using DiagnosticHost host = new(development: true);
    using IServiceScope scope = host.CreateScope();
    IServiceProvider provider = scope.ServiceProvider;
    using SelectedTool selected = await SelectToolAsync(provider, "/Admin/Roles/New", "Prefill.Role.CreateRole")
      .ConfigureAwait(false);
    object? drafted = await selected.Function.InvokeAsync(Arguments(
      provider,
      new Dictionary<string, object?>
      {
        ["name"] = Person,
        ["description"] = Secret,
      })).ConfigureAwait(false);
    drafted.ShouldNotBeNull().ToString()!.ShouldContain("Drafted = True");
    List<string> lines = await LinesAsync(provider).ConfigureAwait(false);
    WriteLines(lines);
    string joined = string.Join('\n', lines);
    joined.ShouldContain("event=tool-start");
    joined.ShouldContain("toolName=Prefill.Role.CreateRole");
    joined.ShouldContain("event=tool-outcome");
    joined.ShouldContain("prefill=true");
    joined.ShouldContain("completed=true");
    joined.ShouldContain("fieldNames=name|description");
    joined.ShouldContain("error=-");
    AssertNoSecrets(joined);
  }

  public static async Task Thrown_Tool_Writes_The_Exception_Type_And_Rethrows()
  {
    using DiagnosticHost host = new(development: true);
    using IServiceScope scope = host.CreateScope();
    IServiceProvider provider = scope.ServiceProvider;
    using SelectedTool selected = await SelectToolAsync(provider, "/Counter", "Counter.IncrementCounter")
      .ConfigureAwait(false);
    Exception? thrown = null;
    try
    {
      await selected.Function.InvokeAsync(Arguments(provider, [])).ConfigureAwait(false);
    }
    catch (ArgumentException exception)
    {
      thrown = exception;
    }

    thrown.ShouldNotBeNull();
    List<string> lines = await LinesAsync(provider).ConfigureAwait(false);
    WriteLines(lines);
    string joined = string.Join('\n', lines);
    joined.ShouldContain("event=tool-start");
    joined.ShouldContain("toolName=Counter.IncrementCounter");
    joined.ShouldContain("event=tool-outcome");
    joined.ShouldContain("completed=false");
    joined.ShouldContain("error=ArgumentException");
    (thrown is ArgumentException || thrown.InnerException is ArgumentException).ShouldBeTrue(thrown.ToString());
  }

  public static async Task Prefill_Scope_Reports_Seen_Applied_And_Already_Handled()
  {
    const string draft = """{"name":"Ada Lovelace","description":"super-secret-token-value"}""";
    using DiagnosticHost host = new(development: true);
    using IServiceScope scope = host.CreateScope();
    IServiceProvider provider = scope.ServiceProvider;
    string form = FormSuggestionState.KeyFor(typeof(RoleState.CreateRoleActionSet.Action));
    RoleDraft model = new();
    ComponentBase? prefill = null;
    await using InteractiveHtmlRenderer renderer = new(provider, provider.GetRequiredService<ILoggerFactory>());
    HtmlRootComponent root = await renderer.Dispatcher.InvokeAsync(() =>
      renderer.BeginRenderingComponent(
        typeof(ScopeHost),
        ParameterView.FromDictionary(new Dictionary<string, object?>
        {
          [nameof(ScopeHost.Model)] = model,
          [nameof(ScopeHost.Capture)] = (Action<ComponentBase>)(component => prefill = component),
        }))).ConfigureAwait(false);
    await root.QuiescenceTask.ConfigureAwait(false);
    prefill.ShouldNotBeNull();
    await provider.GetRequiredService<ISender<ClientPipeline>>()
      .Send(new FormSuggestionState.SetFormSuggestionActionSet.Action(form, draft))
      .ConfigureAwait(false);
    await InteractiveHtmlRenderer.InvokeAfterRenderAsync(prefill).ConfigureAwait(false);
    List<string> lines = await LinesAsync(provider).ConfigureAwait(false);
    WriteLines(lines);
    string joined = string.Join('\n', lines);
    joined.ShouldContain("event=prefill-seen");
    joined.ShouldContain("alreadyHandled=false");
    joined.ShouldContain("event=prefill-outcome");
    joined.ShouldContain("outcome=applied");
    joined.ShouldContain("fieldNames=name|description");
    joined.ShouldContain("outcome=cleared");
    AssertNoSecrets(joined);
    model.Name.ShouldBe(Person);
    provider.GetRequiredService<IStore>().GetState<FormSuggestionState>().For(form).ShouldBeNull();

    string draftId = DraftId(lines);
    FormSuggestionState state = provider.GetRequiredService<IStore>().GetState<FormSuggestionState>();
    MethodInfo put = typeof(FormSuggestionState).GetMethod("Put", BindingFlags.Instance | BindingFlags.NonPublic)
      ?? throw new InvalidOperationException("FormSuggestionState.Put is missing.");
    put.Invoke(state, [form, new FormSuggestion(draft, draftId, Accepted: false)]);
    await InteractiveHtmlRenderer.InvokeAfterRenderAsync(prefill).ConfigureAwait(false);
    List<string> again = await LinesAsync(provider).ConfigureAwait(false);
    WriteLines(again);
    string second = string.Join('\n', again);
    second.ShouldContain("alreadyHandled=true");
    AssertNoSecrets(second);
    Count(again, "outcome=applied").ShouldBe(1);
    model.Name.ShouldBe(Person);
  }

  public static async Task Report_Helpers_Skip_The_Snapshot_When_Diagnostics_Are_Off()
  {
    CountingModel disabledModel = new();
    int disabledReads = await SnapshotReadsAsync(development: false, disabledModel).ConfigureAwait(false);
    CountingModel enabledModel = new();
    int enabledReads = await SnapshotReadsAsync(development: true, enabledModel).ConfigureAwait(false);
    disabledReads.ShouldBeGreaterThan(0);
    enabledReads.ShouldBeGreaterThan(disabledReads);
  }

  public static async Task Agent_Ask_Reports_A_Deferred_Build()
  {
    using DiagnosticHost host = new(development: true);
    using IServiceScope scope = host.CreateScope();
    IServiceProvider provider = scope.ServiceProvider;
    AgentAsk? ask = null;
    await using InteractiveHtmlRenderer renderer = new(provider, provider.GetRequiredService<ILoggerFactory>());
    HtmlRootComponent root = await renderer.Dispatcher.InvokeAsync(() =>
      renderer.BeginRenderingComponent(
        typeof(AskHost),
        ParameterView.FromDictionary(new Dictionary<string, object?>
        {
          [nameof(AskHost.Capture)] = (Action<AgentAsk>)(component => ask = component),
        }))).ConfigureAwait(false);
    await root.QuiescenceTask.ConfigureAwait(false);
    ask.ShouldNotBeNull();
    await InteractiveHtmlRenderer.InvokeAfterRenderAsync(ask).ConfigureAwait(false);
    List<string> lines = await LinesAsync(provider).ConfigureAwait(false);
    WriteLines(lines);
    string joined = string.Join('\n', lines);
    joined.ShouldContain("event=agent-deferred");
    joined.ShouldContain("reason=Readiness");
    AssertNoSecrets(joined);
  }

  private sealed class RecordingRuntime : IJSRuntime
  {
    public List<JsCall> Calls { get; } = [];

    public Exception? Failure { get; set; }

    public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
    {
      Calls.Add(new JsCall(identifier, args ?? []));
      if (Failure is not null)
      {
        return ValueTask.FromException<TValue>(Failure);
      }

      if (string.Equals(identifier, "import", StringComparison.Ordinal)
        && typeof(TValue).IsAssignableFrom(typeof(NoopModule)))
      {
        return ValueTask.FromResult((TValue)(object)new NoopModule());
      }

      return ValueTask.FromResult<TValue>(default!);
    }

    public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
    {
      _ = cancellationToken;
      Calls.Add(new JsCall(identifier, args ?? []));
      if (Failure is not null)
      {
        return ValueTask.FromException<TValue>(Failure);
      }

      if (string.Equals(identifier, "import", StringComparison.Ordinal)
        && typeof(TValue).IsAssignableFrom(typeof(NoopModule)))
      {
        return ValueTask.FromResult((TValue)(object)new NoopModule());
      }

      return ValueTask.FromResult<TValue>(default!);
    }
  }

  private sealed class NoopModule : IJSObjectReference
  {
    public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
    {
      _ = identifier;
      _ = args;
      return ValueTask.FromResult(default(TValue)!);
    }

    public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
    {
      _ = identifier;
      _ = cancellationToken;
      _ = args;
      return ValueTask.FromResult(default(TValue)!);
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
  }

  private sealed record JsCall(string Identifier, object?[] Arguments);

  private static async Task<SelectedTool> SelectToolAsync(IServiceProvider provider, string path, string toolName)
  {
    provider.GetRequiredService<NavigationManager>().NavigateTo(path);
    await provider.GetRequiredService<ISender<ClientPipeline>>()
      .Send(new AgentSurfaceState.SetEditModeActionSet.Action(AgentEditMode.AutomaticallyEdit))
      .ConfigureAwait(false);
    AuthenticationState authentication = await provider.GetRequiredService<AuthenticationStateProvider>()
      .GetAuthenticationStateAsync()
      .ConfigureAwait(false);
    IReadOnlyList<CatalogAgentTool> selected = await CatalogAgentToolSet.SelectAsync
    (
      authentication.User,
      provider.GetRequiredService<IAuthorizationService>(),
      provider.GetRequiredService<IActionCatalog>().Entries,
      PageAgentScope.FromNavigation(provider.GetRequiredService<NavigationManager>()),
      AgentEditMode.AutomaticallyEdit,
      CancellationToken.None
    ).ConfigureAwait(false);
    CatalogAgentFunctions functions = CatalogAgentFunctions.Create(selected);
    AITool? found = null;
    foreach (AITool tool in functions.Tools)
    {
      if (tool.Name == toolName)
      {
        found = tool;
        break;
      }
    }

    if (found is null)
    {
      functions.Dispose();
      throw new InvalidOperationException("Tool was not offered: " + toolName);
    }

    return new SelectedTool(functions, found.ShouldBeAssignableTo<AIFunction>());
  }

  private static AIFunctionArguments Arguments(IServiceProvider provider, Dictionary<string, object?> values) =>
    new(values) { Services = provider };

  private static async Task<List<string>> LinesAsync(IServiceProvider provider)
  {
    AskDiagnostics diagnostics = provider.GetRequiredService<AskDiagnostics>();
    await diagnostics.FlushAsync().ConfigureAwait(false);
    List<string> lines = [];
    RecordingRuntime runtime = (RecordingRuntime)provider.GetRequiredService<IJSRuntime>();
    foreach (JsCall call in runtime.Calls)
    {
      if (call.Identifier == AskDiagnostics.HookName
        && call.Arguments.Length == 2
        && call.Arguments[1] is string message)
      {
        lines.Add(message);
      }
    }

    return lines;
  }

  private static void WriteLines(IReadOnlyList<string> lines)
  {
    foreach (string line in lines)
    {
      Console.WriteLine("ASK-DIAG " + line);
    }
  }

  private static string DraftId(IReadOnlyList<string> lines)
  {
    foreach (string line in lines)
    {
      const string marker = "draftId=";
      int start = line.IndexOf(marker, StringComparison.Ordinal);
      if (start < 0)
      {
        continue;
      }

      int from = start + marker.Length;
      int end = line.IndexOf(' ', from, StringComparison.Ordinal);
      return end < 0 ? line[from..] : line[from..end];
    }

    throw new InvalidOperationException("No draftId on an ask-diag line.");
  }

  private static int Count(IReadOnlyList<string> lines, string token)
  {
    int count = 0;
    foreach (string line in lines)
    {
      if (line.Contains(token, StringComparison.Ordinal))
      {
        count++;
      }
    }

    return count;
  }

  private static async Task<int> SnapshotReadsAsync(bool development, CountingModel model)
  {
    using DiagnosticHost host = new(development);
    using IServiceScope scope = host.CreateScope();
    IServiceProvider provider = scope.ServiceProvider;
    string form = FormSuggestionState.KeyFor(typeof(RoleState.CreateRoleActionSet.Action));
    await provider.GetRequiredService<ISender<ClientPipeline>>()
      .Send(new FormSuggestionState.SetFormSuggestionActionSet.Action(form, """{"name":"kept"}"""))
      .ConfigureAwait(false);
    await using InteractiveHtmlRenderer renderer = new(provider, provider.GetRequiredService<ILoggerFactory>());
    HtmlRootComponent root = await renderer.Dispatcher.InvokeAsync(() =>
      renderer.BeginRenderingComponent(
        typeof(ScopeHost),
        ParameterView.FromDictionary(new Dictionary<string, object?>
        {
          [nameof(ScopeHost.Model)] = model,
        }))).ConfigureAwait(false);
    await root.QuiescenceTask.ConfigureAwait(false);
    return model.Reads;
  }

  private sealed class DiagnosticHost : IDisposable
  {
    private readonly ServiceProvider Provider;

    public RecordingRuntime Runtime { get; } = new();

    public DiagnosticHost(bool development)
    {
      ClaimsPrincipal user = new(new ClaimsIdentity(
        [
          new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
          .. PermissionIds.All.Select(static permission => new Claim(PermissionIds.ClaimType, permission)),
        ],
        authenticationType: "test"));
      ServiceCollection services = new();
      services.AddLogging();
      services.AddFluentUIComponents();
      services.AddWebSpaGeneratedMediator();
      services.AddTimeWarpState(options =>
      {
        options.Assemblies =
        [
          typeof(TimeWarp.Architecture.Web.Spa.IAssemblyMarker).Assembly,
          typeof(TimeWarp.State.Plus.AssemblyMarker).Assembly,
        ];
      });
      services.AddTimeWarpStateBlazor();
      services.AddActionCatalog(typeof(TimeWarp.Architecture.Web.Spa.IAssemblyMarker).Assembly);
      services.AddTimeWarpStatePersistence();
      services.AddAuthorizationCore(PolicyRegistration.AddPolicies);
      services.AddScoped<AuthenticationStateProvider>(_ => new FixedAuthenticationStateProvider(user));
      services.AddScoped<IJSRuntime>(_ => Runtime);
      services.AddScoped(_ => new AskDiagnostics(development, Runtime));
      services.AddScoped<NavigationManager, TestNavigationManager>();
      services.AddScoped<PageAgentRoute>();
      services.AddScoped<AgentCallOutcome>();
      services.AddScoped<AskConversationThreads>();
      Provider = services.BuildServiceProvider();
    }

    public IServiceScope CreateScope() => Provider.CreateScope();

    public void Dispose() => Provider.Dispose();
  }

  private sealed class FixedAuthenticationStateProvider(ClaimsPrincipal user) : AuthenticationStateProvider
  {
    public override Task<AuthenticationState> GetAuthenticationStateAsync() =>
      Task.FromResult(new AuthenticationState(user));
  }

  private sealed record SelectedTool(CatalogAgentFunctions Functions, AIFunction Function) : IDisposable
  {
    public void Dispose() => Functions.Dispose();
  }

  private sealed class RoleDraft
  {
    public string? Name { get; set; }

    public string? Description { get; set; }
  }

  private sealed class CountingModel
  {
    public int Reads { get; private set; }

    public string Name
    {
      get
      {
        Reads++;
        return "kept";
      }
    }
  }

  private sealed class ScopeHost : ComponentBase
  {
    [Parameter] public object? Model { get; set; }

    [Parameter] public Action<ComponentBase>? Capture { get; set; }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
      builder.OpenComponent(0, typeof(FormPrefillScope<RoleState.CreateRoleActionSet.Action>));
      builder.AddAttribute(1, "Model", Model);
      builder.AddAttribute(2, "Noun", "role");
      builder.AddAttribute(3, "Ready", true);
      builder.AddComponentReferenceCapture(4, instance => Capture?.Invoke((ComponentBase)instance));
      builder.CloseComponent();
    }
  }

  private sealed class AskHost : ComponentBase
  {
    [Parameter] public Action<AgentAsk>? Capture { get; set; }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
      builder.OpenComponent<AgentAsk>(0);
      builder.AddComponentReferenceCapture(1, instance => Capture?.Invoke((AgentAsk)instance));
      builder.CloseComponent();
    }
  }
}

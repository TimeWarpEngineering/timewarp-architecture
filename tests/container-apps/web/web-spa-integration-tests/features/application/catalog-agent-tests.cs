#region Purpose
// Catalog tools for the in-app model and WebMCP: mapping, permissions, approval, invoke-time
// re-checks, and a fake client that dispatches a real store action.
#endregion

#region Design
// The host is the same Aspire SPA session the catalog roster uses. No live model and no secret:
// the scripted IChatClient returns one function call, then text. Approval is a ToolApprovalResponse
// the test's RunAsync loop writes before the store runs; that loop lives here, not in product code,
// over the product's CatalogAgentSession.CreateInvokingClient, and its tools come from the product
// selection (CatalogAgentToolSet.SelectAsync + CatalogAgentFunctions.Create). WebMCP registration is
// the same tool list applied to a stand-in model context; a null context registers nothing. WebMCP
// invoke tests drive WebMcpDispatcher with the per-test store and TestNavigationManager, read the
// call id from AgentSurfaceState, and bound every await so a hung approval fails instead of
// stalling the suite.
#endregion

namespace CatalogAgent_;

using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.AI;
using TimeWarp.Architecture.Components;
using TimeWarp.Architecture.Features;
using TimeWarp.Architecture.Features.Settings;
using TimeWarp.Architecture.Web.Spa;
using TimeWarp.Identity;

[TestTag("Integration")]
public class CatalogAgent_Should
{
  private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(10);
  private static SpaSessionFixture? Session;
  private static AspireSpaTestApplication? Spa;

  [System.Runtime.CompilerServices.ModuleInitializer]
  internal static void Register() => RegisterTests<CatalogAgent_Should>();

  public static async Task SetupOnce()
  {
    Session = await SessionFixture.GetAsync<SpaSessionFixture>();
    Spa = new AspireSpaTestApplication(Session.Inner);
  }

  public static Task CleanUpOnce()
  {
    Session = null;
    Spa = null;
    return Task.CompletedTask;
  }

  public static Task Schema_Names_Increment_Amount_And_Omits_CreateRole_UserId()
  {
    using SpaTestScope scope = SpaTestScope.Create(Spa!);
    IActionCatalog catalog = scope.ServiceProvider.GetRequiredService<IActionCatalog>();

    string increment = CatalogAgentSchema.For(catalog.Find("Counter.IncrementCounter").ShouldNotBeNull());
    increment.ShouldContain("amount");
    increment.ShouldContain("integer");

    string createRole = CatalogAgentSchema.For(catalog.Find("Role.CreateRole").ShouldNotBeNull());
    createRole.ShouldContain("name");
    createRole.ShouldNotContain("userId");
    createRole.ShouldNotContain("UserId");
    return Task.CompletedTask;
  }

  public static Task Binds_Json_Amount_To_Int()
  {
    using SpaTestScope scope = SpaTestScope.Create(Spa!);
    IActionCatalog catalog = scope.ServiceProvider.GetRequiredService<IActionCatalog>();
    ActionCatalogEntry increment = catalog.Find("Counter.IncrementCounter").ShouldNotBeNull();
    JsonElement amount = JsonSerializer.SerializeToElement(5);

    object?[] bound = CatalogAgentArguments.Bind
    (
      increment,
      new Dictionary<string, object?> { ["Amount"] = amount }
    );

    bound.Length.ShouldBe(1);
    bound[0].ShouldBe(5);
    return Task.CompletedTask;
  }

  public static Task Binds_UpdateSiteSettings_With_String_Enum_And_Schema_Lists_Enum_Members()
  {
    using SpaTestScope scope = SpaTestScope.Create(Spa!);
    IActionCatalog catalog = scope.ServiceProvider.GetRequiredService<IActionCatalog>();
    ActionCatalogEntry update = catalog.Find("SiteSettings.UpdateSiteSettings").ShouldNotBeNull();
    string parameter = update.Parameters[0].Name;
    JsonElement command = JsonSerializer.Deserialize<JsonElement>
    (
      """{"entraSignInEnabled":true,"entraAllowBootstrap":false,"passkeyPromptMode":"Required","version":3}"""
    );

    object?[] bound = CatalogAgentArguments.Bind(update, new Dictionary<string, object?> { [parameter] = command });

    bound.Length.ShouldBe(1);
    UpdateSiteSettings.Command bind = bound[0].ShouldBeOfType<UpdateSiteSettings.Command>();
    bind.EntraSignInEnabled.ShouldBeTrue();
    bind.PasskeyPromptMode.ShouldBe(PasskeyPromptMode.Required);
    bind.Version.ShouldBe(3);

    // The seam converter refuses integers, so the schema must advertise the member names.
    JsonElement integer = JsonSerializer.Deserialize<JsonElement>
    (
      """{"entraSignInEnabled":true,"entraAllowBootstrap":false,"passkeyPromptMode":1,"version":3}"""
    );
    Should.Throw<JsonException>
    (
      () => CatalogAgentArguments.Bind(update, new Dictionary<string, object?> { [parameter] = integer })
    );

    using JsonDocument schema = JsonDocument.Parse(CatalogAgentSchema.For(update));
    JsonElement mode = schema.RootElement
      .GetProperty("properties")
      .GetProperty(parameter)
      .GetProperty("properties")
      .GetProperty("passkeyPromptMode");
    mode.GetProperty("type").GetString().ShouldBe("string");
    string[] members = [.. mode.GetProperty("enum").EnumerateArray().Select(member => member.GetString() ?? "")];
    members.ShouldBe(Enum.GetNames<PasskeyPromptMode>());
    return Task.CompletedTask;
  }

  public static async Task Select_Is_Page_Scoped_Permission_Filtered_And_Drops_Human()
  {
    using SpaTestScope scope = SpaTestScope.Create(Spa!);
    IActionCatalog catalog = scope.ServiceProvider.GetRequiredService<IActionCatalog>();
    IAuthorizationService authorization = scope.ServiceProvider.GetRequiredService<IAuthorizationService>();
    ClaimsPrincipal everyone = Principal([.. PermissionIds.All]);
    ClaimsPrincipal developer = Principal(PermissionIds.DeveloperAccess);
    ClaimsPrincipal anonymous = new(new ClaimsIdentity());

    IReadOnlyList<CatalogAgentTool> counter = await CatalogAgentToolSet.SelectAsync
    (
      everyone, authorization, catalog.Entries, "/Counter?from=palette", CancellationToken.None
    );
    Names(counter).ShouldBe(["Counter.IncrementCounter"]);
    counter[0].RequiresApproval.ShouldBeTrue();

    IReadOnlyList<CatalogAgentTool> settings = await CatalogAgentToolSet.SelectAsync
    (
      everyone, authorization, catalog.Entries, "/Settings/", CancellationToken.None
    );
    Names(settings).ShouldBe
    (
      [
        "Credentials.FetchCredentials",
        "Credentials.RevokeCredential",
        "Credentials.RenameCredential",
      ]
    );
    settings.Single(tool => tool.Name == "Credentials.FetchCredentials").RequiresApproval.ShouldBeFalse();
    settings.Single(tool => tool.Name == "Credentials.RevokeCredential").RequiresApproval.ShouldBeTrue();

    settings.ShouldNotContain(tool => tool.Name == "Credentials.AddPasskey");
    settings.ShouldNotContain(tool => tool.Name == "Credentials.AddExistingPasskey");
    settings.ShouldNotContain(tool => tool.Name == "Credentials.LinkMicrosoft365");

    // Positive control: a permitted principal is offered CreateRole on this route, so the
    // developer's empty list below is the permission filter, not a missing mapping.
    Names
    (
      await CatalogAgentToolSet.SelectAsync
      (
        everyone, authorization, catalog.Entries, "/Admin/Roles/New", CancellationToken.None
      )
    ).ShouldBe(["Role.CreateRole"]);

    Names
    (
      await CatalogAgentToolSet.SelectAsync
      (
        developer, authorization, catalog.Entries, "/Admin/Roles/New", CancellationToken.None
      )
    ).ShouldBeEmpty();

    Names
    (
      await CatalogAgentToolSet.SelectAsync
      (
        everyone, authorization, catalog.Entries, "/Admin/Roles", CancellationToken.None
      )
    ).ShouldBeEmpty();

    Names
    (
      await CatalogAgentToolSet.SelectAsync
      (
        everyone, authorization, catalog.Entries, "/StyleGuide", CancellationToken.None
      )
    ).ShouldBeEmpty();

    (
      await CatalogAgentToolSet.SelectAsync
      (
        anonymous, authorization, catalog.Entries, "/Counter", CancellationToken.None
      )
    ).ShouldBeEmpty();
  }

  public static async Task Fake_Client_Dispatches_Increment_Only_After_Approval()
  {
    using SpaTestScope scope = SpaTestScope.Create(Spa!);
    scope.ServiceProvider.GetRequiredService<NavigationManager>().NavigateTo("/Counter");
    scope.Store.GetState<CounterState>().Initialize(count: 10);
    using CatalogAgentFunctions functions = CatalogAgentFunctions.Create(await SelectForSessionAsync(scope));
    ChatOptions options = new() { Tools = [.. functions.Tools] };
    Names(functions.Tools).ShouldBe(["Counter.IncrementCounter"]);
    functions.Tools[0].ShouldBeOfType<ApprovalRequiredAIFunction>();

    using ScriptedChatClient rejecting = new();
    string rejected = await RunAsync(rejecting, scope.ServiceProvider, options, static _ => false)
      .WaitAsync(Timeout);
    scope.Store.GetState<CounterState>().Count.ShouldBe(10);
    rejected.ShouldBe("Done.");

    using ScriptedChatClient approving = new();
    string approved = await RunAsync(approving, scope.ServiceProvider, options, static _ => true)
      .WaitAsync(Timeout);

    int count = scope.Store.GetState<CounterState>().Count;
    count.ShouldBe(15);
    approved.ShouldBe("Done.");
    Console.WriteLine
    (
      $"AGENT-PROOF action=Counter.IncrementCounter amount=5 approved count={count} reply={approved}"
    );
  }

  public static async Task Fake_Client_Refuses_Tool_No_Longer_Offered_On_Current_Page()
  {
    using SpaTestScope scope = SpaTestScope.Create(Spa!);
    NavigationManager navigation = scope.ServiceProvider.GetRequiredService<NavigationManager>();
    navigation.NavigateTo("/Counter");
    scope.Store.GetState<CounterState>().Initialize(count: 10);
    using CatalogAgentFunctions functions = CatalogAgentFunctions.Create(await SelectForSessionAsync(scope));
    ChatOptions options = new() { Tools = [.. functions.Tools] };

    // The person moves on after the modal selected its tools; the approved call must not run.
    navigation.NavigateTo("/Settings");
    using ScriptedChatClient approving = new();
    string reply = await RunAsync(approving, scope.ServiceProvider, options, static _ => true)
      .WaitAsync(Timeout);

    scope.Store.GetState<CounterState>().Count.ShouldBe(10);
    reply.ShouldBe("Done.");
  }

  public static Task ConfigureServices_Does_Not_Register_A_Chat_Client()
  {
    ServiceCollection services = new();
    // Qualified: Microsoft.Extensions.Configuration is a global using only when the api flag is on.
    Microsoft.Extensions.Configuration.IConfiguration configuration =
      new Microsoft.Extensions.Configuration.ConfigurationBuilder().Build();
    Program.ConfigureServices(services, configuration, "Testing");

    bool registered = services.Any(descriptor => descriptor.ServiceType == typeof(IChatClient));
    registered.ShouldBeFalse();
    Console.WriteLine($"NO-MODEL-PROOF ConfigureServices IChatClient registered={registered}");
    return Task.CompletedTask;
  }

  public static async Task WebMcp_Registers_Page_Tools_And_Absent_Api_Is_A_NoOp()
  {
    using SpaTestScope scope = SpaTestScope.Create(Spa!);
    IActionCatalog catalog = scope.ServiceProvider.GetRequiredService<IActionCatalog>();
    IAuthorizationService authorization = scope.ServiceProvider.GetRequiredService<IAuthorizationService>();
    ClaimsPrincipal everyone = Principal([.. PermissionIds.All]);

    IReadOnlyList<WebMcpToolDescriptor> counter = await WebMcpPublisher.DescribeAsync
    (
      everyone, authorization, catalog, "/Counter", CancellationToken.None
    );
    Names(counter).ShouldBe(["Counter.IncrementCounter", PageAgentContext.ToolName]);

    IReadOnlyList<WebMcpToolDescriptor> settings = await WebMcpPublisher.DescribeAsync
    (
      everyone, authorization, catalog, "/Settings", CancellationToken.None
    );
    Names(settings).ShouldBe
    (
      [
        "Credentials.FetchCredentials",
        "Credentials.RevokeCredential",
        "Credentials.RenameCredential",
        PageAgentContext.ToolName,
      ]
    );
    settings.ShouldNotContain(tool => tool.Name == "Credentials.AddPasskey");
    settings.ShouldNotContain(tool => tool.Name == "Credentials.LinkMicrosoft365");

    IReadOnlyList<WebMcpToolDescriptor> styleGuide = await WebMcpPublisher.DescribeAsync
    (
      Principal(PermissionIds.DeveloperAccess),
      authorization,
      catalog,
      "/StyleGuide",
      CancellationToken.None
    );
    Names(styleGuide).ShouldBe([PageAgentContext.ToolName]);

    // Denying principals on pages that do have tools: only page_context is published.
    Names
    (
      await WebMcpPublisher.DescribeAsync
      (
        everyone, authorization, catalog, "/Admin/Roles/New", CancellationToken.None
      )
    ).ShouldBe(["Role.CreateRole", PageAgentContext.ToolName]);
    Names
    (
      await WebMcpPublisher.DescribeAsync
      (
        Principal(PermissionIds.DeveloperAccess), authorization, catalog, "/Admin/Roles/New", CancellationToken.None
      )
    ).ShouldBe([PageAgentContext.ToolName]);
    Names
    (
      await WebMcpPublisher.DescribeAsync
      (
        Principal(), authorization, catalog, "/Profile", CancellationToken.None
      )
    ).ShouldBe([PageAgentContext.ToolName]);

    WebMcpApplyResult absent = await WebMcpRegistration.ApplyAsync(null, counter, CancellationToken.None);
    absent.Available.ShouldBeFalse();
    absent.Registered.ShouldBe(0);

    RecordingModelContext recording = new();
    WebMcpApplyResult applied = await WebMcpRegistration.ApplyAsync(recording, counter, CancellationToken.None);
    applied.Available.ShouldBeTrue();
    applied.Registered.ShouldBe(counter.Count);
    Names(recording.Tools).ShouldBe(Names(counter));
    Console.WriteLine($"WEBMCP-PROOF path=/Counter tools={string.Join(",", Names(counter))} registered={applied.Registered}");
    Console.WriteLine($"WEBMCP-PROOF path=/Settings tools={string.Join(",", Names(settings))}");
  }

  public static async Task WebMcp_Invoke_Dispatches_After_In_App_Approval()
  {
    using SpaTestScope scope = SpaTestScope.Create(Spa!);
    scope.ServiceProvider.GetRequiredService<NavigationManager>().NavigateTo("/Counter");
    scope.Store.GetState<CounterState>().Initialize(count: 10);
    WebMcpDispatcher dispatcher = scope.ServiceProvider.GetRequiredService<WebMcpDispatcher>();

    Task<string> invoke = dispatcher.InvokeTool("Counter.IncrementCounter", """{"amount":5,"extra":"ignored"}""");
    Guid callId = await WaitForPendingAsync(scope, invoke);

    AgentSurfaceState surface = scope.Store.GetState<AgentSurfaceState>();
    surface.PendingToolName.ShouldBe("Counter.IncrementCounter");
    surface.PendingArgumentsJson.ShouldNotBeNull().ShouldContain("\"amount\": 5");
    surface.PendingArgumentsJson.ShouldNotContain("extra");
    scope.Store.GetState<CounterState>().Count.ShouldBe(10);

    await surface.ResolveApproval(callId, approved: true);
    string result = await invoke.WaitAsync(Timeout);
    int count = scope.Store.GetState<CounterState>().Count;
    count.ShouldBe(15);
    result.ShouldContain("\"completed\":true");
    scope.Store.GetState<AgentSurfaceState>().HasPendingApproval.ShouldBeFalse();
    Console.WriteLine($"WEBMCP-PROOF invoke Counter.IncrementCounter approved count={count} result={result}");
  }

  public static async Task WebMcp_Invoke_Reject_Leaves_Counter_Unchanged()
  {
    using SpaTestScope scope = SpaTestScope.Create(Spa!);
    scope.ServiceProvider.GetRequiredService<NavigationManager>().NavigateTo("/Counter");
    scope.Store.GetState<CounterState>().Initialize(count: 10);
    WebMcpDispatcher dispatcher = scope.ServiceProvider.GetRequiredService<WebMcpDispatcher>();

    Task<string> invoke = dispatcher.InvokeTool("Counter.IncrementCounter", """{"amount":5}""");
    Guid callId = await WaitForPendingAsync(scope, invoke);
    await scope.Store.GetState<AgentSurfaceState>().ResolveApproval(callId, approved: false);
    string result = await invoke.WaitAsync(Timeout);

    result.ShouldContain("\"approved\":false");
    scope.Store.GetState<CounterState>().Count.ShouldBe(10);
    scope.Store.GetState<AgentSurfaceState>().HasPendingApproval.ShouldBeFalse();
  }

  public static async Task WebMcp_Invoke_Refuses_Off_Page_Human_Only_Unknown_And_Unbindable_Calls()
  {
    using SpaTestScope scope = SpaTestScope.Create(Spa!);
    NavigationManager navigation = scope.ServiceProvider.GetRequiredService<NavigationManager>();
    scope.Store.GetState<CounterState>().Initialize(count: 10);
    WebMcpDispatcher dispatcher = scope.ServiceProvider.GetRequiredService<WebMcpDispatcher>();

    navigation.NavigateTo("/Counter");
    (await dispatcher.InvokeTool("Role.CreateRole", """{"command":{"name":"x"}}""").WaitAsync(Timeout))
      .ShouldContain(WebMcpDispatcher.UnavailableError);
    (await dispatcher.InvokeTool("Nope.Missing", null).WaitAsync(Timeout))
      .ShouldContain(WebMcpDispatcher.UnavailableError);
    (await dispatcher.InvokeTool("Counter.IncrementCounter", """{"amount":"many"}""").WaitAsync(Timeout))
      .ShouldContain("\"error\":");
    (await dispatcher.InvokeTool("Counter.IncrementCounter", "[1]").WaitAsync(Timeout))
      .ShouldContain("\"error\":");

    navigation.NavigateTo("/Settings");
    (await dispatcher.InvokeTool("Credentials.AddPasskey", null).WaitAsync(Timeout))
      .ShouldContain(WebMcpDispatcher.UnavailableError);

    scope.Store.GetState<AgentSurfaceState>().HasPendingApproval.ShouldBeFalse();
    scope.Store.GetState<CounterState>().Count.ShouldBe(10);
  }

  public static async Task WebMcp_Second_Call_While_Pending_Is_Refused_And_Stale_Id_Is_A_NoOp()
  {
    using SpaTestScope scope = SpaTestScope.Create(Spa!);
    scope.ServiceProvider.GetRequiredService<NavigationManager>().NavigateTo("/Counter");
    scope.Store.GetState<CounterState>().Initialize(count: 10);
    WebMcpDispatcher dispatcher = scope.ServiceProvider.GetRequiredService<WebMcpDispatcher>();

    Task<string> first = dispatcher.InvokeTool("Counter.IncrementCounter", """{"amount":5}""");
    Guid callId = await WaitForPendingAsync(scope, first);

    string second = await dispatcher.InvokeTool("Counter.IncrementCounter", """{"amount":100}""")
      .WaitAsync(Timeout);
    second.ShouldContain(WebMcpDispatcher.BusyError);
    scope.Store.GetState<AgentSurfaceState>().PendingCallId.ShouldBe(callId);

    await scope.Store.GetState<AgentSurfaceState>().ResolveApproval(Guid.NewGuid(), approved: true);
    first.IsCompleted.ShouldBeFalse();
    scope.Store.GetState<AgentSurfaceState>().PendingCallId.ShouldBe(callId);
    scope.Store.GetState<CounterState>().Count.ShouldBe(10);

    await scope.Store.GetState<AgentSurfaceState>().ResolveApproval(callId, approved: true);
    (await first.WaitAsync(Timeout)).ShouldContain("\"completed\":true");
    scope.Store.GetState<CounterState>().Count.ShouldBe(15);
  }

  public static async Task WebMcp_Navigation_Cancels_Pending_Call_And_Late_Approval_Does_Not_Execute()
  {
    using SpaTestScope scope = SpaTestScope.Create(Spa!);
    NavigationManager navigation = scope.ServiceProvider.GetRequiredService<NavigationManager>();
    navigation.NavigateTo("/Counter");
    scope.Store.GetState<CounterState>().Initialize(count: 10);
    WebMcpDispatcher dispatcher = scope.ServiceProvider.GetRequiredService<WebMcpDispatcher>();

    Task<string> invoke = dispatcher.InvokeTool("Counter.IncrementCounter", """{"amount":5}""");
    Guid callId = await WaitForPendingAsync(scope, invoke);

    navigation.NavigateTo("/Settings");
    string result = await invoke.WaitAsync(Timeout);
    result.ShouldContain(WebMcpDispatcher.PageChangedError);
    scope.Store.GetState<AgentSurfaceState>().HasPendingApproval.ShouldBeFalse();

    navigation.NavigateTo("/Counter");
    await scope.Store.GetState<AgentSurfaceState>().ResolveApproval(callId, approved: true);
    scope.Store.GetState<CounterState>().Count.ShouldBe(10);
  }

  public static async Task WebMcp_Same_Path_Navigation_Reports_Page_Changed_Not_Rejected()
  {
    using SpaTestScope scope = SpaTestScope.Create(Spa!);
    NavigationManager navigation = scope.ServiceProvider.GetRequiredService<NavigationManager>();
    navigation.NavigateTo("/Counter");
    scope.Store.GetState<CounterState>().Initialize(count: 10);
    WebMcpDispatcher dispatcher = scope.ServiceProvider.GetRequiredService<WebMcpDispatcher>();

    Task<string> invoke = dispatcher.InvokeTool("Counter.IncrementCounter", """{"amount":5}""");
    await WaitForPendingAsync(scope, invoke);

    navigation.NavigateTo("/Counter?tab=1");
    string result = await invoke.WaitAsync(Timeout);
    result.ShouldContain(WebMcpDispatcher.PageChangedError);
    result.ShouldNotContain("\"approved\":false");
    scope.Store.GetState<CounterState>().Count.ShouldBe(10);
  }

  public static Task Page_Context_Carries_Profile_And_Site_Settings_Records()
  {
    using SpaTestScope scope = SpaTestScope.Create(Spa!);

    using JsonDocument profile = JsonDocument.Parse(PageAgentContext.Describe(scope.Store, "/Profile"));
    JsonElement profileRecord = profile.RootElement.GetProperty("profile");
    foreach (string field in new[] { "alias", "email", "language", "region", "theme", "notifications" })
    {
      profileRecord.TryGetProperty(field, out _).ShouldBeTrue(field);
    }

    using JsonDocument settings = JsonDocument.Parse(PageAgentContext.Describe(scope.Store, "/Admin/Authentication"));
    JsonElement settingsRecord = settings.RootElement.GetProperty("siteSettings");
    settingsRecord.GetProperty("version").GetInt64().ShouldBe(scope.Store.GetState<SiteSettingsState>().Version);
    settingsRecord.GetProperty("passkeyPromptMode").GetString()
      .ShouldBe(scope.Store.GetState<SiteSettingsState>().PasskeyPromptMode.ToString());
    settingsRecord.TryGetProperty("entraSignInEnabled", out _).ShouldBeTrue();
    return Task.CompletedTask;
  }

  private static async Task<Guid> WaitForPendingAsync(SpaTestScope scope, Task<string> invoke)
  {
    DateTime started = DateTime.UtcNow;
    while (scope.Store.GetState<AgentSurfaceState>().PendingCallId is null)
    {
      if (invoke.IsCompleted)
      {
        throw new ShouldAssertException($"Invoke finished without waiting for approval: {await invoke}");
      }

      if (DateTime.UtcNow - started > Timeout)
      {
        throw new ShouldAssertException("No approval became pending.");
      }

      await Task.Delay(20);
    }

    return scope.Store.GetState<AgentSurfaceState>().PendingCallId!.Value;
  }

  private static async Task<IReadOnlyList<CatalogAgentTool>> SelectForSessionAsync(SpaTestScope scope)
  {
    AuthenticationState authentication = await scope.ServiceProvider
      .GetRequiredService<AuthenticationStateProvider>()
      .GetAuthenticationStateAsync();
    return await CatalogAgentToolSet.SelectAsync
    (
      authentication.User,
      scope.ServiceProvider.GetRequiredService<IAuthorizationService>(),
      scope.ServiceProvider.GetRequiredService<IActionCatalog>().Entries,
      PageAgentScope.FromNavigation(scope.ServiceProvider.GetRequiredService<NavigationManager>()),
      CancellationToken.None
    );
  }

  /// <summary>Scripted approval loop over the product's invoking client (test-only).</summary>
  private static async Task<string> RunAsync
  (
    IChatClient inner,
    IServiceProvider services,
    ChatOptions options,
    Func<ToolApprovalRequestContent, bool> approve
  )
  {
    const int maximumApprovalRounds = 8;
    using FunctionInvokingChatClient client = CatalogAgentSession.CreateInvokingClient(inner, services);
    List<ChatMessage> history = [new ChatMessage(ChatRole.User, "Add 5 to the counter")];
    for (int round = 0; round < maximumApprovalRounds; round++)
    {
      ChatResponse response = await client.GetResponseAsync(history, options, CancellationToken.None);
      history.AddRange(response.Messages);

      List<AIContent> decisions = [];
      foreach (ChatMessage message in response.Messages)
      {
        foreach (AIContent content in message.Contents)
        {
          if (content is ToolApprovalRequestContent request)
          {
            decisions.Add(request.CreateResponse(approve(request)));
          }
        }
      }

      if (decisions.Count == 0)
      {
        return response.Text ?? "";
      }

      history.Add(new ChatMessage(ChatRole.User, decisions));
    }

    throw new InvalidOperationException($"The catalog agent stopped after {maximumApprovalRounds} approval rounds.");
  }

  private static ClaimsPrincipal Principal(params string[] permissions)
  {
    List<Claim> claims = [new Claim(ClaimTypes.Name, "catalog-agent")];
    foreach (string permission in permissions)
    {
      claims.Add(new Claim(PermissionIds.ClaimType, permission));
    }

    return new ClaimsPrincipal(new ClaimsIdentity(claims, authenticationType: "Test"));
  }

  private static string[] Names(IReadOnlyList<CatalogAgentTool> tools)
  {
    string[] names = new string[tools.Count];
    for (int index = 0; index < tools.Count; index++)
    {
      names[index] = tools[index].Name;
    }

    return names;
  }

  private static string[] Names(IReadOnlyList<AITool> tools)
  {
    string[] names = new string[tools.Count];
    for (int index = 0; index < tools.Count; index++)
    {
      names[index] = tools[index].Name;
    }

    return names;
  }

  private static string[] Names(IReadOnlyList<WebMcpToolDescriptor> tools)
  {
    string[] names = new string[tools.Count];
    for (int index = 0; index < tools.Count; index++)
    {
      names[index] = tools[index].Name;
    }

    return names;
  }

  private sealed class RecordingModelContext : IWebMcpModelContext
  {
    public List<WebMcpToolDescriptor> Tools { get; } = [];

    public ValueTask<WebMcpApplyResult> ReplaceAsync
    (
      IReadOnlyList<WebMcpToolDescriptor> tools,
      CancellationToken cancellationToken
    )
    {
      Tools.Clear();
      Tools.AddRange(tools);
      return ValueTask.FromResult(new WebMcpApplyResult(Available: true, Registered: tools.Count));
    }
  }

  private sealed class ScriptedChatClient : IChatClient
  {
    private bool OfferedCall;

    public Task<ChatResponse> GetResponseAsync
    (
      IEnumerable<ChatMessage> messages,
      ChatOptions? options = null,
      CancellationToken cancellationToken = default
    )
    {
      if (!OfferedCall)
      {
        OfferedCall = true;
        FunctionCallContent call = new
        (
          "call-increment",
          "Counter.IncrementCounter",
          new Dictionary<string, object?> { ["amount"] = 5 }
        );
        return Task.FromResult(new ChatResponse(new ChatMessage(ChatRole.Assistant, [call])));
      }

      return Task.FromResult(new ChatResponse(new ChatMessage(ChatRole.Assistant, "Done.")));
    }

    public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync
    (
      IEnumerable<ChatMessage> messages,
      ChatOptions? options = null,
      [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default
    )
    {
      ChatResponse response = await GetResponseAsync(messages, options, cancellationToken);
      foreach (ChatMessage message in response.Messages)
      {
        yield return new ChatResponseUpdate(message.Role, message.Contents);
      }
    }

    public object? GetService(Type serviceType, object? serviceKey = null) => null;

    public void Dispose() { }
  }
}

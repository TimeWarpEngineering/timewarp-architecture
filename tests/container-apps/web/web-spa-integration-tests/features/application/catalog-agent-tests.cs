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
// stalling the suite. The feedback submit receipt runs on an in-proc FeedbackSpa (C-create) whose
// scripted IWebServerApiService answers SubmitFeedback with a fresh id: the closed-box Aspire SPA
// registers no web-server BFF client and its mock session has no profile store to file against.
// That test drives the real path — dispatcher, approval, ActionSet handler, AgentCallOutcome, and
// the dispatcher's own result serialization — and asserts the JSON the agent receives.
// page_context on /Feedback is driven through CatalogAgentSession.CreateInvokingClient with the
// same scope the panel passes in. A wrapper that answers NavigationManager with a manager stuck
// at the base URI still returns the observed /Feedback route. RelayChatClient is asserted to send
// tool arguments and tool results as JSON, never a dictionary's CLR type name.
#endregion

namespace CatalogAgent_;

using System.Reflection;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.AI;
using TimeWarp.Architecture;
using TimeWarp.Architecture.Components;
using TimeWarp.Architecture.Features;
using TimeWarp.Architecture.Features.AgentChats;
using TimeWarp.Architecture.Features.Feedback;
using TimeWarp.Architecture.Features.Settings;
using TimeWarp.Architecture.Web.Spa;
using TimeWarp.Foundation.Features;
using TimeWarp.Identity;

[TestTag("Integration")]
public partial class CatalogAgent_Should
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
    Names(counter).ShouldBe(await ExpectedNamesAsync(everyone, authorization, catalog, "/Counter?from=palette"));
    counter.Single(tool => tool.Name == "Counter.IncrementCounter").RequiresApproval.ShouldBeTrue();
    counter.ShouldContain(tool => tool.Name == AgentNavigate.ToolName && !tool.RequiresApproval);
    counter.Single(tool => tool.Name == "Credentials.AddPasskey").OffPageRoute.ShouldBe("/Settings");

    IReadOnlyList<CatalogAgentTool> settings = await CatalogAgentToolSet.SelectAsync
    (
      everyone, authorization, catalog.Entries, "/Settings/", CancellationToken.None
    );
    Names(settings).ShouldBe(await ExpectedNamesAsync(everyone, authorization, catalog, "/Settings/"));
    settings.Single(tool => tool.Name == "Credentials.FetchCredentials").RequiresApproval.ShouldBeFalse();
    settings.Single(tool => tool.Name == "Credentials.RevokeCredential").RequiresApproval.ShouldBeTrue();
    settings.Single(tool => tool.Name == "Credentials.AddPasskey").OffPageRoute.ShouldBeNull();
    settings.Single(tool => tool.Name == "Credentials.AddPasskey").RequiresApproval.ShouldBeTrue();

    settings.ShouldNotContain(tool => tool.Name == "Credentials.AddExistingPasskey");
    settings.ShouldNotContain(tool => tool.Name == "Credentials.LinkMicrosoft365");
    settings.ShouldNotContain(tool => tool.Name == "Profile.SignOut");

    IReadOnlyList<CatalogAgentTool> newRole = await CatalogAgentToolSet.SelectAsync
    (
      everyone, authorization, catalog.Entries, "/Admin/Roles/New", CancellationToken.None
    );
    Names(newRole).ShouldBe(await ExpectedNamesAsync(everyone, authorization, catalog, "/Admin/Roles/New"));
    newRole.ShouldContain(tool => tool.Name == "Role.CreateRole");

    IReadOnlyList<CatalogAgentTool> developerNewRole = await CatalogAgentToolSet.SelectAsync
    (
      developer, authorization, catalog.Entries, "/Admin/Roles/New", CancellationToken.None
    );
    Names(developerNewRole).ShouldBe([AgentNavigate.ToolName]);
    developerNewRole.ShouldNotContain(tool => tool.Name == "Role.CreateRole");

    Names
    (
      await CatalogAgentToolSet.SelectAsync
      (
        everyone, authorization, catalog.Entries, "/", CancellationToken.None
      )
    ).ShouldBe(await ExpectedNamesAsync(everyone, authorization, catalog, "/"));

    (
      await CatalogAgentToolSet.SelectAsync
      (
        anonymous, authorization, catalog.Entries, "/", CancellationToken.None
      )
    ).Select(tool => tool.Name).ShouldBe([AgentNavigate.ToolName]);
  }

  public static async Task Fake_Client_Dispatches_Increment_Only_After_Approval()
  {
    using SpaTestScope scope = SpaTestScope.Create(Spa!);
    scope.ServiceProvider.GetRequiredService<NavigationManager>().NavigateTo("/Counter");
    scope.Store.GetState<CounterState>().Initialize(count: 10);
    using CatalogAgentFunctions functions = CatalogAgentFunctions.Create(await SelectForSessionAsync(scope));
    ChatOptions options = new() { Tools = [.. functions.Tools] };
    functions.Tools.Single(tool => tool.Name == "Counter.IncrementCounter").ShouldBeOfType<ApprovalRequiredAIFunction>();
    functions.Tools.Single(tool => tool.Name == AgentNavigate.ToolName).ShouldNotBeOfType<ApprovalRequiredAIFunction>();
    functions.Tools.Single(tool => tool.Name == PageAgentContext.ToolName).ShouldNotBeOfType<ApprovalRequiredAIFunction>();

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

  public static Task ConfigureServices_Registers_Relay_Without_A_Key()
  {
    ServiceCollection services = new();
    // Qualified: Microsoft.Extensions.Configuration is a global using only when the api flag is on.
    Microsoft.Extensions.Configuration.IConfiguration configuration =
      new Microsoft.Extensions.Configuration.ConfigurationBuilder().Build();
    Program.ConfigureServices(services, configuration, "Testing");

    ServiceDescriptor relay = services.Single(descriptor =>
      descriptor.ServiceType == typeof(IChatClient) && descriptor.ServiceKey is null);
    relay.ImplementationType.ShouldBe(typeof(RelayChatClient));
    services.Any(descriptor => descriptor.ServiceKey is not null && descriptor.ServiceType == typeof(IChatClient))
      .ShouldBeFalse();
    typeof(RelayChatClient).GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
      .Any(field => field.Name.Contains("ApiKey", StringComparison.Ordinal) || field.Name.Contains("Key", StringComparison.Ordinal))
      .ShouldBeFalse();
    Console.WriteLine("RELAY-PROOF IChatClient=RelayChatClient keyedXai=False");
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
    Names(counter).ShouldBe(await ExpectedWebMcpNamesAsync(everyone, authorization, catalog, "/Counter"));

    IReadOnlyList<WebMcpToolDescriptor> home = await WebMcpPublisher.DescribeAsync
    (
      everyone, authorization, catalog, "/", CancellationToken.None
    );
    Names(home).ShouldBe(await ExpectedWebMcpNamesAsync(everyone, authorization, catalog, "/"));
    home.ShouldContain(tool => tool.Name == "Credentials.AddPasskey");
    home.ShouldContain(tool => tool.Name == AgentNavigate.ToolName);
    home.ShouldContain(tool => tool.Name == PageAgentContext.ToolName);
    home.ShouldNotContain(tool => tool.Name == "Profile.SignOut");
    home.ShouldNotContain(tool => tool.Name == "Credentials.AddExistingPasskey");
    Console.WriteLine($"WEBMCP-PROOF path=/ tools={string.Join(",", Names(home))}");

    IReadOnlyList<WebMcpToolDescriptor> settings = await WebMcpPublisher.DescribeAsync
    (
      everyone, authorization, catalog, "/Settings", CancellationToken.None
    );
    Names(settings).ShouldBe(await ExpectedWebMcpNamesAsync(everyone, authorization, catalog, "/Settings"));
    settings.ShouldContain(tool => tool.Name == "Credentials.AddPasskey");
    settings.ShouldNotContain(tool => tool.Name == "Credentials.LinkMicrosoft365");

    IReadOnlyList<WebMcpToolDescriptor> styleGuide = await WebMcpPublisher.DescribeAsync
    (
      Principal(PermissionIds.DeveloperAccess),
      authorization,
      catalog,
      "/StyleGuide",
      CancellationToken.None
    );
    Names(styleGuide).ShouldBe([AgentNavigate.ToolName, PageAgentContext.ToolName]);

    Names
    (
      await WebMcpPublisher.DescribeAsync
      (
        everyone, authorization, catalog, "/Admin/Roles/New", CancellationToken.None
      )
    ).ShouldBe(await ExpectedWebMcpNamesAsync(everyone, authorization, catalog, "/Admin/Roles/New"));
    Names
    (
      await WebMcpPublisher.DescribeAsync
      (
        Principal(PermissionIds.DeveloperAccess), authorization, catalog, "/Admin/Roles/New", CancellationToken.None
      )
    ).ShouldBe([AgentNavigate.ToolName, PageAgentContext.ToolName]);
    Names
    (
      await WebMcpPublisher.DescribeAsync
      (
        Principal(), authorization, catalog, "/Profile", CancellationToken.None
      )
    ).ShouldBe([AgentNavigate.ToolName, PageAgentContext.ToolName]);

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
    (await dispatcher.InvokeTool("Credentials.AddExistingPasskey", null).WaitAsync(Timeout))
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

  public static async Task Feedback_Tools_Are_Page_Scoped_And_Read_Only_Except_Submit()
  {
    using SpaTestScope scope = SpaTestScope.Create(Spa!);
    IActionCatalog catalog = scope.ServiceProvider.GetRequiredService<IActionCatalog>();
    IAuthorizationService authorization = scope.ServiceProvider.GetRequiredService<IAuthorizationService>();
    ClaimsPrincipal everyone = Principal([.. PermissionIds.All]);

    IReadOnlyList<WebMcpToolDescriptor> feedback = await WebMcpPublisher.DescribeAsync
    (
      everyone, authorization, catalog, "/Feedback", CancellationToken.None
    );
    Names(feedback).ShouldBe(await ExpectedWebMcpNamesAsync(everyone, authorization, catalog, "/Feedback"));
    feedback.ShouldContain(tool => tool.Name == "Feedback.SubmitFeedback");
    feedback.ShouldContain(tool => tool.Name == "Feedback.ListMyFeedback");
    feedback.ShouldContain(tool => tool.Name == "Feedback.OpenFeedback");

    Guid itemId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
    IReadOnlyList<WebMcpToolDescriptor> detail = await WebMcpPublisher.DescribeAsync
    (
      everyone, authorization, catalog, $"/Feedback/{itemId:D}", CancellationToken.None
    );
    Names(detail).ShouldBe(Names(feedback));

    Names
    (
      await WebMcpPublisher.DescribeAsync
      (
        everyone, authorization, catalog, "/FeedbackExtra", CancellationToken.None
      )
    ).ShouldBe(await ExpectedWebMcpNamesAsync(everyone, authorization, catalog, "/FeedbackExtra"));

    ActionCatalogEntry submit = catalog.Find("Feedback.SubmitFeedback").ShouldNotBeNull();
    ActionCatalogEntry list = catalog.Find("Feedback.ListMyFeedback").ShouldNotBeNull();
    ActionCatalogEntry open = catalog.Find("Feedback.OpenFeedback").ShouldNotBeNull();
    CatalogAgentApproval.RequiresApproval(submit).ShouldBeTrue();
    CatalogAgentApproval.RequiresApproval(list).ShouldBeFalse();
    CatalogAgentApproval.RequiresApproval(open).ShouldBeFalse();
    submit.Permissions.ShouldBe([PermissionIds.FeedbackFileSelf]);
    submit.Visibility.ShouldBe(ActionVisibility.Both);

    string schema = CatalogAgentSchema.For(submit);
    schema.ShouldContain("BugReport");
    schema.ShouldContain("FeatureRequest");
    schema.ShouldContain("Complaint");
    schema.ShouldContain("Other");
    schema.ShouldContain("emailCopy");

    object?[] bound = CatalogAgentArguments.Bind
    (
      submit,
      new Dictionary<string, object?>
      {
        ["kind"] = "Complaint",
        ["title"] = "The export failed",
        ["body"] = "Nothing came back.",
        ["emailCopy"] = false,
      }
    );
    bound[0].ShouldBe(FeedbackKind.Complaint);
    bound[1].ShouldBe("The export failed");
    bound[2].ShouldBe("Nothing came back.");
    bound[3].ShouldBe(false);

    Console.WriteLine($"WEBMCP-PROOF path=/Feedback tools={string.Join(",", Names(feedback))}");
  }

  public static async Task WebMcp_Approved_Submit_Feedback_Returns_The_Real_Receipt()
  {
    using FeedbackSpa spa = new();
    using SpaTestScope scope = SpaTestScope.Create(spa);
    scope.ServiceProvider.GetRequiredService<NavigationManager>().NavigateTo("/Feedback");
    WebMcpDispatcher dispatcher = scope.ServiceProvider.GetRequiredService<WebMcpDispatcher>();

    Task<string> invoke = dispatcher.InvokeTool
    (
      "Feedback.SubmitFeedback",
      """{"kind":"Complaint","title":"The export failed","body":"Nothing came back.","emailCopy":false}"""
    );
    Guid callId = await WaitForPendingAsync(scope, invoke);
    spa.Api.Requests.ShouldBeEmpty();
    await scope.Store.GetState<AgentSurfaceState>().ResolveApproval(callId, approved: true);
    string receipt = await invoke.WaitAsync(Timeout);

    SubmitFeedback.Command sent = spa.Api.Requests.ShouldHaveSingleItem();
    sent.Kind.ShouldBe(FeedbackKind.Complaint);
    sent.Title.ShouldBe("The export failed");
    using JsonDocument document = JsonDocument.Parse(receipt);
    JsonElement root = document.RootElement;
    root.GetProperty("action").GetString().ShouldBe("Feedback.SubmitFeedback");
    root.GetProperty("completed").GetBoolean().ShouldBeTrue();
    JsonElement result = root.GetProperty("result");
    Guid feedbackItemId = result.GetProperty("feedbackItemId").GetGuid();
    feedbackItemId.ShouldBe(spa.Api.LastId.ShouldNotBeNull());
    result.GetProperty("permalink").GetString().ShouldBe($"/Feedback/{feedbackItemId:D}");
    result.GetProperty("emailCopySent").GetBoolean().ShouldBeFalse();

    FeedbackState state = scope.Store.GetState<FeedbackState>();
    state.LastReceipt.ShouldNotBeNull().FeedbackItemId.ShouldBe(feedbackItemId);
    state.Items.ShouldNotBeEmpty();
    state.Items[0].FeedbackItemId.ShouldBe(feedbackItemId);
    state.Items[0].Permalink.ShouldBe($"/Feedback/{feedbackItemId:D}");
    Console.WriteLine($"WEBMCP-PROOF invoke Feedback.SubmitFeedback approved receipt={receipt}");
  }

  public static async Task Home_Page_Context_Carries_Title_Purpose_Tools_And_Headings()
  {
    using SpaTestScope scope = SpaTestScope.Create(Spa!);
    string bare = PageAgentContext.Describe(scope.Store, "/");
    using (JsonDocument document = JsonDocument.Parse(bare))
    {
      document.RootElement.GetProperty("path").GetString().ShouldBe("/");
      document.RootElement.GetProperty("title").GetString().ShouldBe("Home");
      document.RootElement.GetProperty("purpose").GetString()
        .ShouldBe("Public welcome page for TimeWarp.Architecture, with a sign-in entry.");
      document.RootElement.GetProperty("tools").GetArrayLength().ShouldBeGreaterThan(1);
      document.RootElement.GetProperty("screenshot").ValueKind.ShouldBe(JsonValueKind.Null);
    }

    bare.ShouldContain("\"title\"");
    bare.Length.ShouldBeGreaterThan("{\"path\":\"/\"}".Length);

    await scope.Send
    (
      new AgentSurfaceState.RememberPageSurfaceActionSet.Action
      (
        """
        {"headings":["Welcome to TimeWarp.Architecture","Built with","Signed in"],"summary":"Welcome to TimeWarp.Architecture. Built with. Signed in.","forms":[],"buttons":[],"items":[]}
        """
      )
    );
    string described = PageAgentContext.Describe(scope.Store, "/");
    described.ShouldContain("Welcome to TimeWarp.Architecture");
    described.ShouldContain("Built with");
    described.ShouldContain("Signed in");
    Console.WriteLine("PAGE-CONTEXT / " + described);

    string feedback = PageAgentContext.Describe(scope.Store, "/Feedback");
    feedback.ShouldContain("File feedback and review the filings you submitted.");
    feedback.ShouldContain("\"title\":\"Feedback\"");
    Console.WriteLine("PAGE-CONTEXT /Feedback " + feedback);
  }

  public static async Task Navigate_Matches_Palette_Pages_And_Refuses_The_Rest()
  {
    using SpaTestScope scope = SpaTestScope.Create(Spa!);
    IActionCatalog catalog = scope.ServiceProvider.GetRequiredService<IActionCatalog>();
    IAuthorizationService authorization = scope.ServiceProvider.GetRequiredService<IAuthorizationService>();
    ClaimsPrincipal member = Principal(PermissionIds.ProfileRead, PermissionIds.SettingsRead);
    IReadOnlyList<CatalogAgentTool> tools = await CatalogAgentToolSet.SelectAsync
    (
      member, authorization, catalog.Entries, "/", CancellationToken.None
    );
    tools.ShouldNotContain(tool => tool.Name == "Credentials.AddPasskey");
    tools.ShouldNotContain(tool => tool.Name == "Feedback.ListMyFeedback");
    CatalogAgentTool navigate = tools.Single(tool => tool.Name == AgentNavigate.ToolName);
    navigate.RequiresApproval.ShouldBeFalse();
    navigate.InputSchema.ShouldContain("\"/Settings\"");
    navigate.InputSchema.ShouldContain("\"/Profile\"");
    navigate.InputSchema.ShouldContain("\"/\"");
    navigate.InputSchema.ShouldNotContain("\"/Admin/Roles\"");
    navigate.InputSchema.ShouldNotContain("\"/Counter\"");
    navigate.InputSchema.ShouldNotContain("\"/Feedback\"");

    NavigationManager navigation = scope.ServiceProvider.GetRequiredService<NavigationManager>();
    string before = navigation.Uri;
    AgentNavigate.Result refused = await AgentNavigate.InvokeAsync
    (
      member,
      authorization,
      catalog.Entries,
      scope.Store,
      "/",
      new Dictionary<string, object?> { ["url"] = "/Admin/Roles" },
      CancellationToken.None
    );
    refused.Navigated.ShouldBeFalse();
    refused.Error.ShouldBe(AgentNavigate.Refusal);
    navigation.Uri.ShouldBe(before);

    AgentNavigate.Result allowed = await AgentNavigate.InvokeAsync
    (
      member,
      authorization,
      catalog.Entries,
      scope.Store,
      "/",
      new Dictionary<string, object?> { ["url"] = "/Settings" },
      CancellationToken.None
    );
    allowed.Navigated.ShouldBeTrue();
    allowed.Destination.ShouldBe("/Settings");
    navigation.Uri.ShouldContain("/Settings");

    navigation.NavigateTo("/");
    WebMcpDispatcher dispatcher = scope.ServiceProvider.GetRequiredService<WebMcpDispatcher>();
    string missing = await dispatcher.InvokeTool("navigate", """{"url":"/not-a-page"}""").WaitAsync(Timeout);
    missing.ShouldContain(AgentNavigate.Refusal);
    missing.ShouldContain("\"navigated\":false");
  }

  public static async Task AddPasskey_Off_Settings_Offers_Navigation_And_On_Settings_Runs()
  {
    using SpaTestScope scope = SpaTestScope.Create(Spa!);
    NavigationManager navigation = scope.ServiceProvider.GetRequiredService<NavigationManager>();
    navigation.NavigateTo("/");
    int messages = scope.Store.GetState<NotificationState>().Messages.Count;
    using CatalogAgentFunctions homeFunctions = CatalogAgentFunctions.Create(await SelectForSessionAsync(scope));
    AIFunction homeAdd = homeFunctions.Tools.Single(tool => tool.Name == "Credentials.AddPasskey")
      .ShouldBeAssignableTo<AIFunction>();
    homeAdd.ShouldNotBeOfType<ApprovalRequiredAIFunction>();
    object? asked = await homeAdd.InvokeAsync
    (
      new AIFunctionArguments(new Dictionary<string, object?>()) { Services = scope.ServiceProvider }
    );
    AgentNavigate.Offer offer = asked.ShouldBeOfType<AgentNavigate.Offer>();
    offer.Executed.ShouldBeFalse();
    offer.NavigateTo.ShouldBe("/Settings");
    offer.Message.ShouldBe("Add passkey is on Settings; I can take you to the Settings page.");
    scope.Store.GetState<AgentSurfaceState>().HasPendingApproval.ShouldBeFalse();
    scope.Store.GetState<NotificationState>().Messages.Count.ShouldBe(messages);

    WebMcpDispatcher dispatcher = scope.ServiceProvider.GetRequiredService<WebMcpDispatcher>();
    string json = await dispatcher.InvokeTool("Credentials.AddPasskey", null).WaitAsync(Timeout);
    using (JsonDocument document = JsonDocument.Parse(json))
    {
      document.RootElement.GetProperty("executed").GetBoolean().ShouldBeFalse();
      document.RootElement.GetProperty("navigateTo").GetString().ShouldBe("/Settings");
      document.RootElement.GetProperty("message").GetString().ShouldBe(offer.Message);
    }

    scope.Store.GetState<AgentSurfaceState>().HasPendingApproval.ShouldBeFalse();
    scope.Store.GetState<NotificationState>().Messages.Count.ShouldBe(messages);

    navigation.NavigateTo("/Settings");
    using CatalogAgentFunctions settingsFunctions = CatalogAgentFunctions.Create(await SelectForSessionAsync(scope));
    settingsFunctions.Tools.Single(tool => tool.Name == "Credentials.AddPasskey")
      .ShouldBeOfType<ApprovalRequiredAIFunction>();
    bool askedApproval = false;
    using ScriptedChatClient client = new("Credentials.AddPasskey", new Dictionary<string, object?>());
    await RunAsync
    (
      client,
      scope.ServiceProvider,
      new ChatOptions { Tools = [.. settingsFunctions.Tools] },
      _ =>
      {
        askedApproval = true;
        return false;
      }
    ).WaitAsync(Timeout);
    askedApproval.ShouldBeTrue();
    scope.Store.GetState<NotificationState>().Messages.Count.ShouldBe(messages);

    Task<string> pending = dispatcher.InvokeTool("Credentials.AddPasskey", null);
    Guid callId = await WaitForPendingAsync(scope, pending);
    await scope.Store.GetState<AgentSurfaceState>().ResolveApproval(callId, approved: false);
    (await pending.WaitAsync(Timeout)).ShouldContain("\"approved\":false");

    await scope.Send(new AgentSurfaceState.SetEditModeActionSet.Action(AgentEditMode.AutomaticallyEdit));
    // The closed-box Aspire SPA registers no web-server BFF client, so the handler cannot finish
    // the ceremony. Reaching that activation proves this was an execute, not a navigate offer.
    InvalidOperationException thrown = await Should.ThrowAsync<InvalidOperationException>(
      () => dispatcher.InvokeTool("Credentials.AddPasskey", null).WaitAsync(Timeout));
    thrown.Message.ShouldContain("IWebServerApiService");
    thrown.Message.ShouldNotContain("navigateTo");
    scope.Store.GetState<AgentSurfaceState>().HasPendingApproval.ShouldBeFalse();
  }

  public static Task Instructions_Stay_Under_The_Contract_Cap()
  {
    string text = AgentAskInstructions.For(new string('x', 20_000));
    text.Length.ShouldBeLessThanOrEqualTo(AgentAskInstructions.MaxLength);
    text.Length.ShouldBeLessThan(CompleteAgentChat.MaxInstructionsLength);
    text.ShouldStartWith("You can run global actions and navigate from any page");
    AgentAskInstructions.Preface.ShouldNotContain("Drive this page only");
    return Task.CompletedTask;
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

  public static Task Arguments_Render_As_Json_And_Empty_Payloads_Are_Omitted()
  {
    AgentArgumentText.Format(null).ShouldBeNull();
    AgentArgumentText.Format(new Dictionary<string, object?>()).ShouldBeNull();

    Dictionary<string, object?>? deserialized =
      JsonSerializer.Deserialize<Dictionary<string, object?>>("""{"path":"/Feedback"}""");
    string formatted = AgentArgumentText.Format(deserialized).ShouldNotBeNull();
    formatted.ShouldNotContain("System.Collections.Generic");
    using JsonDocument document = JsonDocument.Parse(formatted);
    document.RootElement.GetProperty("path").GetString().ShouldBe("/Feedback");

    string razor = File.ReadAllText(RepoFile(
      "source",
      "container-apps",
      "web",
      "projects",
      "web-spa",
      "features",
      "application",
      "modals",
      "agent-ask",
      "AgentAsk.razor"));
    razor.ShouldNotContain("@call.Arguments");
    razor.ShouldNotContain("@approval.Arguments");
    razor.ShouldContain("AgentArgumentText.Format(call.Arguments)");
    razor.ShouldContain("AgentArgumentText.Format(approval.Arguments)");
    return Task.CompletedTask;
  }

  public static async Task Relay_Sends_Tool_Arguments_And_Page_Context_As_Json()
  {
    CapturingChatApi api = new();
    RelayChatClient client = new(api);
    Dictionary<string, object?> arguments = JsonSerializer.Deserialize<Dictionary<string, object?>>
    (
      """{"title":"Export failed"}"""
    ).ShouldNotBeNull();
    ChatMessage user = new(ChatRole.User, "Explain this page");
    ChatMessage call = new
    (
      ChatRole.Assistant,
      [new FunctionCallContent("call-page", "page_context", arguments)]
    );
    ChatMessage pageResult = new
    (
      ChatRole.Tool,
      [new FunctionResultContent("call-page", """{"path":"/Feedback","page":"Feedback"}""")]
    );
    ChatMessage dictionaryResult = new
    (
      ChatRole.Tool,
      [
        new FunctionResultContent
        (
          "call-dict",
          new Dictionary<string, object?> { ["path"] = "/Feedback" }
        ),
      ]
    );

    await client.GetResponseAsync([user, call, pageResult, dictionaryResult]);

    CompleteAgentChat.Command command = api.Command.ShouldNotBeNull();
    string payload = JsonSerializer.Serialize(command);
    payload.ShouldNotContain("System.Collections.Generic");

    CompleteAgentChat.Turn callTurn = command.Messages.Single(turn => turn.ToolCalls.Count > 0);
    string argumentsJson = callTurn.ToolCalls.Single().ArgumentsJson;
    argumentsJson.ShouldNotContain("System.Collections.Generic");
    using (JsonDocument parsedArguments = JsonDocument.Parse(argumentsJson))
    {
      parsedArguments.RootElement.GetProperty("title").GetString().ShouldBe("Export failed");
    }

    CompleteAgentChat.Turn pageTurn = command.Messages.Single(turn => turn.ToolCallId == "call-page");
    pageTurn.ToolResult.ShouldBe("""{"path":"/Feedback","page":"Feedback"}""");
    using JsonDocument parsedPage = JsonDocument.Parse(pageTurn.ToolResult.ShouldNotBeNull());
    parsedPage.RootElement.GetProperty("path").GetString().ShouldBe("/Feedback");

    CompleteAgentChat.Turn dictionaryTurn = command.Messages.Single(turn => turn.ToolCallId == "call-dict");
    string dictionaryJson = dictionaryTurn.ToolResult.ShouldNotBeNull();
    dictionaryJson.ShouldNotContain("System.Collections.Generic");
    using JsonDocument parsedDictionary = JsonDocument.Parse(dictionaryJson);
    parsedDictionary.RootElement.GetProperty("path").GetString().ShouldBe("/Feedback");
  }

  public static async Task Route_Follows_The_Observed_Manager_And_Keeps_The_Path_For_A_Stuck_One()
  {
    TestNavigationManager shell = new();
    TestNavigationManager stuck = new();
    PageAgentRoute route = new();
    route.PathOr(shell).ShouldBe("/");

    shell.NavigateTo("/Feedback");
    route.Observe(shell, jsRuntime: null);
    route.PathOr(shell).ShouldBe("/Feedback");
    route.PathOr(stuck).ShouldBe("/Feedback");

    // No observer runs on a focused-page hop. The observed manager's live path still wins.
    shell.NavigateTo("/Settings");
    route.PathOr(shell).ShouldBe("/Settings");
    route.PathOr(stuck).ShouldBe("/Settings");
    await Task.CompletedTask;
  }

  public static async Task Page_Context_On_Feedback_Uses_The_Ask_Scope_Not_A_Stuck_Manager()
  {
    const string secretBody = "secret body that must not reach the model";
    using FeedbackSpa spa = new();
    using SpaTestScope scope = SpaTestScope.Create(spa);
    NavigationManager shellNavigation = scope.ServiceProvider.GetRequiredService<NavigationManager>();
    shellNavigation.NavigateTo("/Feedback");
    StuckNavigationProvider stuck = new(scope.ServiceProvider);

    string stuckAtRoot = await InvokePageContextAsync(stuck);
    stuckAtRoot.ShouldNotContain("System.Collections.Generic");
    using (JsonDocument root = JsonDocument.Parse(stuckAtRoot))
    {
      root.RootElement.GetProperty("path").GetString().ShouldBe("/");
    }

    scope.ServiceProvider.GetRequiredService<PageAgentRoute>().Observe
    (
      shellNavigation,
      scope.ServiceProvider.GetRequiredService<Microsoft.JSInterop.IJSRuntime>()
    );
    await scope.Send(new FeedbackState.ListMyFeedbackActionSet.Action());
    await scope.Send(new FeedbackState.NoteComposerActionSet.Action(true, "Complaint", false, true));

    string feedback = await InvokePageContextAsync(stuck);
    feedback.ShouldNotContain("System.Collections.Generic");
    feedback.ShouldNotContain(secretBody);
    using (JsonDocument document = JsonDocument.Parse(feedback))
    {
      JsonElement element = document.RootElement;
      element.GetProperty("path").GetString().ShouldBe("/Feedback");
      element.GetProperty("page").GetString().ShouldBe("Feedback");
      element.GetProperty("filingsLoaded").GetBoolean().ShouldBeTrue();
      element.GetProperty("filingCount").GetInt32().ShouldBe(21);
      element.GetProperty("filings").GetArrayLength().ShouldBe(20);
      element.GetProperty("filings")[0].GetProperty("title").GetString().ShouldBe("Filing 0");
      element.GetProperty("filings")[0].TryGetProperty("body", out _).ShouldBeFalse();
      element.GetProperty("emailCopyAvailable").GetBoolean().ShouldBeTrue();
      element.GetProperty("draftAttachmentCount").GetInt32().ShouldBe(0);
      JsonElement draft = element.GetProperty("draft");
      draft.GetProperty("kind").GetString().ShouldBe("Complaint");
      draft.GetProperty("hasTitle").GetBoolean().ShouldBeFalse();
      draft.GetProperty("hasBody").GetBoolean().ShouldBeTrue();
      draft.TryGetProperty("body", out _).ShouldBeFalse();
    }

    Guid openId = Guid.Parse("00000000-0000-0000-0000-000000000001");
    string permalink = $"/Feedback/{openId:D}";
    shellNavigation.NavigateTo(permalink);
    scope.ServiceProvider.GetRequiredService<PageAgentRoute>().Observe
    (
      shellNavigation,
      scope.ServiceProvider.GetRequiredService<Microsoft.JSInterop.IJSRuntime>()
    );
    await scope.Send(new FeedbackState.OpenFeedbackActionSet.Action(openId));

    string item = await InvokePageContextAsync(stuck);
    item.ShouldNotContain("System.Collections.Generic");
    item.ShouldNotContain(secretBody);
    using JsonDocument itemDocument = JsonDocument.Parse(item);
    itemDocument.RootElement.GetProperty("path").GetString().ShouldBe(permalink);
    itemDocument.RootElement.GetProperty("openFeedbackId").GetString().ShouldBe(openId.ToString("D"));
    itemDocument.RootElement.GetProperty("openItem").GetProperty("title").GetString().ShouldBe("Filing 0");
    itemDocument.RootElement.GetProperty("openItem").TryGetProperty("body", out _).ShouldBeFalse();
  }

  private static async Task<string> InvokePageContextAsync(IServiceProvider services)
  {
    PageContextEchoClient echo = new();
    using CatalogAgentFunctions functions = CatalogAgentFunctions.Create([]);
    using FunctionInvokingChatClient client = CatalogAgentSession.CreateInvokingClient(echo, services);
    ChatOptions options = new()
    {
      Tools = [.. functions.Tools],
    };
    await client.GetResponseAsync
    (
      [new ChatMessage(ChatRole.User, "Explain this page")],
      options,
      CancellationToken.None
    );
    return echo.ToolResult.ShouldNotBeNull();
  }

  private static string RepoFile(params string[] parts)
  {
    string? directory = AppContext.BaseDirectory;
    while (directory is not null && !File.Exists(Path.Combine(directory, "timewarp-architecture.slnx")))
    {
      directory = Path.GetDirectoryName(directory);
    }

    directory.ShouldNotBeNull();
    return Path.Combine([directory, .. parts]);
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

  private static async Task<string[]> ExpectedNamesAsync
  (
    ClaimsPrincipal user,
    IAuthorizationService authorization,
    IActionCatalog catalog,
    string path
  )
  {
    string normalized = PageAgentScope.Normalize(path);
    IReadOnlyList<CommandPaletteRow> rows = await CommandPaletteRoster.BuildAsync
    (
      user,
      authorization,
      PageRegistry.All,
      catalog.Entries,
      normalized
    );
    Dictionary<string, ActionCatalogEntry> byName = [];
    foreach (ActionCatalogEntry entry in catalog.Entries)
    {
      byName[entry.Name] = entry;
    }

    List<string> names = [];
    HashSet<string> seen = [];
    bool anyPage = false;
    foreach (CommandPaletteRow row in rows)
    {
      if (row.Kind == CommandPaletteRowKind.Page)
      {
        anyPage = true;
        continue;
      }

      if (row.Kind != CommandPaletteRowKind.Command
        || !byName.TryGetValue(row.Target, out ActionCatalogEntry? entry)
        || !entry.Visibility.HasFlag(ActionVisibility.Agent)
        || !seen.Add(entry.Name))
      {
        continue;
      }

      names.Add(entry.Name);
    }

    foreach (string name in PageAgentScope.ActionNamesFor(normalized))
    {
      if (!seen.Add(name) || !byName.TryGetValue(name, out ActionCatalogEntry? entry))
      {
        continue;
      }

      if (!entry.Visibility.HasFlag(ActionVisibility.Agent))
      {
        continue;
      }

      if (await CommandPaletteRoster.IsPermittedAsync(user, authorization, entry))
      {
        names.Add(name);
      }
    }

    if (anyPage)
    {
      names.Add(AgentNavigate.ToolName);
    }

    return [.. names];
  }

  private static async Task<string[]> ExpectedWebMcpNamesAsync
  (
    ClaimsPrincipal user,
    IAuthorizationService authorization,
    IActionCatalog catalog,
    string path
  )
  {
    string[] names = await ExpectedNamesAsync(user, authorization, catalog, path);
    return [.. names, PageAgentContext.ToolName];
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

  private static string[] Names(IReadOnlyList<WebMcpToolDescriptor> tools)
  {
    string[] names = new string[tools.Count];
    for (int index = 0; index < tools.Count; index++)
    {
      names[index] = tools[index].Name;
    }

    return names;
  }

  /// <summary>In-proc SPA: real catalog, WebMCP dispatcher, and store; signed-in with every permission; scripted feedback BFF.</summary>
  private sealed class FeedbackSpa : ISpaTestApplication, IDisposable
  {
    public IServiceProvider ServiceProvider { get; }
    public ScriptedFeedbackApiService Api { get; } = new();

    public FeedbackSpa()
    {
      ClaimsPrincipal user = new(new ClaimsIdentity(
        [
          new Claim("sub", Guid.NewGuid().ToString()),
          .. PermissionIds.All.Select(static permission => new Claim(PermissionIds.ClaimType, permission)),
        ],
        authenticationType: "test"));

      ServiceCollection services = new();
      services.AddLogging();
      services.AddWebSpaGeneratedMediator();
      services.AddTimeWarpState
      (
        options =>
        {
          options.Assemblies =
          [
            typeof(TimeWarp.Architecture.Web.Spa.IAssemblyMarker).Assembly,
            typeof(TimeWarp.State.Plus.AssemblyMarker).Assembly
          ];
        }
      );
      services.AddTimeWarpStateBlazor();
      services.AddActionCatalog(typeof(TimeWarp.Architecture.Web.Spa.IAssemblyMarker).Assembly);
      services.AddScoped<
        TimeWarp.Features.Persistence.IPersistenceService,
        TimeWarp.Features.Persistence.PersistenceService>();
      services.AddAuthorizationCore(TimeWarp.Architecture.PolicyRegistration.AddPolicies);
      services.AddScoped<AuthenticationStateProvider>(_ => new FixedAuthenticationStateProvider(user));
      services.AddSingleton<TimeWarp.Architecture.Services.IWebServerApiService>(Api);
      services.AddScoped(_ => FakeItEasy.A.Fake<Microsoft.JSInterop.IJSRuntime>());
      services.AddScoped<NavigationManager, TestNavigationManager>();
      services.AddScoped<PageAgentRoute>();
      services.AddScoped<AgentCallOutcome>();
      services.AddScoped<WebMcpApprovalGate>();
      services.AddScoped<WebMcpDispatcher>();

      ServiceProvider = services.BuildServiceProvider();
    }

    public void Dispose()
    {
      if (ServiceProvider is IDisposable disposable)
      {
        disposable.Dispose();
      }
    }
  }

  private sealed class FixedAuthenticationStateProvider(ClaimsPrincipal user) : AuthenticationStateProvider
  {
    public override Task<AuthenticationState> GetAuthenticationStateAsync() =>
      Task.FromResult(new AuthenticationState(user));
  }

  /// <summary>Scripted BFF: answers SubmitFeedback with a fresh id and the server's permalink shape, echoing the command.</summary>
  private sealed class ScriptedFeedbackApiService : TimeWarp.Architecture.Services.IWebServerApiService
  {
    private static readonly DateTimeOffset FiledAt = new(2026, 10, 9, 0, 0, 0, TimeSpan.Zero);

    public List<SubmitFeedback.Command> Requests { get; } = [];
    public Guid? LastId { get; private set; }

    public Task<OneOf<TResponse, FileResponse, SharedProblemDetails>> GetResponse<TResponse>
    (
      IApiRequest request,
      CancellationToken cancellationToken
    ) where TResponse : class
    {
      _ = cancellationToken;
      if (request is SubmitFeedback.Command command)
      {
        Requests.Add(command);
        Guid id = Guid.NewGuid();
        LastId = id;
        SubmitFeedback.Response response = new
        (
          id,
          $"/Feedback/{id:D}",
          command.Kind,
          command.Title.Trim(),
          command.Body.Trim(),
          emailCopySent: false
        );
        if (response is TResponse typed)
        {
          return Task.FromResult<OneOf<TResponse, FileResponse, SharedProblemDetails>>(typed);
        }
      }

      if (request is ListMyFeedback.Query)
      {
        List<ListMyFeedback.Item> items = [];
        for (int index = 1; index <= 21; index++)
        {
          // Item rejects Guid.Empty. Index 1 is the open-id fixture (...0001) and "Filing 0".
          Guid id = Guid.Parse($"00000000-0000-0000-0000-{index:x12}");
          items.Add
          (
            new ListMyFeedback.Item
            (
              id,
              $"/Feedback/{id:D}",
              FeedbackKind.Complaint,
              $"Filing {index - 1}",
              FiledAt
            )
          );
        }

        ListMyFeedback.Response listed = new(items, emailCopyAvailable: true);
        if (listed is TResponse listedTyped)
        {
          return Task.FromResult<OneOf<TResponse, FileResponse, SharedProblemDetails>>(listedTyped);
        }
      }

      if (request is GetFeedback.Query query)
      {
        GetFeedback.Response opened = new
        (
          query.FeedbackItemId,
          $"/Feedback/{query.FeedbackItemId:D}",
          FeedbackKind.Complaint,
          "Filing 0",
          "secret body that must not reach the model",
          FiledAt
        );
        if (opened is TResponse openedTyped)
        {
          return Task.FromResult<OneOf<TResponse, FileResponse, SharedProblemDetails>>(openedTyped);
        }
      }

      throw new InvalidOperationException($"No scripted response for {request.GetType()} → {typeof(TResponse)}.");
    }
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
    private readonly string ToolName;
    private readonly Dictionary<string, object?> Arguments;
    private bool OfferedCall;

    public ScriptedChatClient()
      : this("Counter.IncrementCounter", new Dictionary<string, object?> { ["amount"] = 5 })
    {
    }

    public ScriptedChatClient(string toolName, Dictionary<string, object?> arguments)
    {
      ToolName = toolName;
      Arguments = arguments;
    }

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
        FunctionCallContent call = new("call-scripted", ToolName, Arguments);
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

  private sealed class CapturingChatApi : TimeWarp.Architecture.Services.IWebServerApiService
  {
    public CompleteAgentChat.Command? Command { get; private set; }

    public Task<OneOf<TResponse, FileResponse, SharedProblemDetails>> GetResponse<TResponse>
    (
      IApiRequest request,
      CancellationToken cancellationToken
    ) where TResponse : class
    {
      _ = cancellationToken;
      Command = request.ShouldBeOfType<CompleteAgentChat.Command>();
      CompleteAgentChat.Response response = new() { Text = "ok" };
      return Task.FromResult<OneOf<TResponse, FileResponse, SharedProblemDetails>>
      (
        response.ShouldBeOfType<TResponse>()
      );
    }
  }

  private sealed class PageContextEchoClient : IChatClient
  {
    private bool OfferedCall;

    public string? ToolResult { get; private set; }

    public Task<ChatResponse> GetResponseAsync
    (
      IEnumerable<ChatMessage> messages,
      ChatOptions? options = null,
      CancellationToken cancellationToken = default
    )
    {
      _ = options;
      _ = cancellationToken;
      foreach (ChatMessage message in messages)
      {
        foreach (AIContent content in message.Contents)
        {
          if (content is FunctionResultContent result)
          {
            ToolResult = result.Result switch
            {
              string text => text,
              JsonElement { ValueKind: JsonValueKind.String } element => element.GetString(),
              _ => result.Result?.ToString(),
            };
          }
        }
      }

      if (!OfferedCall)
      {
        OfferedCall = true;
        FunctionCallContent call = new
        (
          "call-page",
          PageAgentContext.ToolName,
          new Dictionary<string, object?>()
        );
        return Task.FromResult(new ChatResponse(new ChatMessage(ChatRole.Assistant, [call])));
      }

      return Task.FromResult(new ChatResponse(new ChatMessage(ChatRole.Assistant, "done")));
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

  /// <summary>
  /// Answers NavigationManager with a manager that never left the base URI.
  /// Every other service, including PageAgentRoute and IStore, comes from the shell scope.
  /// </summary>
  private sealed class StuckNavigationProvider : IServiceProvider
  {
    private readonly IServiceProvider Inner;
    private readonly NavigationManager Stuck = new TestNavigationManager();

    public StuckNavigationProvider(IServiceProvider inner)
    {
      Inner = inner;
    }

    public object? GetService(Type serviceType) =>
      serviceType == typeof(NavigationManager) ? Stuck : Inner.GetService(serviceType);
  }
}

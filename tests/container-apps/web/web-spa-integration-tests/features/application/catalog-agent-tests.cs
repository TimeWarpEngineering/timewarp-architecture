#region Purpose
// Catalog tools for the in-app model and WebMCP: schema, argument binding, selection, and the
// fake client that dispatches a real store action. Registers the CatalogAgent_Should partial class.
#endregion

#region Design
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
// AddPasskey on Settings uses that same closed box. Mediator 15 activates the handler inside the
// pipeline, so the missing client becomes an ExceptionNotification error bar and the dispatcher
// returns completed. That bar is the proof the call executed rather than offering navigation.
// page_context on /Feedback is driven through CatalogAgentSession.CreateInvokingClient with the
// same scope the panel passes in. A wrapper that answers NavigationManager with a manager stuck
// at the base URI still returns the observed /Feedback route. RelayChatClient is asserted to send
// tool arguments and tool results as JSON, never a dictionary's CLR type name.
// site_map and navigate list the design-system parts Ctrl-K lists for a principal who may open the
// Design System page, and refuse them for one who may not (task 446).
#endregion
#endregion

namespace CatalogAgent_;

using System.Reflection;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.AI;
using TimeWarp.Architecture.Components;
using TimeWarp.Architecture.Features;
using TimeWarp.Architecture.Features.Settings;
using TimeWarp.Architecture.Web.Spa;
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
    bind.EntraTenantAcceptanceMode.ShouldBe(EntraTenantAcceptanceMode.Restricted);
    bind.EntraAcceptedTenants.ShouldBeEmpty();
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
    JsonElement acceptance = schema.RootElement
      .GetProperty("properties")
      .GetProperty(parameter)
      .GetProperty("properties")
      .GetProperty("entraTenantAcceptanceMode");
    acceptance.GetProperty("type").GetString().ShouldBe("string");
    string[] acceptanceMembers = [.. acceptance.GetProperty("enum").EnumerateArray().Select(member => member.GetString() ?? "")];
    acceptanceMembers.ShouldBe(Enum.GetNames<EntraTenantAcceptanceMode>());
    schema.RootElement
      .GetProperty("properties")
      .GetProperty(parameter)
      .GetProperty("properties")
      .TryGetProperty("assistantName", out _)
      .ShouldBeFalse();
    schema.RootElement
      .GetProperty("properties")
      .GetProperty(parameter)
      .GetProperty("properties")
      .TryGetProperty("xaiByokMode", out _)
      .ShouldBeFalse();
    return Task.CompletedTask;
  }

  public static Task Binds_UpdateXaiByokMode_With_Mode_And_Version()
  {
    using SpaTestScope scope = SpaTestScope.Create(Spa!);
    IActionCatalog catalog = scope.ServiceProvider.GetRequiredService<IActionCatalog>();
    ActionCatalogEntry update = catalog.Find("SiteSettings.UpdateXaiByokMode").ShouldNotBeNull();
    string parameter = update.Parameters[0].Name;
    JsonElement command = JsonSerializer.Deserialize<JsonElement>
    (
      """{"xaiByokMode":"Required","version":2}"""
    );

    object?[] bound = CatalogAgentArguments.Bind(update, new Dictionary<string, object?> { [parameter] = command });

    bound.Length.ShouldBe(1);
    UpdateXaiByokMode.Command bind = bound[0].ShouldBeOfType<UpdateXaiByokMode.Command>();
    bind.XaiByokMode.ShouldBe(XaiByokMode.Required);
    bind.Version.ShouldBe(2);

    using JsonDocument modeSchema = JsonDocument.Parse(CatalogAgentSchema.For(update));
    JsonElement properties = modeSchema.RootElement
      .GetProperty("properties")
      .GetProperty(parameter)
      .GetProperty("properties");
    JsonElement mode = properties.GetProperty("xaiByokMode");
    mode.GetProperty("type").GetString().ShouldBe("string");
    string[] members = [.. mode.GetProperty("enum").EnumerateArray().Select(member => member.GetString() ?? "")];
    members.ShouldBe(Enum.GetNames<XaiByokMode>());
    properties.GetProperty("version").GetProperty("type").GetString().ShouldBe("integer");
    return Task.CompletedTask;
  }

  public static Task Binds_UpdateAssistantName_With_Name_And_Version()
  {
    using SpaTestScope scope = SpaTestScope.Create(Spa!);
    IActionCatalog catalog = scope.ServiceProvider.GetRequiredService<IActionCatalog>();
    ActionCatalogEntry rename = catalog.Find("SiteSettings.UpdateAssistantName").ShouldNotBeNull();
    string parameter = rename.Parameters[0].Name;
    JsonElement command = JsonSerializer.Deserialize<JsonElement>
    (
      """{"assistantName":"Zawadi","version":2}"""
    );

    object?[] bound = CatalogAgentArguments.Bind(rename, new Dictionary<string, object?> { [parameter] = command });

    bound.Length.ShouldBe(1);
    UpdateAssistantName.Command bind = bound[0].ShouldBeOfType<UpdateAssistantName.Command>();
    bind.AssistantName.ShouldBe("Zawadi");
    bind.Version.ShouldBe(2);

    using JsonDocument schema = JsonDocument.Parse(CatalogAgentSchema.For(rename));
    JsonElement properties = schema.RootElement
      .GetProperty("properties")
      .GetProperty(parameter)
      .GetProperty("properties");
    properties.GetProperty("assistantName").GetProperty("type").GetString().ShouldBe("string");
    properties.GetProperty("version").GetProperty("type").GetString().ShouldBe("integer");
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
    counter.ShouldContain(tool => tool.Name == AgentNavigate.ToolName && tool.RequiresApproval);
    counter.ShouldContain(tool => tool.Name == AgentSiteMap.ToolName && !tool.RequiresApproval);
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
    // Capture screenshot is a global palette command with no permission (task 303-001).
    Names(developerNewRole).ShouldBe(["Screenshot.CaptureScreenshot", AgentNavigate.ToolName, AgentSiteMap.ToolName]);
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
    ).Select(tool => tool.Name).ShouldBe([AgentNavigate.ToolName, AgentSiteMap.ToolName]);

    (
      await CatalogAgentToolSet.SelectAsync
      (
        anonymous, authorization, catalog.Entries, "/Counter", CancellationToken.None
      )
    ).Select(tool => tool.Name).ShouldBe([AgentNavigate.ToolName, AgentSiteMap.ToolName]);
  }

  public static async Task Fake_Client_Dispatches_Increment_Only_After_Approval()
  {
    using SpaTestScope scope = SpaTestScope.Create(Spa!);
    scope.ServiceProvider.GetRequiredService<NavigationManager>().NavigateTo("/Counter");
    scope.Store.GetState<CounterState>().Initialize(count: 10);
    using CatalogAgentFunctions functions = CatalogAgentFunctions.Create(await SelectForSessionAsync(scope));
    ChatOptions options = new() { Tools = [.. functions.Tools] };
    functions.Tools.Single(tool => tool.Name == "Counter.IncrementCounter").ShouldBeOfType<ApprovalRequiredAIFunction>();
    functions.Tools.Single(tool => tool.Name == AgentNavigate.ToolName).ShouldBeOfType<ApprovalRequiredAIFunction>();
    functions.Tools.Single(tool => tool.Name == AgentSiteMap.ToolName).ShouldNotBeOfType<ApprovalRequiredAIFunction>();
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
}

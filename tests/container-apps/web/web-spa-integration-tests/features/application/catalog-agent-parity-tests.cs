#region Purpose
// Task 292: in-app and WebMCP tool lists match, and edit mode plus the conversation credential
// behave the same on both drivers.
#endregion

#region Design
// The parity loop is every known page route, two routes with no catalog tools, a feedback detail
// route, and /FeedbackExtra, crossed with four principals and both edit modes. Each pair compares
// name, description, input schema, and the approval bit, in order. A second parity case goes
// through the real publish path: the mode is set on AgentSurfaceState and WebMcpPublisher.PublishAsync
// runs against a recording model context. Listing does not take a credential. Invoke tests put one
// on AgentSurfaceState and drive WebMCP and the in-app function. Credentials meant to be valid are
// issued at a fixed mid-day time tomorrow (FutureNoon) and pure Denial checks take a fixed `now`,
// so the UTC-midnight cap cannot make a run near midnight flake.
#endregion

namespace CatalogAgent_;

using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.AI;
using Microsoft.JSInterop;
using TimeWarp.Architecture.Components;
using TimeWarp.Architecture.Features;
using TimeWarp.Architecture.Features.Admin.Roles;
using TimeWarp.Architecture.Features.AgentChats;
using TimeWarp.Architecture.Features.Applications;
using TimeWarp.Architecture.Services;

public partial class CatalogAgent_Should
{
  public static async Task In_App_And_WebMcp_Tool_Lists_Match_For_Every_Page_Principal_And_Edit_Mode()
  {
    using SpaTestScope scope = SpaTestScope.Create(Spa!);
    IActionCatalog catalog = scope.ServiceProvider.GetRequiredService<IActionCatalog>();
    IAuthorizationService authorization = scope.ServiceProvider.GetRequiredService<IAuthorizationService>();
    List<string> routes =
    [
      .. PageAgentScope.KnownRoutes,
      "/",
      "/StyleGuide",
      "/Nope",
      "/Feedback/item",
      "/FeedbackExtra",
    ];
    ClaimsPrincipal[] principals =
    [
      Principal([.. PermissionIds.All]),
      Principal(PermissionIds.DeveloperAccess),
      Principal(),
      new(new ClaimsIdentity()),
    ];
    AgentEditMode[] modes = [AgentEditMode.AskBeforeEditing, AgentEditMode.AutomaticallyEdit];

    foreach (string route in routes)
    {
      foreach (ClaimsPrincipal principal in principals)
      {
        foreach (AgentEditMode mode in modes)
        {
          await AssertSameSurfaceAsync(principal, authorization, catalog, route, mode);
        }
      }
    }
  }

  public static Task Edit_Mode_Credential_And_Panel_State_Follow_The_Shared_Rules()
  {
    DateTimeOffset evening = new(2026, 10, 9, 20, 0, 0, TimeSpan.Zero);
    AgentConversationCredential capped = AgentConversationCredentialIssuer.Issue
    (
      Guid.CreateVersion7(),
      ["a"],
      evening,
      TimeSpan.FromHours(12),
      new string('x', 200)
    );
    capped.ExpiresAt.ShouldBe(new DateTimeOffset(2026, 10, 10, 0, 0, 0, TimeSpan.Zero));
    capped.DisplayName.Length.ShouldBe(AgentConversationCredentialIssuer.MaxDisplayNameLength);
    capped.Id.Version.ShouldBe(7);

    DateTimeOffset morning = new(2026, 10, 9, 1, 0, 0, TimeSpan.Zero);
    AgentConversationCredential sooner = AgentConversationCredentialIssuer.Issue
    (
      Guid.Empty,
      [],
      morning,
      TimeSpan.FromHours(2)
    );
    sooner.ExpiresAt.ShouldBe(new DateTimeOffset(2026, 10, 9, 3, 0, 0, TimeSpan.Zero));
    sooner.DisplayName.ShouldBe(AgentConversationCredentialIssuer.DefaultDisplayName);

    DateTimeOffset noon = new(2026, 10, 9, 12, 0, 0, TimeSpan.Zero);
    Guid principalId = Guid.NewGuid();
    ClaimsPrincipal user = new(new ClaimsIdentity(
      [new Claim(ClaimTypes.NameIdentifier, principalId.ToString())],
      authenticationType: "Test"));
    AgentConversationCredential credential = AgentConversationCredentialIssuer.Issue
    (
      principalId,
      ["perm.a"],
      noon,
      TimeSpan.FromHours(1)
    );
    AgentConversationAuthority.Denial(null, user, ["perm.a"], noon).ShouldBeNull();
    AgentConversationAuthority.Denial(null, null, ["perm.a"], noon).ShouldBeNull();
    AgentConversationAuthority.Denial(credential, user, [], noon).ShouldBeNull();
    AgentConversationAuthority.Denial(credential, user, ["perm.a"], noon).ShouldBeNull();
    AgentConversationAuthority.Denial(credential, user, ["perm.missing"], noon)
      .ShouldBe(AgentConversationAuthority.ScopeError);
    ClaimsPrincipal other = new(new ClaimsIdentity(
      [new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString())],
      authenticationType: "Test"));
    AgentConversationAuthority.Denial(credential, other, [], noon).ShouldBe(AgentConversationAuthority.PrincipalError);
    // With a credential present the principal check fails closed: no id, an unparseable id, or no user.
    ClaimsPrincipal nameless = new(new ClaimsIdentity([new Claim(ClaimTypes.Name, "n")], authenticationType: "Test"));
    AgentConversationAuthority.Denial(credential, nameless, [], noon).ShouldBe(AgentConversationAuthority.PrincipalError);
    ClaimsPrincipal garbled = new(new ClaimsIdentity(
      [new Claim(ClaimTypes.NameIdentifier, "not-a-guid")],
      authenticationType: "Test"));
    AgentConversationAuthority.Denial(credential, garbled, [], noon).ShouldBe(AgentConversationAuthority.PrincipalError);
    AgentConversationAuthority.Denial(credential, null, [], noon).ShouldBe(AgentConversationAuthority.PrincipalError);
    AgentConversationAuthority.IsExpired(credential, noon).ShouldBeFalse();
    AgentConversationAuthority.IsExpired(credential, credential.ExpiresAt).ShouldBeTrue();
    AgentConversationAuthority.IsExpired(null, noon).ShouldBeFalse();
    AgentConversationAuthority.Denial(credential, user, [], credential.ExpiresAt)
      .ShouldBe(AgentConversationAuthority.ExpiredError);
    AgentConversationCredential expired = AgentConversationCredentialIssuer.Issue
    (
      principalId,
      ["perm.a"],
      noon.AddDays(-2),
      TimeSpan.FromHours(1)
    );
    AgentConversationAuthority.Denial(expired, other, ["perm.missing"], noon)
      .ShouldBe(AgentConversationAuthority.ExpiredError);

    AskPrivacyNotice.ShouldShow(recordChats: true, dismissed: false, "Chats are recorded.").ShouldBeTrue();
    AskPrivacyNotice.ShouldShow(recordChats: false, dismissed: false, "Chats are recorded.").ShouldBeFalse();
    AskPrivacyNotice.ShouldShow(recordChats: true, dismissed: true, "Chats are recorded.").ShouldBeFalse();
    AskPrivacyNotice.ShouldShow(recordChats: true, dismissed: false, "  ").ShouldBeFalse();

    IReadOnlyList<AskResourceReference> references = AskResourceReferences.FromPageContext
    (
      """{"credentials":[{"id":"abc","nickname":"Laptop"}],"profile":{"alias":"ada"},"siteSettings":{"version":3}}"""
    );
    references.ShouldContain(item => item.Token == "@credential:abc" && item.Label == "Laptop");
    references.ShouldContain(item => item.Token == "@profile:ada");
    references.ShouldContain(item => item.Token == "@siteSettingsVersion:3");
    AskResourceReferences.FromPageContext("not json").ShouldBeEmpty();
    AskResourceReferences.FromPageContext("""{"path":"/"}""").ShouldBeEmpty();

    // Token values drop whitespace and control characters; labels keep the text.
    IReadOnlyList<AskResourceReference> messy = AskResourceReferences.FromPageContext
    (
      """{"credentials":[{"id":" a b\tc "},{"id":" \n "}],"profile":{"alias":" a d\na\u0007 "},"siteSettings":{"version":" 4 "}}"""
    );
    messy.Select(item => item.Token).ShouldBe(["@credential:abc", "@profile:ada", "@siteSettingsVersion:4"]);
    messy.Single(item => item.Token == "@profile:ada").Label.ShouldBe("a d\na\u0007");
    AskResourceReferences.TokenValue(" x\r\ny\u0000 ").ShouldBe("xy");
    AskResourceReferences.TokenValue(null).ShouldBe("");

    // The Support href accepts an app-relative path or http/https, else the default.
    AskSupportLink.Normalize("/Help").ShouldBe("/Help");
    AskSupportLink.Normalize("https://example.com/help").ShouldBe("https://example.com/help");
    AskSupportLink.Normalize("http://example.com").ShouldBe("http://example.com");
    AskSupportLink.Normalize("javascript:alert(1)").ShouldBe(XaiChatDefaults.SupportUrl);
    AskSupportLink.Normalize("data:text/html,x").ShouldBe(XaiChatDefaults.SupportUrl);
    AskSupportLink.Normalize("//evil.example/x").ShouldBe(XaiChatDefaults.SupportUrl);
    AskSupportLink.Normalize("/\\evil.example/x").ShouldBe(XaiChatDefaults.SupportUrl);
    AskSupportLink.Normalize("help").ShouldBe(XaiChatDefaults.SupportUrl);
    AskSupportLink.Normalize("  ").ShouldBe(XaiChatDefaults.SupportUrl);
    AskSupportLink.Normalize(null).ShouldBe(XaiChatDefaults.SupportUrl);
    return Task.CompletedTask;
  }

  public static async Task Panel_Actions_Open_Expand_And_Reset_The_Conversation()
  {
    using SpaTestScope scope = SpaTestScope.Create(Spa!);
    AgentSurfaceState surface = scope.Store.GetState<AgentSurfaceState>();
    await surface.OpenAskPanel();
    surface = scope.Store.GetState<AgentSurfaceState>();
    surface.IsPanelOpen.ShouldBeTrue();
    await surface.ToggleAskExpand();
    surface = scope.Store.GetState<AgentSurfaceState>();
    surface.IsPanelExpanded.ShouldBeTrue();
    await surface.SetEditMode(AgentEditMode.AutomaticallyEdit);
    surface = scope.Store.GetState<AgentSurfaceState>();
    surface.EditMode.ShouldBe(AgentEditMode.AutomaticallyEdit);
    await surface.NewConversation();
    surface = scope.Store.GetState<AgentSurfaceState>();
    surface.EditMode.ShouldBe(AgentEditMode.AskBeforeEditing);
    surface.Conversation.ShouldBeNull();
    surface.ConversationGeneration.ShouldBe(1);
    await surface.CloseAskPanel();
    surface = scope.Store.GetState<AgentSurfaceState>();
    surface.IsPanelOpen.ShouldBeFalse();
    surface.IsPanelExpanded.ShouldBeFalse();
  }

  public static async Task Close_Resets_Edit_Mode_But_Keeps_The_Conversation()
  {
    using SpaTestScope scope = SpaTestScope.Create(Spa!);
    AgentSurfaceState surface = scope.Store.GetState<AgentSurfaceState>();
    await surface.OpenAskPanel();
    await RememberConversationAsync(scope, surface, [.. PermissionIds.All], expired: false);
    await surface.SetEditMode(AgentEditMode.AutomaticallyEdit);
    surface = scope.Store.GetState<AgentSurfaceState>();
    Guid? conversationId = surface.ConversationId;
    int generation = surface.ConversationGeneration;

    await surface.CloseAskPanel();

    surface = scope.Store.GetState<AgentSurfaceState>();
    surface.EditMode.ShouldBe(AgentEditMode.AskBeforeEditing);
    surface.ConversationId.ShouldBe(conversationId);
    surface.ConversationGeneration.ShouldBe(generation);

    // WebMCP reads the same mode, so after Close a mutating call prompts again.
    scope.ServiceProvider.GetRequiredService<NavigationManager>().NavigateTo("/Counter");
    scope.Store.GetState<CounterState>().Initialize(count: 10);
    WebMcpDispatcher dispatcher = scope.ServiceProvider.GetRequiredService<WebMcpDispatcher>();
    Task<string> asking = dispatcher.InvokeTool("Counter.IncrementCounter", """{"amount":5}""");
    Guid callId = await WaitForPendingAsync(scope, asking);
    await surface.ResolveApproval(callId, approved: false);
    await asking.WaitAsync(Timeout);
    scope.Store.GetState<CounterState>().Count.ShouldBe(10);
  }

  public static async Task Dismiss_Privacy_Notice_Hides_It_For_The_Session()
  {
    using SpaTestScope scope = SpaTestScope.Create(Spa!);
    AgentSurfaceState surface = scope.Store.GetState<AgentSurfaceState>();
    surface.PrivacyNoticeDismissed.ShouldBeFalse();

    await surface.DismissPrivacyNotice();

    surface = scope.Store.GetState<AgentSurfaceState>();
    surface.PrivacyNoticeDismissed.ShouldBeTrue();
    AskPrivacyNotice.ShouldShow(recordChats: true, surface.PrivacyNoticeDismissed, "Chats are recorded.")
      .ShouldBeFalse();
    await surface.NewConversation();
    scope.Store.GetState<AgentSurfaceState>().PrivacyNoticeDismissed.ShouldBeTrue();
  }

  public static async Task Published_WebMcp_List_Reads_The_Store_Edit_Mode_And_Matches_In_App()
  {
    using SpaTestScope scope = SpaTestScope.Create(Spa!);
    scope.ServiceProvider.GetRequiredService<NavigationManager>().NavigateTo("/Counter");
    AgentSurfaceState surface = scope.Store.GetState<AgentSurfaceState>();
    IServiceProvider services = scope.ServiceProvider;
    // Built by hand: the JS context is IAsyncDisposable only, and the test scope disposes synchronously.
    await using JsWebMcpModelContext unused = new
    (
      services.GetRequiredService<IJSRuntime>(),
      services.GetRequiredService<WebMcpDispatcher>()
    );
    WebMcpPublisher publisher = new
    (
      unused,
      services.GetRequiredService<IActionCatalog>(),
      services.GetRequiredService<IAuthorizationService>(),
      services.GetRequiredService<AuthenticationStateProvider>(),
      services.GetRequiredService<NavigationManager>(),
      scope.Store
    );

    foreach (AgentEditMode mode in new[] { AgentEditMode.AskBeforeEditing, AgentEditMode.AutomaticallyEdit })
    {
      await surface.SetEditMode(mode);
      RecordingModelContext recording = new();
      await publisher.PublishAsync(recording, CancellationToken.None);

      using CatalogAgentFunctions functions = CatalogAgentFunctions.Create(await SelectForSessionAsync(scope, mode));
      recording.Tools.Count.ShouldBe(functions.Tools.Count, $"{mode}");
      recording.Tools.Count.ShouldBeGreaterThan(1, $"{mode}");
      for (int index = 0; index < recording.Tools.Count; index++)
      {
        WebMcpToolDescriptor published = recording.Tools[index];
        AITool inApp = functions.Tools[index];
        published.Name.ShouldBe(inApp.Name, $"{mode}");
        published.RequiresApproval.ShouldBe(inApp is ApprovalRequiredAIFunction, $"{mode} {published.Name}");
      }

      recording.Tools.Single(tool => tool.Name == "Counter.IncrementCounter").RequiresApproval
        .ShouldBe(mode == AgentEditMode.AskBeforeEditing);
    }
  }

  public static async Task In_App_Ask_Mode_Expired_Refuses_After_Approval_And_Automatic_Run_Completes()
  {
    using SpaTestScope scope = SpaTestScope.Create(Spa!);
    scope.ServiceProvider.GetRequiredService<NavigationManager>().NavigateTo("/Counter");
    scope.Store.GetState<CounterState>().Initialize(count: 10);
    AgentSurfaceState surface = scope.Store.GetState<AgentSurfaceState>();
    await RememberConversationAsync(scope, surface, [.. PermissionIds.All], expired: true);

    using (CatalogAgentFunctions asking = CatalogAgentFunctions.Create
    (
      await SelectForSessionAsync(scope, AgentEditMode.AskBeforeEditing)
    ))
    {
      AIFunction increment = asking.Tools.Single(tool => tool.Name == "Counter.IncrementCounter")
        .ShouldBeOfType<ApprovalRequiredAIFunction>();
      // Invoking the wrapper is what FunctionInvokingChatClient does once the person approves.
      object? refused = await increment.InvokeAsync(Arguments(scope, """{"amount":5}"""));
      refused.ShouldNotBeNull().ToString()!.ShouldContain(AgentConversationAuthority.ExpiredError);
      scope.Store.GetState<CounterState>().Count.ShouldBe(10);
    }

    await RememberConversationAsync(scope, surface, [.. PermissionIds.All], expired: false);
    await surface.SetEditMode(AgentEditMode.AutomaticallyEdit);
    using CatalogAgentFunctions automatic = CatalogAgentFunctions.Create
    (
      await SelectForSessionAsync(scope, AgentEditMode.AutomaticallyEdit)
    );
    AIFunction run = automatic.Tools.Single(tool => tool.Name == "Counter.IncrementCounter")
      .ShouldBeAssignableTo<AIFunction>();
    run.ShouldNotBeOfType<ApprovalRequiredAIFunction>();
    object? completed = await run.InvokeAsync(Arguments(scope, """{"amount":5}"""));
    completed.ShouldNotBeNull().ToString()!.ShouldContain("Completed = True");
    scope.Store.GetState<CounterState>().Count.ShouldBe(15);
  }

  public static async Task Unwrapped_Function_Refuses_When_The_Mode_Returns_To_Ask()
  {
    using SpaTestScope scope = SpaTestScope.Create(Spa!);
    scope.ServiceProvider.GetRequiredService<NavigationManager>().NavigateTo("/Counter");
    scope.Store.GetState<CounterState>().Initialize(count: 10);
    AgentSurfaceState surface = scope.Store.GetState<AgentSurfaceState>();
    await surface.SetEditMode(AgentEditMode.AutomaticallyEdit);
    using CatalogAgentFunctions functions = CatalogAgentFunctions.Create
    (
      await SelectForSessionAsync(scope, AgentEditMode.AutomaticallyEdit)
    );
    AIFunction increment = functions.Tools.Single(tool => tool.Name == "Counter.IncrementCounter")
      .ShouldBeAssignableTo<AIFunction>();
    increment.ShouldNotBeOfType<ApprovalRequiredAIFunction>();

    await surface.SetEditMode(AgentEditMode.AskBeforeEditing);
    object? refused = await increment.InvokeAsync(Arguments(scope, """{"amount":5}"""));

    refused.ShouldNotBeNull().ToString()!.ShouldContain(CatalogAgentFunctions.ApprovalRequiredError);
    scope.Store.GetState<CounterState>().Count.ShouldBe(10);

    // A read-only tool never needs approval, so its unwrapped function still runs in Ask mode.
    AIFunction pageContext = functions.Tools.Single(tool => tool.Name == PageAgentContext.ToolName)
      .ShouldBeAssignableTo<AIFunction>();
    (await pageContext.InvokeAsync(Arguments(scope, "{}"))).ShouldNotBeNull().ToString()!
      .ShouldNotContain(CatalogAgentFunctions.ApprovalRequiredError);
  }

  public static async Task Automatically_Edit_Runs_Without_A_Prompt()
  {
    using SpaTestScope scope = SpaTestScope.Create(Spa!);
    scope.ServiceProvider.GetRequiredService<NavigationManager>().NavigateTo("/Counter");
    scope.Store.GetState<CounterState>().Initialize(count: 10);
    await scope.Store.GetState<AgentSurfaceState>().SetEditMode(AgentEditMode.AutomaticallyEdit);
    WebMcpDispatcher dispatcher = scope.ServiceProvider.GetRequiredService<WebMcpDispatcher>();

    string result = await dispatcher.InvokeTool("Counter.IncrementCounter", """{"amount":5}""")
      .WaitAsync(Timeout);

    result.ShouldContain("\"completed\":true");
    scope.Store.GetState<CounterState>().Count.ShouldBe(15);
    scope.Store.GetState<AgentSurfaceState>().HasPendingApproval.ShouldBeFalse();
  }

  public static async Task Expired_Credential_Refuses_After_Approval_And_Without_A_Prompt_In_Auto_Mode()
  {
    using SpaTestScope scope = SpaTestScope.Create(Spa!);
    scope.ServiceProvider.GetRequiredService<NavigationManager>().NavigateTo("/Counter");
    scope.Store.GetState<CounterState>().Initialize(count: 10);
    AgentSurfaceState surface = scope.Store.GetState<AgentSurfaceState>();
    await RememberConversationAsync(scope, surface, [.. PermissionIds.All], expired: true);
    WebMcpDispatcher dispatcher = scope.ServiceProvider.GetRequiredService<WebMcpDispatcher>();

    Task<string> asking = dispatcher.InvokeTool("Counter.IncrementCounter", """{"amount":5}""");
    Guid callId = await WaitForPendingAsync(scope, asking);
    await surface.ResolveApproval(callId, approved: true);
    string asked = await asking.WaitAsync(Timeout);
    ShouldReport(asked, AgentConversationAuthority.ExpiredError);
    scope.Store.GetState<CounterState>().Count.ShouldBe(10);

    await surface.SetEditMode(AgentEditMode.AutomaticallyEdit);
    string automatic = await dispatcher.InvokeTool("Counter.IncrementCounter", """{"amount":5}""")
      .WaitAsync(Timeout);
    ShouldReport(automatic, AgentConversationAuthority.ExpiredError);
    scope.Store.GetState<AgentSurfaceState>().HasPendingApproval.ShouldBeFalse();
    scope.Store.GetState<CounterState>().Count.ShouldBe(10);

    using CatalogAgentFunctions functions = CatalogAgentFunctions.Create
    (
      await SelectForSessionAsync(scope, AgentEditMode.AutomaticallyEdit)
    );
    AIFunction increment = functions.Tools[0].ShouldBeAssignableTo<AIFunction>();
    increment.ShouldNotBeOfType<ApprovalRequiredAIFunction>();
    object? chat = await increment.InvokeAsync(Arguments(scope, """{"amount":5}"""));
    string expiredText = chat.ShouldNotBeNull().ToString()!;
    expiredText.ShouldContain(AgentConversationAuthority.ExpiredError);
    scope.Store.GetState<CounterState>().Count.ShouldBe(10);
  }

  public static async Task Credential_Scope_And_Principal_Block_Both_Drivers()
  {
    using SpaTestScope scope = SpaTestScope.Create(Spa!);
    NavigationManager navigation = scope.ServiceProvider.GetRequiredService<NavigationManager>();
    navigation.NavigateTo("/Admin/Roles/New");
    AgentSurfaceState surface = scope.Store.GetState<AgentSurfaceState>();
    await surface.SetEditMode(AgentEditMode.AutomaticallyEdit);
    string[] scopes = [.. PermissionIds.All.Where(permission => permission != PermissionIds.AdminRolesManage)];
    await RememberConversationAsync(scope, surface, scopes, expired: false);
    WebMcpDispatcher dispatcher = scope.ServiceProvider.GetRequiredService<WebMcpDispatcher>();
    const string arguments = """{"command":{"name":"Denied","description":"no"}}""";

    using CatalogAgentFunctions functions = CatalogAgentFunctions.Create
    (
      await SelectForSessionAsync(scope, AgentEditMode.AutomaticallyEdit)
    );
    AIFunction createRole = functions.Tools.Single(tool => tool.Name == "Role.CreateRole")
      .ShouldBeAssignableTo<AIFunction>();
    object? chat = await createRole.InvokeAsync(Arguments(scope, arguments));
    string scopeText = chat.ShouldNotBeNull().ToString()!;
    scopeText.ShouldContain(AgentConversationAuthority.ScopeError);

    string web = await dispatcher.InvokeTool("Role.CreateRole", arguments).WaitAsync(Timeout);
    ShouldReport(web, AgentConversationAuthority.ScopeError);
    scope.Store.GetState<RoleState>().LastCreatedRoleId.ShouldBeNull();
    scope.Store.GetState<AgentSurfaceState>().HasPendingApproval.ShouldBeFalse();

    navigation.NavigateTo("/Counter");
    scope.Store.GetState<CounterState>().Initialize(count: 10);
    AgentConversationCredential foreign = AgentConversationCredentialIssuer.Issue
    (
      Guid.NewGuid(),
      [.. PermissionIds.All],
      FutureNoon(),
      TimeSpan.FromHours(1)
    );
    await surface.SetConversation
    (
      foreign.Id,
      foreign.PrincipalId,
      [.. foreign.Scopes],
      foreign.ExpiresAt,
      foreign.DisplayName
    );
    string mismatch = await dispatcher.InvokeTool("Counter.IncrementCounter", """{"amount":5}""")
      .WaitAsync(Timeout);
    ShouldReport(mismatch, AgentConversationAuthority.PrincipalError);
    scope.Store.GetState<CounterState>().Count.ShouldBe(10);
    scope.Store.GetState<AgentSurfaceState>().HasPendingApproval.ShouldBeFalse();
  }

  private static async Task AssertSameSurfaceAsync
  (
    ClaimsPrincipal principal,
    IAuthorizationService authorization,
    IActionCatalog catalog,
    string route,
    AgentEditMode mode
  )
  {
    IReadOnlyList<CatalogAgentTool> selected = await CatalogAgentToolSet.SelectAsync
    (
      principal,
      authorization,
      catalog.Entries,
      route,
      mode,
      CancellationToken.None
    );
    using CatalogAgentFunctions functions = CatalogAgentFunctions.Create(selected);
    IReadOnlyList<WebMcpToolDescriptor> web = await WebMcpPublisher.DescribeAsync
    (
      principal,
      authorization,
      catalog,
      route,
      mode,
      CancellationToken.None
    );
    string because = $"{route} {mode}";
    functions.Tools.Count.ShouldBe(web.Count, because);
    for (int index = 0; index < web.Count; index++)
    {
      AITool aiTool = functions.Tools[index];
      WebMcpToolDescriptor mcp = web[index];
      aiTool.Name.ShouldBe(mcp.Name, because);
      aiTool.Description.ShouldBe(mcp.Description, because);
      (aiTool is ApprovalRequiredAIFunction).ShouldBe(mcp.RequiresApproval, $"{because} {mcp.Name}");
      AIFunction function = aiTool.ShouldBeAssignableTo<AIFunction>();
      using JsonDocument aiSchema = JsonDocument.Parse(function.JsonSchema.GetRawText());
      using JsonDocument mcpSchema = JsonDocument.Parse(mcp.InputSchemaJson);
      JsonElement.DeepEquals(aiSchema.RootElement, mcpSchema.RootElement)
        .ShouldBeTrue($"{because} {mcp.Name}");
    }
  }

  private static async Task RememberConversationAsync
  (
    SpaTestScope scope,
    AgentSurfaceState surface,
    IReadOnlyList<string> scopes,
    bool expired
  )
  {
    AuthenticationState authentication = await scope.ServiceProvider
      .GetRequiredService<AuthenticationStateProvider>()
      .GetAuthenticationStateAsync();
    Guid principalId = Guid.Parse(authentication.User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
    DateTimeOffset utcNow = expired ? FutureNoon().AddDays(-3) : FutureNoon();
    AgentConversationCredential credential = AgentConversationCredentialIssuer.Issue
    (
      principalId,
      scopes,
      utcNow,
      TimeSpan.FromHours(1)
    );
    await surface.SetConversation
    (
      credential.Id,
      credential.PrincipalId,
      [.. credential.Scopes],
      credential.ExpiresAt,
      credential.DisplayName
    );
  }

  private static async Task<IReadOnlyList<CatalogAgentTool>> SelectForSessionAsync
  (
    SpaTestScope scope,
    AgentEditMode mode
  )
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
      mode,
      CancellationToken.None
    );
  }

  /// <summary>Mid-day tomorrow (UTC): a credential issued then is valid now and far from any midnight.</summary>
  private static DateTimeOffset FutureNoon() =>
    new(DateTimeOffset.UtcNow.UtcDateTime.Date.AddDays(1).AddHours(12), TimeSpan.Zero);

  private static AIFunctionArguments Arguments(SpaTestScope scope, string json)
  {
    using JsonDocument document = JsonDocument.Parse(json);
    Dictionary<string, object?> values = [];
    foreach (JsonProperty property in document.RootElement.EnumerateObject())
    {
      values[property.Name] = property.Value.Clone();
    }

    return new AIFunctionArguments(values) { Services = scope.ServiceProvider };
  }

  private static void ShouldReport(string json, string message)
  {
    using JsonDocument document = JsonDocument.Parse(json);
    document.RootElement.GetProperty("error").GetString().ShouldBe(message);
  }
}

#region Purpose
// Task 292: in-app and WebMCP tool lists match, and edit mode plus the conversation credential
// behave the same on both drivers.
#endregion

#region Design
// The parity loop is every known page route, two routes with no catalog tools, a feedback detail
// route, and /FeedbackExtra, crossed with four principals and both edit modes. Each pair compares
// name, description, input schema, and the approval bit, in order. Listing does not take a
// credential. Invoke tests put one on AgentSurfaceState and drive WebMCP and the in-app function.
#endregion

namespace CatalogAgent_;

using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.AI;
using TimeWarp.Architecture.Features;
using TimeWarp.Architecture.Features.Admin.Roles;
using TimeWarp.Architecture.Features.Applications;

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

    Guid principalId = Guid.NewGuid();
    ClaimsPrincipal user = new(new ClaimsIdentity(
      [new Claim(ClaimTypes.NameIdentifier, principalId.ToString())],
      authenticationType: "Test"));
    AgentConversationCredential credential = AgentConversationCredentialIssuer.Issue
    (
      principalId,
      ["perm.a"],
      DateTimeOffset.UtcNow,
      TimeSpan.FromHours(1)
    );
    AgentConversationAuthority.Denial(null, user, ["perm.a"]).ShouldBeNull();
    AgentConversationAuthority.Denial(credential, user, []).ShouldBeNull();
    AgentConversationAuthority.Denial(credential, user, ["perm.a"]).ShouldBeNull();
    AgentConversationAuthority.Denial(credential, user, ["perm.missing"])
      .ShouldBe(AgentConversationAuthority.ScopeError);
    ClaimsPrincipal other = new(new ClaimsIdentity(
      [new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString())],
      authenticationType: "Test"));
    AgentConversationAuthority.Denial(credential, other, []).ShouldBe(AgentConversationAuthority.PrincipalError);
    ClaimsPrincipal nameless = new(new ClaimsIdentity([new Claim(ClaimTypes.Name, "n")], authenticationType: "Test"));
    AgentConversationAuthority.Denial(credential, nameless, []).ShouldBeNull();
    AgentConversationCredential expired = AgentConversationCredentialIssuer.Issue
    (
      principalId,
      ["perm.a"],
      DateTimeOffset.UtcNow.AddDays(-2),
      TimeSpan.FromHours(1)
    );
    AgentConversationAuthority.Denial(expired, other, ["perm.missing"])
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
      DateTimeOffset.UtcNow,
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
    DateTimeOffset utcNow = expired ? DateTimeOffset.UtcNow.AddDays(-2) : DateTimeOffset.UtcNow;
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

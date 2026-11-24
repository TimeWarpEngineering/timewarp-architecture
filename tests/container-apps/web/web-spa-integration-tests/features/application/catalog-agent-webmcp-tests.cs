#region Purpose
// WebMCP registration and invoke tests: approval, reject, refusals, pending and stale calls, navigation cancel.
#endregion

namespace CatalogAgent_;

using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.AI;
using TimeWarp.Architecture.Components;
using TimeWarp.Architecture.Features;
using TimeWarp.Architecture.Features.Profiles;

[TestTag("Integration")]
public partial class CatalogAgent_Should
{
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
    home.ShouldContain(tool => tool.Name == AgentSiteMap.ToolName);
    home.ShouldContain(tool => tool.Name == PageAgentContext.ToolName);
    home.ShouldNotContain(tool => tool.Name == "Profile.SignOut");
    home.ShouldNotContain(tool => tool.Name == "Credentials.AddExistingPasskey");
    // A literal, not the selection algorithm again: the tool list on / that the task promises.
    Names(home).ShouldBe(
    [
      "Credentials.AddPasskey",
      "Feedback.ListMyFeedback",
      "Role.OpenNewRole",
      "Screenshot.CaptureScreenshot",
      "Prefill.Profile.UpdateProfile",
      "Prefill.Role.CreateRole",
      "Prefill.SiteSettings.UpdateAssistantName",
      "Prefill.SiteSettings.UpdateSiteSettings",
      "Prefill.SiteSettings.UpdateXaiByokMode",
      AgentNavigate.ToolName,
      AgentSiteMap.ToolName,
      PageAgentContext.ToolName,
    ]);
    Console.WriteLine($"WEBMCP-PROOF path=/ tools={string.Join(",", Names(home))}");

    IReadOnlyList<WebMcpToolDescriptor> settings = await WebMcpPublisher.DescribeAsync
    (
      everyone, authorization, catalog, "/Settings", CancellationToken.None
    );
    Names(settings).ShouldBe(await ExpectedWebMcpNamesAsync(everyone, authorization, catalog, "/Settings"));
    settings.ShouldContain(tool => tool.Name == "Credentials.AddPasskey");
    settings.ShouldNotContain(tool => tool.Name == "Credentials.LinkMicrosoft365");

    IReadOnlyList<WebMcpToolDescriptor> designSystem = await WebMcpPublisher.DescribeAsync
    (
      Principal(PermissionIds.DeveloperAccess),
      authorization,
      catalog,
      "/DesignSystem",
      CancellationToken.None
    );
    Names(designSystem).ShouldBe(["Screenshot.CaptureScreenshot", AgentNavigate.ToolName, AgentSiteMap.ToolName, PageAgentContext.ToolName]);
    Names
    (
      await WebMcpPublisher.DescribeAsync
      (
        Principal(PermissionIds.DeveloperAccess),
        authorization,
        catalog,
        "/DesignSystem/drawers",
        CancellationToken.None
      )
    ).ShouldBe(Names(designSystem));

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
    ).ShouldBe(["Screenshot.CaptureScreenshot", AgentNavigate.ToolName, AgentSiteMap.ToolName, PageAgentContext.ToolName]);
    Names
    (
      await WebMcpPublisher.DescribeAsync
      (
        Principal(), authorization, catalog, "/Profile", CancellationToken.None
      )
    ).ShouldBe(["Screenshot.CaptureScreenshot", AgentNavigate.ToolName, AgentSiteMap.ToolName, PageAgentContext.ToolName]);

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

  public static async Task Prefill_Profile_Update_Is_Optional_And_Offered_Off_Page()
  {
    using SpaTestScope scope = SpaTestScope.Create(Spa!);
    IActionCatalog catalog = scope.ServiceProvider.GetRequiredService<IActionCatalog>();
    IAuthorizationService authorization = scope.ServiceProvider.GetRequiredService<IAuthorizationService>();
    ClaimsPrincipal everyone = Principal([.. PermissionIds.All]);

    IReadOnlyList<CatalogAgentTool> home = await CatalogAgentToolSet.SelectAsync
    (
      everyone, authorization, catalog.Entries, "/", AgentEditMode.AskBeforeEditing, CancellationToken.None
    );
    CatalogAgentTool offPage = home.Single(tool => tool.Name == "Prefill.Profile.UpdateProfile");
    offPage.Prefill.ShouldBeTrue();
    offPage.OffPageRoute.ShouldBe("/Profile");
    offPage.RequiresApproval.ShouldBeFalse();
    offPage.Entry.ShouldNotBeNull().Name.ShouldBe("Profile.UpdateProfile");
    home.ShouldNotContain(tool => tool.Name == "FormSuggestion.SetFormSuggestion");

    using JsonDocument schema = JsonDocument.Parse(offPage.InputSchema);
    schema.RootElement.TryGetProperty("required", out _).ShouldBeFalse();
    schema.RootElement.GetProperty("additionalProperties").GetBoolean().ShouldBeFalse();
    JsonElement properties = schema.RootElement.GetProperty("properties");
    properties.GetProperty("displayName").GetProperty("type").GetString().ShouldBe("string");
    properties.GetProperty("email").GetProperty("type").GetString().ShouldBe("string");
    properties.GetProperty("notifications").GetProperty("type").GetString().ShouldBe("boolean");
    properties.TryGetProperty("userId", out _).ShouldBeFalse();

    IReadOnlyList<CatalogAgentTool> profile = await CatalogAgentToolSet.SelectAsync
    (
      everyone, authorization, catalog.Entries, "/Profile", AgentEditMode.AskBeforeEditing, CancellationToken.None
    );
    CatalogAgentTool onPage = profile.Single(tool => tool.Name == "Prefill.Profile.UpdateProfile");
    onPage.OffPageRoute.ShouldBeNull();
    onPage.RequiresApproval.ShouldBeTrue();
    profile.ShouldContain(tool => tool.Name == "Profile.UpdateProfile");

    IReadOnlyList<CatalogAgentTool> automatic = await CatalogAgentToolSet.SelectAsync
    (
      everyone, authorization, catalog.Entries, "/Profile", AgentEditMode.AutomaticallyEdit, CancellationToken.None
    );
    automatic.Single(tool => tool.Name == "Prefill.Profile.UpdateProfile").RequiresApproval.ShouldBeFalse();

    (await CatalogAgentToolSet.SelectAsync
    (
      Principal(PermissionIds.ProfileRead),
      authorization,
      catalog.Entries,
      "/Profile",
      AgentEditMode.AskBeforeEditing,
      CancellationToken.None
    )).ShouldNotContain(tool => tool.Name == "Prefill.Profile.UpdateProfile");

    (await CatalogAgentToolSet.SelectAsync
    (
      new ClaimsPrincipal(new ClaimsIdentity()),
      authorization,
      catalog.Entries,
      "/",
      AgentEditMode.AskBeforeEditing,
      CancellationToken.None
    )).ShouldNotContain(tool => tool.Prefill);
  }

  public static async Task WebMcp_Prefill_On_Profile_Asks_Then_Stores_The_Draft()
  {
    using SpaTestScope scope = SpaTestScope.Create(Spa!);
    scope.ServiceProvider.GetRequiredService<NavigationManager>().NavigateTo("/Profile");
    WebMcpDispatcher dispatcher = scope.ServiceProvider.GetRequiredService<WebMcpDispatcher>();

    Task<string> invoke = dispatcher.InvokeTool(
      "Prefill.Profile.UpdateProfile",
      """{"displayName":"Ada","notifications":false,"extra":"ignored"}""");
    Guid callId = await WaitForPendingAsync(scope, invoke);
    AgentSurfaceState surface = scope.Store.GetState<AgentSurfaceState>();
    surface.PendingToolName.ShouldBe("Prefill.Profile.UpdateProfile");
    surface.PendingArgumentsJson.ShouldNotBeNull().ShouldContain("displayName");
    ProfileDraft(scope).ShouldBeNull();

    await surface.ResolveApproval(callId, approved: true);
    string result = await invoke.WaitAsync(Timeout);
    result.ShouldContain("\"drafted\":true");
    FormSuggestion stored = ProfileDraft(scope).ShouldNotBeNull();
    scope.Store.GetState<ProfileState>().Profile.ShouldBeNull();
    stored.Json.ShouldContain("Ada");
    stored.Json.ShouldNotContain("extra");
    stored.Id.ShouldNotBeNullOrWhiteSpace();
    stored.Accepted.ShouldBeFalse();
  }

  public static async Task WebMcp_Prefill_Reject_Leaves_No_Draft()
  {
    using SpaTestScope scope = SpaTestScope.Create(Spa!);
    scope.ServiceProvider.GetRequiredService<NavigationManager>().NavigateTo("/Profile");
    WebMcpDispatcher dispatcher = scope.ServiceProvider.GetRequiredService<WebMcpDispatcher>();

    Task<string> invoke = dispatcher.InvokeTool("Prefill.Profile.UpdateProfile", """{"displayName":"Ada"}""");
    Guid callId = await WaitForPendingAsync(scope, invoke);
    await scope.Store.GetState<AgentSurfaceState>().ResolveApproval(callId, approved: false);
    string result = await invoke.WaitAsync(Timeout);

    result.ShouldContain("\"approved\":false");
    ProfileDraft(scope).ShouldBeNull();
    scope.Store.GetState<AgentSurfaceState>().HasPendingApproval.ShouldBeFalse();
  }

  public static async Task WebMcp_Prefill_Off_Page_Returns_Navigate_And_Automatic_Stores_Without_A_Prompt()
  {
    using SpaTestScope scope = SpaTestScope.Create(Spa!);
    NavigationManager navigation = scope.ServiceProvider.GetRequiredService<NavigationManager>();
    navigation.NavigateTo("/");
    WebMcpDispatcher dispatcher = scope.ServiceProvider.GetRequiredService<WebMcpDispatcher>();

    string offer = await dispatcher.InvokeTool("Prefill.Profile.UpdateProfile", """{"displayName":"Ada"}""")
      .WaitAsync(Timeout);
    offer.ShouldContain("\"navigateTo\":\"/Profile\"");
    offer.ShouldContain("\"executed\":false");
    scope.Store.GetState<AgentSurfaceState>().HasPendingApproval.ShouldBeFalse();
    ProfileDraft(scope).ShouldBeNull();

    await scope.Send(new AgentSurfaceState.SetEditModeActionSet.Action(AgentEditMode.AutomaticallyEdit));
    navigation.NavigateTo("/Profile");
    string drafted = await dispatcher.InvokeTool(
      "Prefill.Profile.UpdateProfile",
      """{"email":null,"displayName":"Grace"}""").WaitAsync(Timeout);
    drafted.ShouldContain("\"drafted\":true");
    scope.Store.GetState<AgentSurfaceState>().HasPendingApproval.ShouldBeFalse();
    string json = ProfileDraft(scope).ShouldNotBeNull().Json;
    json.ShouldContain("Grace");
    json.ShouldContain("null");
  }

  public static async Task Ask_Prefill_Automatic_Stores_The_Draft_And_Off_Page_Offers_Navigate()
  {
    using SpaTestScope scope = SpaTestScope.Create(Spa!);
    NavigationManager navigation = scope.ServiceProvider.GetRequiredService<NavigationManager>();
    AgentSurfaceState surface = scope.Store.GetState<AgentSurfaceState>();
    navigation.NavigateTo("/");
    await surface.SetEditMode(AgentEditMode.AskBeforeEditing);
    using (CatalogAgentFunctions home = CatalogAgentFunctions.Create
    (
      await SelectForSessionAsync(scope, AgentEditMode.AskBeforeEditing)
    ))
    {
      AIFunction offPage = home.Tools.Single(tool => tool.Name == "Prefill.Profile.UpdateProfile")
        .ShouldBeAssignableTo<AIFunction>();
      offPage.ShouldNotBeOfType<ApprovalRequiredAIFunction>();
      object? offer = await offPage.InvokeAsync(Arguments(scope, """{"displayName":"Ada"}"""));
      offer.ShouldBeOfType<AgentNavigate.Offer>().NavigateTo.ShouldBe("/Profile");
    }

    navigation.NavigateTo("/Profile");
    await surface.SetEditMode(AgentEditMode.AutomaticallyEdit);
    using CatalogAgentFunctions functions = CatalogAgentFunctions.Create
    (
      await SelectForSessionAsync(scope, AgentEditMode.AutomaticallyEdit)
    );
    AIFunction prefill = functions.Tools.Single(tool => tool.Name == "Prefill.Profile.UpdateProfile")
      .ShouldBeAssignableTo<AIFunction>();
    prefill.ShouldNotBeOfType<ApprovalRequiredAIFunction>();
    object? drafted = await prefill.InvokeAsync(Arguments(scope, """{"displayName":"Ada","notifications":true}"""));
    FormPrefill.Result result = drafted.ShouldBeOfType<FormPrefill.Result>();
    result.Drafted.ShouldBeTrue();
    result.Fields.ShouldBe(["displayName", "notifications"]);
    scope.Store.GetState<ProfileState>().Profile.ShouldBeNull();
    ProfileDraft(scope).ShouldNotBeNull().Json.ShouldContain("Ada");
  }

  public static async Task Prefill_With_No_Matching_Field_Does_Not_Store_A_Draft()
  {
    using SpaTestScope scope = SpaTestScope.Create(Spa!);
    NavigationManager navigation = scope.ServiceProvider.GetRequiredService<NavigationManager>();
    navigation.NavigateTo("/Profile");
    await scope.Store.GetState<AgentSurfaceState>().SetEditMode(AgentEditMode.AutomaticallyEdit);

    WebMcpDispatcher dispatcher = scope.ServiceProvider.GetRequiredService<WebMcpDispatcher>();
    string empty = await dispatcher.InvokeTool("Prefill.Profile.UpdateProfile", "").WaitAsync(Timeout);
    empty.ShouldContain("\"drafted\":false");
    empty.ShouldContain("displayName");
    empty.ShouldContain("notifications");
    ProfileDraft(scope).ShouldBeNull();

    string unknown = await dispatcher.InvokeTool(
      "Prefill.Profile.UpdateProfile",
      """{"nope":1}""").WaitAsync(Timeout);
    unknown.ShouldContain("\"drafted\":false");
    unknown.ShouldContain("Accepted keys");
    ProfileDraft(scope).ShouldBeNull();

    using CatalogAgentFunctions functions = CatalogAgentFunctions.Create
    (
      await SelectForSessionAsync(scope, AgentEditMode.AutomaticallyEdit)
    );
    AIFunction prefill = functions.Tools.Single(tool => tool.Name == "Prefill.Profile.UpdateProfile")
      .ShouldBeAssignableTo<AIFunction>();
    object? drafted = await prefill.InvokeAsync(Arguments(scope, "{}"));
    FormPrefill.Result result = drafted.ShouldBeOfType<FormPrefill.Result>();
    result.Drafted.ShouldBeFalse();
    result.Error.ShouldNotBeNull().ShouldContain("email");
    result.Fields.ShouldBeEmpty();
    ProfileDraft(scope).ShouldBeNull();
  }

  public static async Task WebMcp_Lists_A_Prefill_Tool_For_Every_Edit_Form()
  {
    using SpaTestScope scope = SpaTestScope.Create(Spa!);
    IActionCatalog catalog = scope.ServiceProvider.GetRequiredService<IActionCatalog>();
    IAuthorizationService authorization = scope.ServiceProvider.GetRequiredService<IAuthorizationService>();
    ClaimsPrincipal everyone = Principal([.. PermissionIds.All]);
    (string Path, string[] OnPage)[] pages =
    [
      ("/Profile", ["Prefill.Profile.UpdateProfile"]),
      ("/Admin/Roles/New", ["Prefill.Role.CreateRole"]),
      ("/Admin/Roles/" + Guid.NewGuid().ToString("D"), ["Prefill.Role.UpdateRole"]),
      ("/Admin/AI", ["Prefill.SiteSettings.UpdateAssistantName", "Prefill.SiteSettings.UpdateXaiByokMode"]),
      ("/Admin/Authentication", ["Prefill.SiteSettings.UpdateSiteSettings"]),
    ];

    foreach ((string path, string[] onPage) in pages)
    {
      IReadOnlyList<WebMcpToolDescriptor> tools = await WebMcpPublisher.DescribeAsync
      (
        everyone, authorization, catalog, path, CancellationToken.None
      );
      string[] prefill = [.. Names(tools).Where(static name => name.StartsWith(FormPrefill.Prefix, StringComparison.Ordinal))];
      foreach (string name in onPage)
      {
        prefill.ShouldContain(name);
      }

      Console.WriteLine($"PREFILL-PROOF path={path} tools={string.Join(",", prefill)}");
    }

    // Off the role detail page there is no static route to offer, so UpdateRole is not listed.
    Names(await WebMcpPublisher.DescribeAsync(everyone, authorization, catalog, "/", CancellationToken.None))
      .ShouldNotContain("Prefill.Role.UpdateRole");
  }

  private static FormSuggestion? ProfileDraft(SpaTestScope scope) =>
    scope.Store.GetState<FormSuggestionState>()
      .For(FormSuggestionState.KeyFor(typeof(ProfileState.UpdateProfileActionSet.Action)));
}

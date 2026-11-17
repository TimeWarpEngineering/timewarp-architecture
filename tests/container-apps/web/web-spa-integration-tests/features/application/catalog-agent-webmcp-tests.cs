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
      ["Credentials.AddPasskey", "Feedback.ListMyFeedback", "Screenshot.CaptureScreenshot", AgentNavigate.ToolName, AgentSiteMap.ToolName, PageAgentContext.ToolName]);
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
}

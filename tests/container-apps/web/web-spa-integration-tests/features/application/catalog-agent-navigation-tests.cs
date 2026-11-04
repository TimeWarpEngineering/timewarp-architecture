#region Purpose
// site_map and navigate destinations, and the AddPasskey navigate-versus-run behavior.
#endregion

namespace CatalogAgent_;

using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.AI;
using TimeWarp.Architecture.Features;

[TestTag("Integration")]
public partial class CatalogAgent_Should
{
  public static async Task Navigate_Matches_Menu_Destinations_And_Refuses_The_Rest()
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
    navigate.RequiresApproval.ShouldBeTrue();
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
      scope.Store,
      "/",
      new Dictionary<string, object?> { ["url"] = "/Settings" },
      CancellationToken.None
    );
    allowed.Navigated.ShouldBeTrue();
    allowed.Destination.ShouldBe("/Settings");
    allowed.Title.ShouldBe("Settings");
    navigation.Uri.ShouldContain("/Settings");

    navigation.NavigateTo("/");
    WebMcpDispatcher dispatcher = scope.ServiceProvider.GetRequiredService<WebMcpDispatcher>();
    string missing = await dispatcher.InvokeTool("navigate", """{"url":"/not-a-page"}""").WaitAsync(Timeout);
    missing.ShouldContain(AgentNavigate.Refusal);
    missing.ShouldContain("\"navigated\":false");
    scope.Store.GetState<AgentSurfaceState>().HasPendingApproval.ShouldBeFalse();

    // Ask before editing: a permitted url waits for approval, a rejection does not move.
    Task<string> pending = dispatcher.InvokeTool("navigate", """{"url":"/Settings"}""");
    Guid callId = await WaitForPendingAsync(scope, pending);
    await scope.Store.GetState<AgentSurfaceState>().ResolveApproval(callId, approved: false);
    (await pending.WaitAsync(Timeout)).ShouldContain("\"approved\":false");
    navigation.Uri.ShouldNotContain("/Settings");

    pending = dispatcher.InvokeTool("navigate", """{"url":"/Settings"}""");
    callId = await WaitForPendingAsync(scope, pending);
    await scope.Store.GetState<AgentSurfaceState>().ResolveApproval(callId, approved: true);
    (await pending.WaitAsync(Timeout)).ShouldContain("\"navigated\":true");
    navigation.Uri.ShouldContain("/Settings");

    // Automatically edit: navigate runs without a prompt.
    navigation.NavigateTo("/");
    await scope.Send(new AgentSurfaceState.SetEditModeActionSet.Action(AgentEditMode.AutomaticallyEdit));
    (await dispatcher.InvokeTool("navigate", """{"url":"/Settings"}""").WaitAsync(Timeout))
      .ShouldContain("\"navigated\":true");
    scope.Store.GetState<AgentSurfaceState>().HasPendingApproval.ShouldBeFalse();
  }

  public static async Task Site_Map_And_Navigate_Include_Design_System_Parts()
  {
    using SpaTestScope scope = SpaTestScope.Create(Spa!);
    IActionCatalog catalog = scope.ServiceProvider.GetRequiredService<IActionCatalog>();
    IAuthorizationService authorization = scope.ServiceProvider.GetRequiredService<IAuthorizationService>();
    ClaimsPrincipal developer = Principal(PermissionIds.DeveloperAccess);

    AgentSiteMap.Result map = await AgentSiteMap.InvokeAsync(developer, authorization, scope.Store, "/", CancellationToken.None);
    map.Error.ShouldBeNull();
    AgentSiteMap.Destination filterBar = map.Destinations.Single(destination => destination.Route == "/DesignSystem/filter-bar");
    filterBar.Title.ShouldBe("Design System: Filter Bar");
    filterBar.Parent.ShouldBe("/DesignSystem");
    filterBar.Description.ShouldContain("Filter Bar");
    map.Destinations.ShouldContain(destination => destination.Route == "/DesignSystem/buttons");

    IReadOnlyList<CatalogAgentTool> tools = await CatalogAgentToolSet.SelectAsync
    (
      developer, authorization, catalog.Entries, "/", CancellationToken.None
    );
    CatalogAgentTool navigate = tools.Single(tool => tool.Name == AgentNavigate.ToolName);
    navigate.InputSchema.ShouldContain("\"/DesignSystem/filter-bar\"");
    navigate.Description.ShouldContain("/DesignSystem/<part> for Design System parts:");
    navigate.Description.ShouldContain("filter-bar");

    string summary = AgentSiteMap.Summary(map.Destinations, AgentAskInstructions.SiteMapCap);
    summary.ShouldContain("filter-bar");
    summary.ShouldContain("buttons");

    NavigationManager navigation = scope.ServiceProvider.GetRequiredService<NavigationManager>();
    AgentNavigate.Result moved = await AgentNavigate.InvokeAsync
    (
      developer,
      authorization,
      scope.Store,
      "/",
      new Dictionary<string, object?> { ["url"] = "/DesignSystem/filter-bar" },
      CancellationToken.None
    );
    moved.Navigated.ShouldBeTrue();
    moved.Destination.ShouldBe("/DesignSystem/filter-bar");
    moved.Title.ShouldBe("Design System: Filter Bar");
    navigation.Uri.ShouldEndWith("/DesignSystem/filter-bar");

    // Without the Design System page, its parts are neither listed nor reachable.
    ClaimsPrincipal member = Principal(PermissionIds.ProfileRead, PermissionIds.SettingsRead);
    AgentSiteMap.Result memberMap = await AgentSiteMap.InvokeAsync(member, authorization, scope.Store, "/", CancellationToken.None);
    memberMap.Destinations.ShouldNotContain(destination => destination.Parent != null);
    AgentNavigate.Result refused = await AgentNavigate.InvokeAsync
    (
      member,
      authorization,
      scope.Store,
      "/",
      new Dictionary<string, object?> { ["url"] = "/DesignSystem/filter-bar" },
      CancellationToken.None
    );
    refused.Navigated.ShouldBeFalse();
    refused.Error.ShouldBe(AgentNavigate.Refusal);
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
    // Mediator 15 resolves the handler inside the pipeline. The closed-box Aspire SPA registers no
    // web-server BFF client, so activation fails. StateTransactionBehavior publishes
    // ExceptionNotification and the dispatcher returns completed. A navigate offer would not add
    // this error bar.
    string executedJson = await dispatcher.InvokeTool("Credentials.AddPasskey", null).WaitAsync(Timeout);
    using (JsonDocument executed = JsonDocument.Parse(executedJson))
    {
      JsonElement root = executed.RootElement;
      root.GetProperty("completed").GetBoolean().ShouldBeTrue();
      root.TryGetProperty("navigateTo", out _).ShouldBeFalse();
    }

    scope.Store.GetState<AgentSurfaceState>().HasPendingApproval.ShouldBeFalse();
    NotificationState notifications = scope.Store.GetState<NotificationState>();
    notifications.Messages.Count.ShouldBe(messages + 1);
    NotificationState.NotificationMessage activation = notifications.Messages[^1];
    activation.Intent.ShouldBe(MessageBarIntent.Error);
    activation.Title.ShouldContain("IWebServerApiService");
  }
}

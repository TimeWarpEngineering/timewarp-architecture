#region Purpose
// Runs a WebMCP tool call through the same page, permission, approval, and store path as the ask UI.
#endregion

#region Design
// The browser agent is not trusted. InvokeTool selects the tools again for the current principal
// and route, so a stale registration cannot call an action the person can no longer run.
// page_context is not a catalog action; it returns the page facts and does not ask for approval.
// The route is the shell's PageAgentRoute when one has been observed.
// Arguments are parsed and bound before anything is shown: a call that cannot bind returns
// {action, error} without a prompt, and the banner shows the canonical rendering of the bound
// values (CatalogAgentArguments.Render). Execute receives exactly that bound array.
// Mutating tools take the single WebMcpApprovalGate slot. A second mutating call while one waits
// is refused with an error instead of replacing the call on screen. While waiting, the dispatcher
// listens to LocationChanged and cancels its own call on any navigation, which also covers
// focused pages that do not host the banner. After the wait it clears its banner (ResolveApproval
// with its own id is a no-op if already answered), then refuses with PageChangedError if any
// navigation happened while waiting (even one that returns to the same path, such as a query-only
// change). Otherwise it refuses unless a fresh selection for the current principal still offers the
// same tool, and it checks the path once more after that await, right before Execute.
// Result JSON uses the contract seam options. Execute failures are not mapped: handlers report
// their own outcomes on NotificationState.
#endregion

namespace TimeWarp.Architecture.Features.Applications;

using Microsoft.AspNetCore.Components.Routing;
using System.Text.Json.Serialization;

/// <summary>JS entry point for WebMCP tool execution.</summary>
[SideEffectService]
public sealed class WebMcpDispatcher
{
  public const string BusyError = "Another action is waiting for confirmation.";
  public const string UnavailableError = "The action is not available on this page.";
  public const string PageChangedError = "The page changed before the action ran.";

  private readonly IStore Store;
  private readonly IActionCatalog Catalog;
  private readonly IAuthorizationService AuthorizationService;
  private readonly AuthenticationStateProvider AuthenticationStateProvider;
  private readonly NavigationManager Navigation;
  private readonly PageAgentRoute Route;
  private readonly WebMcpApprovalGate Gate;
  private readonly AgentCallOutcome AgentCallOutcome;

  public WebMcpDispatcher
  (
    IStore store,
    IActionCatalog catalog,
    IAuthorizationService authorizationService,
    AuthenticationStateProvider authenticationStateProvider,
    NavigationManager navigation,
    PageAgentRoute route,
    WebMcpApprovalGate gate,
    AgentCallOutcome agentCallOutcome
  )
  {
    Store = store;
    Catalog = catalog;
    AuthorizationService = authorizationService;
    AuthenticationStateProvider = authenticationStateProvider;
    Navigation = navigation;
    Route = route;
    Gate = gate;
    AgentCallOutcome = agentCallOutcome;
  }

  [JSInvokable]
  public async Task<string> InvokeTool(string name, string? argumentsJson)
  {
    ArgumentException.ThrowIfNullOrEmpty(name);
    string path = Route.PathOr(Navigation);
    if (string.Equals(name, PageAgentContext.ToolName, StringComparison.Ordinal))
    {
      string? pageDenial = await CredentialDenialAsync([]);
      if (pageDenial is not null)
      {
        return Error(name, pageDenial);
      }

      return PageAgentContext.Describe(Store, path);
    }

    CatalogAgentTool? tool = await FindOfferedAsync(path, name);
    if (tool is null)
    {
      return Error(name, UnavailableError);
    }

    object?[] bound;
    string rendered;
    try
    {
      bound = CatalogAgentArguments.Bind(tool.Entry, ParseArguments(argumentsJson));
      rendered = CatalogAgentArguments.Render(tool.Entry, bound);
    }
    catch (Exception exception) when
    (
      exception is ArgumentException or JsonException or NotSupportedException or InvalidOperationException
    )
    {
      return Error(name, exception.Message);
    }

    if (tool.RequiresApproval)
    {
      if (!Gate.TryBegin(out Guid callId, out Task<bool> decision))
      {
        return Error(name, BusyError);
      }

      (bool approved, bool navigated) = await WaitForApprovalAsync(callId, decision, tool.Name, rendered);
      if (navigated || !IsOnPath(path))
      {
        return Error(name, PageChangedError);
      }

      if (!approved)
      {
        return Serialize(new WebMcpRejected(tool.Name, Approved: false));
      }

      tool = await FindOfferedAsync(path, name);
      if (tool is null)
      {
        return Error(name, UnavailableError);
      }

      // The permission check awaited; navigation may have happened after the listener was removed.
      if (!IsOnPath(path))
      {
        return Error(name, PageChangedError);
      }
    }

    string? denial = await CredentialDenialAsync(tool.Entry.Permissions);
    if (denial is not null)
    {
      return Error(name, denial);
    }

    AgentCallOutcome.Clear();
    await tool.Entry.Execute(Store, bound, CancellationToken.None);
    return Serialize(new WebMcpCompleted(tool.Name, Completed: true, AgentCallOutcome.Result));
  }

  private bool IsOnPath(string path) =>
    string.Equals(Route.PathOr(Navigation), path, StringComparison.OrdinalIgnoreCase);

  private async Task<(bool Approved, bool Navigated)> WaitForApprovalAsync
  (
    Guid callId,
    Task<bool> decision,
    string toolName,
    string rendered
  )
  {
    bool navigated = false;
    void Cancel(object? _, LocationChangedEventArgs __)
    {
      navigated = true;
      Gate.Complete(callId, approved: false);
    }

    Navigation.LocationChanged += Cancel;
    try
    {
      AgentSurfaceState surface = Store.GetState<AgentSurfaceState>();
      await surface.ShowApproval(callId, toolName, rendered);
      bool approved = await decision;
      return (approved, navigated);
    }
    finally
    {
      Navigation.LocationChanged -= Cancel;
      Gate.Complete(callId, approved: false);
      await Store.GetState<AgentSurfaceState>().ResolveApproval(callId, approved: false);
    }
  }

  private async Task<CatalogAgentTool?> FindOfferedAsync(string path, string name)
  {
    // Re-read on every call. The dispatcher does not cache an AuthenticationState.
#pragma warning disable BL0013
    AuthenticationState authentication = await AuthenticationStateProvider.GetAuthenticationStateAsync();
#pragma warning restore BL0013
    return await CatalogAgentToolSet.FindOfferedAsync
    (
      authentication.User,
      AuthorizationService,
      Catalog.Entries,
      path,
      name,
      Store.GetState<AgentSurfaceState>().EditMode,
      CancellationToken.None
    );
  }

  private async Task<string?> CredentialDenialAsync(IEnumerable<string> requiredPermissions)
  {
    // Re-read on every call. The dispatcher does not cache an AuthenticationState.
#pragma warning disable BL0013
    AuthenticationState authentication = await AuthenticationStateProvider.GetAuthenticationStateAsync();
#pragma warning restore BL0013
    return AgentConversationAuthority.Denial(
      Store.GetState<AgentSurfaceState>().Conversation,
      authentication.User,
      requiredPermissions);
  }

  private static Dictionary<string, object?> ParseArguments(string? argumentsJson)
  {
    if (string.IsNullOrWhiteSpace(argumentsJson))
    {
      return [];
    }

    Dictionary<string, JsonElement>? parsed = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>
    (
      argumentsJson,
      ContractSerializationDefaults.Options
    );
    Dictionary<string, object?> arguments = [];
    if (parsed is null)
    {
      return arguments;
    }

    foreach (KeyValuePair<string, JsonElement> pair in parsed)
    {
      arguments[pair.Key] = pair.Value;
    }

    return arguments;
  }

  private static string Serialize<T>(T result) =>
    JsonSerializer.Serialize(result, ContractSerializationDefaults.Options);

  private static string Error(string action, string error) => Serialize(new WebMcpFailed(action, error));

  private sealed record WebMcpCompleted(
    string Action,
    bool Completed,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] object? Result = null);

  private sealed record WebMcpRejected(string Action, bool Approved);

  private sealed record WebMcpFailed(string Action, string Error);
}

#region Purpose
// Runs a WebMCP tool call through the same page, permission, approval, and store path as the ask UI.
#endregion

#region Design
// The browser agent is not trusted. InvokeTool selects the tools again for the current principal
// and route, so a stale registration cannot call an action the person can no longer run.
// page_context is not a catalog action; it returns the page facts and does not ask for approval.
// Arguments are parsed and bound before anything is shown: a call that cannot bind returns
// {action, error} without a prompt, and the banner shows the canonical rendering of the bound
// values (CatalogAgentArguments.Render). Execute receives exactly that bound array.
// Mutating tools take the single WebMcpApprovalGate slot. A second mutating call while one waits
// is refused with an error instead of replacing the call on screen. While waiting, the dispatcher
// listens to LocationChanged and cancels its own call on any navigation, which also covers
// focused pages that do not host the banner. After the wait it clears its banner (ResolveApproval
// with its own id is a no-op if already answered), then refuses unless the path is still the path
// at call time and a fresh selection for the current principal still offers the same tool.
// Result JSON uses the contract seam options. Execute failures are not mapped: handlers report
// their own outcomes on NotificationState.
#endregion

namespace TimeWarp.Architecture.Features.Applications;

using Microsoft.AspNetCore.Components.Routing;

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
  private readonly WebMcpApprovalGate Gate;

  public WebMcpDispatcher
  (
    IStore store,
    IActionCatalog catalog,
    IAuthorizationService authorizationService,
    AuthenticationStateProvider authenticationStateProvider,
    NavigationManager navigation,
    WebMcpApprovalGate gate
  )
  {
    Store = store;
    Catalog = catalog;
    AuthorizationService = authorizationService;
    AuthenticationStateProvider = authenticationStateProvider;
    Navigation = navigation;
    Gate = gate;
  }

  [JSInvokable]
  public async Task<string> InvokeTool(string name, string? argumentsJson)
  {
    ArgumentException.ThrowIfNullOrEmpty(name);
    string path = PageAgentScope.FromNavigation(Navigation);
    if (string.Equals(name, PageAgentContext.ToolName, StringComparison.Ordinal))
    {
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

      bool approved = await WaitForApprovalAsync(callId, decision, tool.Name, rendered);
      if (!string.Equals(PageAgentScope.FromNavigation(Navigation), path, StringComparison.OrdinalIgnoreCase))
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
    }

    await tool.Entry.Execute(Store, bound, CancellationToken.None);
    return Serialize(new WebMcpCompleted(tool.Name, Completed: true));
  }

  private async Task<bool> WaitForApprovalAsync(Guid callId, Task<bool> decision, string toolName, string rendered)
  {
    void Cancel(object? _, LocationChangedEventArgs __) => Gate.Complete(callId, approved: false);

    Navigation.LocationChanged += Cancel;
    try
    {
      AgentSurfaceState surface = Store.GetState<AgentSurfaceState>();
      await surface.ShowApproval(callId, toolName, rendered);
      return await decision;
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
      CancellationToken.None
    );
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

  private sealed record WebMcpCompleted(string Action, bool Completed);

  private sealed record WebMcpRejected(string Action, bool Approved);

  private sealed record WebMcpFailed(string Action, string Error);
}

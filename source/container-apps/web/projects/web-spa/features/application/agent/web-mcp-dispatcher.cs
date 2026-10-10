#region Purpose
// Runs a WebMCP tool call through the same page, permission, approval, and store path as the ask UI.
#endregion

#region Design
// The browser agent is not trusted. InvokeTool selects the tools again for the current principal
// and route, so a stale registration cannot call an action the person can no longer run.
// page_context and navigate are not catalog actions. page_context walks the page body again
// (PageSurfaceJsModule) so the text matches the page on screen when the tool is called, returns
// the page facts, and does not ask for approval. navigate rebuilds the palette and calls RouteState.ChangeRoute, and it
// does not ask for approval. A page-bound palette tool invoked off its page returns the navigate
// offer and does not bind or execute. A page-only tool is refused off its page.
// The route is the shell's PageAgentRoute when one has been observed.
// Arguments are parsed and bound before anything is shown: a call that cannot bind returns
// {action, error} without a prompt, and the banner shows the canonical rendering of the bound
// values (CatalogAgentArguments.Render). Execute receives exactly that bound array.
// Mutating tools take the single WebMcpApprovalGate slot. A second mutating call while one waits
// is refused with an error instead of replacing the call on screen. While a page-bound call waits,
// the dispatcher listens to LocationChanged and cancels that call, which also covers focused pages
// that do not host the banner. A tool that is not page-bound keeps waiting across navigation.
// After the wait it clears its banner (ResolveApproval with its own id is a no-op if already
// answered). A page-bound tool then refuses with PageChangedError if any navigation happened
// while waiting (even one that returns to the same path, such as a query-only change), and it
// re-selects on the path the call started on. The page-changed refusal is distinct from a person
// rejecting the prompt.
// A tool that is not page-bound is not refused only because the path changed; it re-selects on
// the path now showing. Only page-bound tools carry an OffPageRoute, so it cannot become an offer.
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
  private readonly IJSRuntime JsRuntime;

  public WebMcpDispatcher
  (
    IStore store,
    IActionCatalog catalog,
    IAuthorizationService authorizationService,
    AuthenticationStateProvider authenticationStateProvider,
    NavigationManager navigation,
    PageAgentRoute route,
    WebMcpApprovalGate gate,
    AgentCallOutcome agentCallOutcome,
    IJSRuntime jsRuntime
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
    JsRuntime = jsRuntime;
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

      IReadOnlyList<string> offered = await OfferedNamesAsync(path);
      string? surface = await PageSurfaceJsModule.TrySummarizeAsync(JsRuntime, CancellationToken.None);
      return PageAgentContext.Describe(Store, path, offered, surface, PageAgentContext.DocumentCap);
    }

    if (string.Equals(name, AgentNavigate.ToolName, StringComparison.Ordinal))
    {
      AuthenticationState authentication = await AuthenticationAsync();
      AgentNavigate.Result navigated = await AgentNavigate.InvokeAsync
      (
        authentication.User,
        AuthorizationService,
        Catalog.Entries,
        Store,
        path,
        ParseArguments(argumentsJson),
        CancellationToken.None
      );
      return Serialize(navigated);
    }

    CatalogAgentTool? tool = await FindOfferedAsync(path, name);
    if (tool is null)
    {
      return Error(name, UnavailableError);
    }

    if (tool.OffPageRoute is not null)
    {
      if (tool.Entry is null)
      {
        return Error(name, UnavailableError);
      }

      string? offerDenial = await CredentialDenialAsync(tool.Entry.Permissions);
      if (offerDenial is not null)
      {
        return Error(name, offerDenial);
      }

      return Serialize(AgentNavigate.OfferFor(tool.Entry, tool.OffPageRoute));
    }

    if (tool.Entry is null)
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

      bool pageBound = PageAgentScope.PrimaryPage(tool.Name) is not null;
      (bool approved, bool navigated) = await WaitForApprovalAsync
      (
        callId,
        decision,
        tool.Name,
        rendered,
        pageBound
      );
      if (pageBound && (navigated || !IsOnPath(path)))
      {
        return Error(name, PageChangedError);
      }

      if (!approved)
      {
        return Serialize(new WebMcpRejected(tool.Name, Approved: false));
      }

      if (pageBound)
      {
        tool = await FindOfferedAsync(path, name);
        if (tool?.Entry is null || tool.OffPageRoute is not null)
        {
          return Error(name, UnavailableError);
        }

        // The permission check awaited; navigation may have happened after the listener was removed.
        if (!IsOnPath(path))
        {
          return Error(name, PageChangedError);
        }
      }
      else
      {
        string currentPath = Route.PathOr(Navigation);
        tool = await FindOfferedAsync(currentPath, name);
        if (tool?.Entry is null)
        {
          return Error(name, UnavailableError);
        }
      }

      try
      {
        bound = CatalogAgentArguments.Bind(tool.Entry, ParseArguments(argumentsJson));
      }
      catch (Exception exception) when
      (
        exception is ArgumentException or JsonException or NotSupportedException or InvalidOperationException
      )
      {
        return Error(name, exception.Message);
      }
    }

    if (tool.Entry is null)
    {
      return Error(name, UnavailableError);
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

  private async Task<IReadOnlyList<string>> OfferedNamesAsync(string path)
  {
    AuthenticationState authentication = await AuthenticationAsync();
    IReadOnlyList<CatalogAgentTool> selected = await CatalogAgentToolSet.SelectAsync
    (
      authentication.User,
      AuthorizationService,
      Catalog.Entries,
      path,
      Store.GetState<AgentSurfaceState>().EditMode,
      CancellationToken.None
    );
    List<string> names = [];
    foreach (CatalogAgentTool tool in selected)
    {
      names.Add(tool.Name);
    }

    names.Add(PageAgentContext.ToolName);
    return names;
  }

  private async Task<AuthenticationState> AuthenticationAsync()
  {
    // Re-read on every call. The dispatcher does not cache an AuthenticationState.
#pragma warning disable BL0013
    return await AuthenticationStateProvider.GetAuthenticationStateAsync();
#pragma warning restore BL0013
  }

  private bool IsOnPath(string path) =>
    string.Equals(Route.PathOr(Navigation), path, StringComparison.OrdinalIgnoreCase);

  private async Task<(bool Approved, bool Navigated)> WaitForApprovalAsync
  (
    Guid callId,
    Task<bool> decision,
    string toolName,
    string rendered,
    bool pageBound
  )
  {
    bool navigated = false;
    void Cancel(object? _, LocationChangedEventArgs __)
    {
      if (!pageBound)
      {
        return;
      }

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
    AuthenticationState authentication = await AuthenticationAsync();
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
    AuthenticationState authentication = await AuthenticationAsync();
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

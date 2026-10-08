#region Purpose
// Runs a WebMCP tool call through the same page, permission, approval, and store path as the ask UI.
#endregion

#region Design
// The browser agent is not trusted. InvokeTool selects the tools again for the current principal
// and route, so a stale registration cannot call an action the person can no longer run.
// page_context is not a catalog action; it returns the page facts and does not ask for approval.
// Mutating catalog tools wait on the in-app confirmation bar before Execute.
#endregion

namespace TimeWarp.Architecture.Features.Applications;

/// <summary>JS entry point for WebMCP tool execution.</summary>
[SideEffectService]
public sealed class WebMcpDispatcher
{
  private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

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

    // Re-read on every call. The dispatcher does not cache an AuthenticationState.
#pragma warning disable BL0013
    AuthenticationState authentication = await AuthenticationStateProvider.GetAuthenticationStateAsync();
#pragma warning restore BL0013
    IReadOnlyList<CatalogAgentTool> tools = await CatalogAgentToolSet.SelectAsync
    (
      authentication.User,
      AuthorizationService,
      Catalog.Entries,
      path,
      CancellationToken.None
    );
    CatalogAgentTool? tool = null;
    foreach (CatalogAgentTool candidate in tools)
    {
      if (string.Equals(candidate.Name, name, StringComparison.Ordinal))
      {
        tool = candidate;
        break;
      }
    }

    if (tool is null)
    {
      return Error(name, "The action is not available on this page.");
    }

    Dictionary<string, object?> arguments;
    try
    {
      arguments = ParseArguments(argumentsJson);
      if (tool.RequiresApproval)
      {
        Gate.Arm();
        await Store.GetState<AgentSurfaceState>().ShowApproval(tool.Name, argumentsJson ?? "{}");
        bool approved = await Gate.WaitAsync(CancellationToken.None);
        if (!approved)
        {
          return JsonSerializer.Serialize(new WebMcpRejected(tool.Name, Approved: false), SerializerOptions);
        }
      }

      object?[] bound = CatalogAgentArguments.Bind(tool.Entry, arguments);
      await tool.Entry.Execute(Store, bound, CancellationToken.None);
      return JsonSerializer.Serialize(new WebMcpCompleted(tool.Name, Completed: true), SerializerOptions);
    }
    catch (ArgumentException exception)
    {
      return Error(name, exception.Message);
    }
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
      SerializerOptions
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

  private static string Error(string action, string error) =>
    JsonSerializer.Serialize(new WebMcpFailed(action, error), SerializerOptions);

  private sealed record WebMcpCompleted(string Action, bool Completed);

  private sealed record WebMcpRejected(string Action, bool Approved);

  private sealed record WebMcpFailed(string Action, string Error);
}

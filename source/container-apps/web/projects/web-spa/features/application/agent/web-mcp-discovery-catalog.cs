#region Purpose
// The generated WebMCP discovery document and the page-load navigate payload.
#endregion

#region Design
// Backend MCP (the server card) and frontend WebMCP are separate transports. This type describes
// only the browser transport. The card stays a stub; capabilities.tools is not set here.
// Anonymous destinations are AgentSiteMap for the caller-supplied principal, the same list
// navigate's enum uses. Route-scoped tools are PageRegistry.ActionScopes: one row per route
// template, tool names derived from the action type (owner state + action set) so the document
// is not a second hand-written list. CatalogName is checked against IActionCatalog entry names.
// Page-load JSON is AgentNavigate.Create for that principal in Ask before editing, so the early
// tool is the real navigate tool and the person approves. '<' is escaped so the JSON can sit in
// an HTML script element. No page route literal belongs in this file; the guard test rejects one.
#endregion

namespace TimeWarp.Architecture.Features.Applications;

using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

/// <summary>WebMCP discovery JSON and the HTML page-load navigate payload.</summary>
public static class WebMcpDiscoveryCatalog
{
  public const string DocumentPath = "/.well-known/webmcp.json";

  public const string CardPath = "/.well-known/mcp/server-card.json";

  public const string Transport = "webmcp";

  public const string FeatureDetect = "typeof navigator.modelContext";

  public const string ChromeFlag = "#enable-webmcp-testing";

  public const string Polyfill = "MCP-B";

  public const string Trace = "Copy trace";

  public const string BackendNote =
    "Backend MCP is a separate transport. The server card is a stub and capabilities.tools is false. "
    + "Do not call that endpoint expecting tools/list.";

  private static readonly JsonSerializerOptions DocumentOptions = new(JsonSerializerDefaults.Web)
  {
    WriteIndented = true,
  };

  /// <summary>Browser tool name for a catalog action type: owner without State, set without ActionSet.</summary>
  public static string CatalogName(Type actionType)
  {
    ArgumentNullException.ThrowIfNull(actionType);
    Type set = actionType.DeclaringType ?? actionType;
    Type owner = set.DeclaringType ?? set;
    const string stateSuffix = "State";
    const string setSuffix = "ActionSet";
    string ownerName = owner.Name.EndsWith(stateSuffix, StringComparison.Ordinal)
      ? owner.Name[..^stateSuffix.Length]
      : owner.Name;
    string actionName = set.Name.EndsWith(setSuffix, StringComparison.Ordinal)
      ? set.Name[..^setSuffix.Length]
      : set.Name;
    return ownerName + "." + actionName;
  }

  /// <summary>Anonymous-or-caller WebMCP document. Route rows come from <see cref="PageRegistry.ActionScopes"/>.</summary>
  public static async Task<string> DocumentJsonAsync
  (
    ClaimsPrincipal user,
    IAuthorizationService authorizationService,
    string? path
  )
  {
    ArgumentNullException.ThrowIfNull(user);
    ArgumentNullException.ThrowIfNull(authorizationService);
    IReadOnlyList<AgentSiteMap.Destination> destinations =
      await AgentSiteMap.DestinationsAsync(user, authorizationService, path);
    List<WebMcpRouteTools> routes = [];
    foreach (PageActionScope scope in PageRegistry.ActionScopes)
    {
      string[] tools = [.. scope.ActionTypes.Select(CatalogName)];
      foreach (string routeTemplate in scope.RouteTemplates)
      {
        routes.Add(new WebMcpRouteTools(routeTemplate, tools));
      }
    }

    WebMcpDiscoveryDocument document = new
    (
      Transport,
      new WebMcpBackendCard(CardPath, CapabilitiesTools: false, BackendNote),
      ["document.modelContext", "navigator.modelContext"],
      AgentNavigate.ToolName,
      [AgentNavigate.ToolName, AgentSiteMap.ToolName, PageAgentContext.ToolName],
      destinations,
      routes,
      new WebMcpInspector(FeatureDetect, ChromeFlag, Polyfill, Trace)
    );
    return JsonSerializer.Serialize(document, DocumentOptions);
  }

  /// <summary>navigate payload embedded in the host page. Ask before editing, so the person approves.</summary>
  public static async Task<string> PageLoadJsonAsync
  (
    ClaimsPrincipal user,
    IAuthorizationService authorizationService,
    string? path
  )
  {
    ArgumentNullException.ThrowIfNull(user);
    ArgumentNullException.ThrowIfNull(authorizationService);
    IReadOnlyList<AgentSiteMap.Destination> destinations =
      await AgentSiteMap.DestinationsAsync(user, authorizationService, path);
    CatalogAgentTool tool = AgentNavigate.Create(destinations, AgentEditMode.AskBeforeEditing);
    var schema = JsonNode.Parse(tool.InputSchema);
    JsonObject payload = new()
    {
      ["name"] = tool.Name,
      ["description"] = tool.Description,
      ["inputSchema"] = schema,
      ["requiresApproval"] = tool.RequiresApproval,
      ["refusal"] = AgentNavigate.Refusal,
    };
    return payload.ToJsonString().Replace("<", "\\u003c", StringComparison.Ordinal);
  }

  /// <summary>One generated route row.</summary>
  public sealed record WebMcpRouteTools(
    [property: JsonPropertyName("routeTemplate")] string RouteTemplate,
    [property: JsonPropertyName("tools")] IReadOnlyList<string> Tools);

  /// <summary>The well-known WebMCP document.</summary>
  public sealed record WebMcpDiscoveryDocument(
    [property: JsonPropertyName("transport")] string Transport,
    [property: JsonPropertyName("backendMcp")] WebMcpBackendCard BackendMcp,
    [property: JsonPropertyName("entryPoints")] IReadOnlyList<string> EntryPoints,
    [property: JsonPropertyName("pageLoadTool")] string PageLoadTool,
    [property: JsonPropertyName("everyPageTools")] IReadOnlyList<string> EveryPageTools,
    [property: JsonPropertyName("anonymousDestinations")] IReadOnlyList<AgentSiteMap.Destination> AnonymousDestinations,
    [property: JsonPropertyName("routeScopedTools")] IReadOnlyList<WebMcpRouteTools> RouteScopedTools,
    [property: JsonPropertyName("inspector")] WebMcpInspector Inspector);

  /// <summary>Pointer at the backend MCP card. capabilities.tools stays false.</summary>
  public sealed record WebMcpBackendCard(
    [property: JsonPropertyName("card")] string Card,
    [property: JsonPropertyName("capabilitiesTools")] bool CapabilitiesTools,
    [property: JsonPropertyName("note")] string Note);

  /// <summary>How a person inspects WebMCP in a browser.</summary>
  public sealed record WebMcpInspector(
    [property: JsonPropertyName("featureDetect")] string FeatureDetect,
    [property: JsonPropertyName("chromeFlag")] string ChromeFlag,
    [property: JsonPropertyName("polyfill")] string Polyfill,
    [property: JsonPropertyName("trace")] string Trace);
}

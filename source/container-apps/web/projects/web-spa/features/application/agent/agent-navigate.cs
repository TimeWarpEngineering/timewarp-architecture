#region Purpose
// One navigate tool whose destinations are the navigation menu's pages the principal may open,
// plus the deep links Ctrl-K lists under them.
#endregion

#region Design
// Destinations are AgentSiteMap.DestinationsAsync: NavigationDestinations (the policy-filtered
// PageRegistry that NavMenu and Ctrl-K read) plus the palette's Sign in row while signed out
// (task 300) and the palette's deep links (task 446, the design-system parts). site_map lists the
// same rows, so every site_map URL is a navigate target and nothing else is. A single tool with a
// JSON-schema enum of permitted URLs stays short. One tool per destination was rejected: it would
// duplicate every menu row. The description names each page, then one AgentSiteMap.DeepLinkLines
// line per parent, so 30+ parts stay under the contract's tool-description cap; the enum still
// lists every url.
// The enum is a hint. Invoke rebuilds the destinations for the principal on the current path and
// refuses any url that is not one of them, then calls RouteState.ChangeRoute, the palette's path.
// Approval follows the edit mode (task 300): in Ask before editing the tool is approval-wrapped
// (Ask) or takes the approval gate (WebMCP), in Automatically edit it runs at once. The result
// carries the page title, so the answer can link the page. After it runs, the shell's
// rebuild-on-path-change picks up the new page's tools. A page-bound catalog action invoked off
// its page does not run: OfferFor tells the agent to call navigate.
#endregion

namespace TimeWarp.Architecture.Features.Applications;

using System.Text;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

/// <summary>Menu destinations as one agent tool.</summary>
public static class AgentNavigate
{
  public const string ToolName = "navigate";

  public const string Refusal = "That page is not available.";

  public const string UrlRequired = "url is required.";

  /// <summary>A page-bound action invoked off its page. <see cref="Executed"/> is false.</summary>
  public sealed record Offer(
    [property: JsonPropertyName("executed")] bool Executed,
    [property: JsonPropertyName("navigateTo")] string NavigateTo,
    [property: JsonPropertyName("message")] string Message);

  /// <summary>Result of the navigate tool.</summary>
  public sealed record Result(
    [property: JsonPropertyName("navigated")] bool Navigated,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [property: JsonPropertyName("url")] string? Destination,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [property: JsonPropertyName("error")] string? Error,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [property: JsonPropertyName("title")] string? Title = null);

  public static CatalogAgentTool Create(IReadOnlyList<AgentSiteMap.Destination> destinations, AgentEditMode editMode)
  {
    ArgumentNullException.ThrowIfNull(destinations);
    bool approval = editMode == AgentEditMode.AskBeforeEditing;
    JsonArray urls = [];
    HashSet<string> seen = [];
    StringBuilder described = new("Go to a page this person may open (the site_map destinations). ");
    described.Append("Pass url as one permitted destination. ");
    described.Append(approval
      ? "The person approves the move first. "
      : "It runs without a prompt in this conversation. ");
    described.Append("Destinations: ");
    bool first = true;
    foreach (AgentSiteMap.Destination destination in destinations)
    {
      if (!seen.Add(destination.Route))
      {
        continue;
      }

      urls.Add(destination.Route);
      if (destination.Parent is not null)
      {
        continue;
      }

      if (!first)
      {
        described.Append("; ");
      }

      first = false;
      described.Append(destination.Route).Append(" (").Append(destination.Title).Append(')');
    }

    foreach (string line in AgentSiteMap.DeepLinkLines(destinations))
    {
      described.Append("; ").Append(line);
    }

    JsonObject schema = new()
    {
      ["type"] = "object",
      ["properties"] = new JsonObject
      {
        ["url"] = new JsonObject
        {
          ["type"] = "string",
          ["enum"] = urls,
          ["description"] = "A destination URL from this tool's description.",
        },
      },
      ["required"] = new JsonArray("url"),
      ["additionalProperties"] = false,
    };

    return new CatalogAgentTool
    (
      ToolName,
      described.ToString(),
      schema.ToJsonString(),
      approval,
      Entry: null
    );
  }

  public static Offer OfferFor(ActionCatalogEntry entry, string pageRoute)
  {
    ArgumentNullException.ThrowIfNull(entry);
    ArgumentException.ThrowIfNullOrWhiteSpace(pageRoute);
    string labeled = CommandPaletteRoster.Label(entry);
    int split = labeled.IndexOf(": ", StringComparison.Ordinal);
    string action = split < 0 ? labeled : labeled[(split + 2)..];
    string page = PageTitle(pageRoute);
    return new Offer(
      Executed: false,
      pageRoute,
      $"{action} is on {page}; I can take you to the {page} page.");
  }

  public static async Task<Result> InvokeAsync
  (
    ClaimsPrincipal user,
    IAuthorizationService authorizationService,
    IStore store,
    string? path,
    IReadOnlyDictionary<string, object?>? arguments,
    CancellationToken cancellationToken
  )
  {
    ArgumentNullException.ThrowIfNull(user);
    ArgumentNullException.ThrowIfNull(authorizationService);
    ArgumentNullException.ThrowIfNull(store);
    cancellationToken.ThrowIfCancellationRequested();

    string? denial = AgentConversationAuthority.Denial(
      store.GetState<AgentSurfaceState>().Conversation,
      user,
      []);
    if (denial is not null)
    {
      return new Result(Navigated: false, Destination: null, denial);
    }

    string? destination = ReadDestination(arguments);
    if (string.IsNullOrWhiteSpace(destination))
    {
      return new Result(Navigated: false, Destination: null, UrlRequired);
    }

    AgentSiteMap.Destination? permitted = await FindAsync(user, authorizationService, path, destination);
    if (permitted is null)
    {
      return new Result(Navigated: false, Destination: null, Refusal);
    }

    await store.GetState<RouteState>().ChangeRoute(destination, externalCancellationToken: cancellationToken);
    return new Result(Navigated: true, destination, Error: null, permitted.Title);
  }

  /// <summary>The permitted destination whose url is <paramref name="route"/>, or null.</summary>
  public static async Task<AgentSiteMap.Destination?> FindAsync
  (
    ClaimsPrincipal user,
    IAuthorizationService authorizationService,
    string? path,
    string route
  )
  {
    foreach (AgentSiteMap.Destination candidate in await AgentSiteMap.DestinationsAsync(user, authorizationService, path))
    {
      if (string.Equals(candidate.Route, route, StringComparison.Ordinal))
      {
        return candidate;
      }
    }

    return null;
  }

  public static string? ReadDestination(IReadOnlyDictionary<string, object?>? arguments)
  {
    if (arguments is null)
    {
      return null;
    }

    if (!arguments.TryGetValue("url", out object? value) && !arguments.TryGetValue("Url", out value))
    {
      return null;
    }

    if (value is JsonElement element)
    {
      return element.ValueKind == JsonValueKind.String ? element.GetString() : null;
    }

    return value as string;
  }

  private static string PageTitle(string pageRoute)
  {
    foreach (PageRegistryEntry entry in PageRegistry.All)
    {
      if (string.Equals(entry.Url, pageRoute, StringComparison.OrdinalIgnoreCase))
      {
        return entry.Title;
      }
    }

    return pageRoute;
  }
}

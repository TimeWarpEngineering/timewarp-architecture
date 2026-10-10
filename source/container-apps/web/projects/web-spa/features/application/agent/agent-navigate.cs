#region Purpose
// One navigate tool whose destinations are the Ctrl-K page rows the principal may open.
#endregion

#region Design
// A single tool with a JSON-schema enum of permitted URLs stays short and maps 1:1 to palette
// Page rows, including the signed-out Sign in row. One tool per destination was rejected: it
// would duplicate every page row and drift from the palette's ordering.
// The enum is a hint. Invoke rebuilds CommandPaletteRoster and refuses any url that is not a
// Page-kind target for this principal, then calls RouteState.ChangeRoute, the palette's path.
// Navigation is not an edit. RequiresApproval is false in both edit modes; the palette's page
// rows also run without a confirm step. A page-bound catalog action that is invoked off its
// page does not run: OfferFor tells the agent to call navigate.
// Anonymous principals get the palette's anonymous pages (Home) plus Sign in. Signed-in
// principals get every page row the palette would show them, and no Sign in row.
#endregion

namespace TimeWarp.Architecture.Features.Applications;

using System.Text;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

/// <summary>Palette page rows as one agent tool.</summary>
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
    [property: JsonPropertyName("error")] string? Error);

  public static CatalogAgentTool Create(IReadOnlyList<CommandPaletteRow> pageRows)
  {
    ArgumentNullException.ThrowIfNull(pageRows);
    JsonArray urls = [];
    HashSet<string> seen = [];
    StringBuilder described = new("Go to a page this person may open. ");
    described.Append("Pass url as one permitted destination. ");
    described.Append("Navigation is not an edit and does not ask for approval. Destinations: ");
    bool first = true;
    foreach (CommandPaletteRow row in pageRows)
    {
      if (row.Kind != CommandPaletteRowKind.Page || !seen.Add(row.Target))
      {
        continue;
      }

      urls.Add(row.Target);
      if (!first)
      {
        described.Append("; ");
      }

      first = false;
      described.Append(row.Target).Append(" (").Append(row.Name).Append(')');
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
      RequiresApproval: false,
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
    IEnumerable<ActionCatalogEntry> entries,
    IStore store,
    string? path,
    IReadOnlyDictionary<string, object?>? arguments,
    CancellationToken cancellationToken
  )
  {
    ArgumentNullException.ThrowIfNull(user);
    ArgumentNullException.ThrowIfNull(authorizationService);
    ArgumentNullException.ThrowIfNull(entries);
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

    string current = PageAgentScope.Normalize(path);
    IReadOnlyList<CommandPaletteRow> rows = await CommandPaletteRoster.BuildAsync
    (
      user,
      authorizationService,
      PageRegistry.All,
      entries,
      current
    );
    bool permitted = false;
    foreach (CommandPaletteRow row in rows)
    {
      if (row.Kind == CommandPaletteRowKind.Page && string.Equals(row.Target, destination, StringComparison.Ordinal))
      {
        permitted = true;
        break;
      }
    }

    if (!permitted)
    {
      return new Result(Navigated: false, Destination: null, Refusal);
    }

    await store.GetState<RouteState>().ChangeRoute(destination, externalCancellationToken: cancellationToken);
    return new Result(Navigated: true, destination, Error: null);
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

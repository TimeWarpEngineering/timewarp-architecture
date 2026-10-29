#region Purpose
// The read-only site_map agent tool: every destination the principal's navigation menu shows,
// plus the deep links Ctrl-K lists under those pages.
#endregion

#region Design
// Task 300: Ask and WebMCP know the whole app, not only the page on screen. Destinations come
// from NavigationDestinations, the policy-filtered PageRegistry that NavMenu and Ctrl-K read, so
// site_map lists exactly the menu's pages for this principal, in menu order, plus the palette's
// Sign in row while signed out. Each row is the [Page] declaration's title, URL, NavGroup, icon
// name and Description; nothing about a page is written anywhere else. Task 446: the palette's
// deep links (CommandPaletteRoster.DeepLinks, the design-system parts) follow the pages, in the
// palette's order, each with its parent page url, so Ask finds what Ctrl-K finds. A part is only
// listed when its page is. navigate's enum is the same list (Destinations), so a site_map URL is
// always a navigate target and nothing else is.
// site_map takes no arguments, changes nothing, and is never approval-wrapped, like page_context.
// It is offered on every page, signed in or out. The conversation credential still applies: a
// stale or foreign credential gets an error instead of the list.
// Summary is the compact text Ask inlines in its instructions, bounded by a length cap. Deep
// links collapse to one line per parent (DeepLinkLines: the url pattern, then each part's last
// segment), so 30+ parts cost a few hundred characters; navigate's description uses the same
// lines. Summary drops descriptions, then rows, before it exceeds the cap.
#endregion

namespace TimeWarp.Architecture.Features.Applications;

using System.Text;
using System.Text.Json.Serialization;

/// <summary>The site_map tool and the destination list navigate shares.</summary>
public static class AgentSiteMap
{
  public const string ToolName = "site_map";

  public const string ToolDescription =
    "Every page this person may open from the app's navigation menu, plus deep links into a page "
    + "(parent is the page's url): title, url, menu group, icon, and what the page is for. Read-only. "
    + "Use it to answer where something is, then link the page or call navigate with its url.";

  /// <summary>One permitted destination.</summary>
  public sealed record Destination(
    [property: JsonPropertyName("title")] string Title,
    [property: JsonPropertyName("url")] string Route,
    [property: JsonPropertyName("group")] string Group,
    [property: JsonPropertyName("icon")] string? Icon,
    [property: JsonPropertyName("description")] string Description,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [property: JsonPropertyName("parent")] string? Parent = null);

  /// <summary>Result of the site_map tool.</summary>
  public sealed record Result(
    [property: JsonPropertyName("current")] string Current,
    [property: JsonPropertyName("destinations")] IReadOnlyList<Destination> Destinations,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [property: JsonPropertyName("error")] string? Error = null);

  /// <summary>The menu's destinations for <paramref name="user"/>, with Sign in first while signed out and deep links last.</summary>
  public static async Task<IReadOnlyList<Destination>> DestinationsAsync
  (
    ClaimsPrincipal user,
    IAuthorizationService authorizationService,
    string? path
  )
  {
    ArgumentNullException.ThrowIfNull(user);
    ArgumentNullException.ThrowIfNull(authorizationService);
    List<Destination> destinations = [];
    if (user.Identity?.IsAuthenticated != true)
    {
      CommandPaletteRow signIn = CommandPaletteRoster.SignInRow(PageAgentScope.Normalize(path));
      destinations.Add(new Destination(signIn.Name, signIn.Target, "", Icon: null, signIn.Description));
    }

    List<Destination> deepLinks = [];
    foreach (PageRegistryEntry page in await NavigationDestinations.ForAsync(user, authorizationService))
    {
      destinations.Add(new Destination(page.Title, page.Url, page.NavGroup, page.NavIcon?.Name, page.Description));
      foreach (CommandPaletteRoster.PageDeepLink link in CommandPaletteRoster.DeepLinks(page))
      {
        deepLinks.Add(new Destination(link.Title, link.Route, page.NavGroup, Icon: null, link.Description, page.Url));
      }
    }

    destinations.AddRange(deepLinks);
    return destinations;
  }

  public static CatalogAgentTool Create() =>
    new
    (
      ToolName,
      ToolDescription,
      PageAgentContext.EmptyInputSchema,
      RequiresApproval: false,
      Entry: null
    );

  public static async Task<Result> InvokeAsync
  (
    ClaimsPrincipal user,
    IAuthorizationService authorizationService,
    IStore store,
    string? path,
    CancellationToken cancellationToken
  )
  {
    ArgumentNullException.ThrowIfNull(user);
    ArgumentNullException.ThrowIfNull(store);
    cancellationToken.ThrowIfCancellationRequested();
    string current = PageAgentScope.Normalize(path);
    string? denial = AgentConversationAuthority.Denial(
      store.GetState<AgentSurfaceState>().Conversation,
      user,
      []);
    if (denial is not null)
    {
      return new Result(current, [], denial);
    }

    return new Result(current, await DestinationsAsync(user, authorizationService, current));
  }

  /// <summary>
  /// One line per page ("Title: url [Group] - description") and one per parent's deep links, at most
  /// <paramref name="maxLength"/> characters.
  /// </summary>
  public static string Summary(IReadOnlyList<Destination> destinations, int maxLength)
  {
    ArgumentNullException.ThrowIfNull(destinations);
    ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxLength);
    string full = Render(destinations, withDescriptions: true);
    if (full.Length <= maxLength)
    {
      return full;
    }

    string brief = Render(destinations, withDescriptions: false);
    if (brief.Length <= maxLength)
    {
      return brief;
    }

    int cut = brief.AsSpan(0, maxLength).LastIndexOf('\n');
    return cut <= 0 ? "" : brief[..(cut + 1)];
  }

  private static string Render(IReadOnlyList<Destination> destinations, bool withDescriptions)
  {
    StringBuilder text = new();
    foreach (Destination destination in destinations)
    {
      if (destination.Parent is not null)
      {
        continue;
      }

      text.Append("- ").Append(destination.Title).Append(": ").Append(destination.Route);
      if (destination.Group.Length > 0)
      {
        text.Append(" [").Append(destination.Group).Append(']');
      }

      if (withDescriptions && destination.Description.Length > 0)
      {
        text.Append(" - ").Append(destination.Description);
      }

      text.Append('\n');
    }

    foreach (string line in DeepLinkLines(destinations))
    {
      text.Append("- ").Append(line).Append('\n');
    }

    return text.ToString();
  }

  /// <summary>
  /// One line per parent with deep links, in order: "{parent}/&lt;part&gt; for {Title} parts: a, b, c",
  /// where each name is the url's segment after the parent.
  /// </summary>
  public static IEnumerable<string> DeepLinkLines(IReadOnlyList<Destination> destinations)
  {
    ArgumentNullException.ThrowIfNull(destinations);
    List<string> parents = [];
    Dictionary<string, List<string>> partsByParent = new(StringComparer.Ordinal);
    foreach (Destination destination in destinations)
    {
      if (destination.Parent is not { } parent)
      {
        continue;
      }

      if (!partsByParent.TryGetValue(parent, out List<string>? parts))
      {
        parts = [];
        partsByParent[parent] = parts;
        parents.Add(parent);
      }

      string prefix = parent + "/";
      parts.Add(destination.Route.StartsWith(prefix, StringComparison.Ordinal)
        ? destination.Route[prefix.Length..]
        : destination.Route);
    }

    foreach (string parent in parents)
    {
      string title = destinations.FirstOrDefault(destination =>
        destination.Parent is null && string.Equals(destination.Route, parent, StringComparison.Ordinal))?.Title ?? parent;
      yield return $"{parent}/<part> for {title} parts: {string.Join(", ", partsByParent[parent])}";
    }
  }
}

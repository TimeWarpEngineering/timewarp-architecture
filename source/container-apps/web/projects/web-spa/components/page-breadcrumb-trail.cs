#region Purpose
// Location breadcrumb: Home, then ancestor pages, then the current page, from PageRegistry routes.
#endregion

#region Design
// A parent is the registered page whose route is the longest literal prefix of the subject
// route, on a segment boundary. "/" is a prefix of every other route, so Home is the root
// ancestor and a page whose only prefix is Home has no nearer parent. A route equal to the
// subject is not its own parent. Parameterized routes are not parents: a "{" candidate is
// skipped, and a template such as /DesignSystem/{Part} still matches the literal /DesignSystem.
// Nav groups are not routes, so they are not crumbs. The generator already emits the routes
// on PageRegistryEntry; this walk reads that list and does not keep a second parent table.
// Ancestor labels are the registry nav titles. The current crumb uses BreadcrumbLabel when
// the page supplies one, otherwise the registry title on an exact route, otherwise the page
// title (the role name). The current crumb has no URL.
#endregion

namespace TimeWarp.Architecture.Components;

/// <summary>Location crumbs for one path, derived from registered page routes.</summary>
public static class PageBreadcrumbTrail
{
  /// <summary>One crumb. <paramref name="Href"/> is null on the current page.</summary>
  public readonly record struct Crumb(string Label, string? Href);

  /// <summary>
  /// The registered route that is the longest literal prefix of <paramref name="routeOrPath"/>,
  /// or null when none is.
  /// </summary>
  public static string? ParentRoute(string routeOrPath, IReadOnlyList<string> routes)
  {
    string subject = Normalize(routeOrPath);
    string? best = null;
    foreach (string route in routes)
    {
      string candidate = Normalize(route);
      if (!IsStrictLiteralPrefix(candidate, subject))
      {
        continue;
      }

      if (best is null || candidate.Length > best.Length)
      {
        best = candidate;
      }
    }

    return best;
  }

  /// <summary>Home, ancestors, and the current page for <paramref name="relativePath"/>.</summary>
  public static IReadOnlyList<Crumb> Build
  (
    string relativePath,
    string? breadcrumbLabel,
    string? pageTitle,
    IReadOnlyList<PageRegistryEntry> registry
  )
  {
    string path = Normalize(relativePath);
    Dictionary<string, PageRegistryEntry> byRoute = new(StringComparer.OrdinalIgnoreCase);
    List<string> templates = [];
    foreach (PageRegistryEntry entry in registry)
    {
      string template = Normalize(entry.RouteTemplate);
      if (byRoute.ContainsKey(template))
      {
        continue;
      }

      byRoute.Add(template, entry);
      templates.Add(template);
    }

    List<string> ancestorRoutes;
    string label;
    if (byRoute.TryGetValue(path, out PageRegistryEntry? currentPage))
    {
      ancestorRoutes = Ancestors(path, templates);
      label = LabelOr(breadcrumbLabel, currentPage.Title);
    }
    else
    {
      ancestorRoutes = AncestorsOfChild(path, templates);
      label = LabelOr(breadcrumbLabel, LabelOr(pageTitle, LastSegment(path)));
    }

    List<Crumb> crumbs = [];
    foreach (string ancestorRoute in ancestorRoutes)
    {
      if (!byRoute.TryGetValue(ancestorRoute, out PageRegistryEntry? ancestor))
      {
        continue;
      }

      crumbs.Add(new Crumb(ancestor.Title, string.IsNullOrEmpty(ancestor.Url) ? ancestorRoute : ancestor.Url));
    }

    crumbs.Add(new Crumb(label, null));
    return crumbs;
  }

  private static List<string> AncestorsOfChild(string path, IReadOnlyList<string> templates)
  {
    string? nearest = ParentRoute(path, templates);
    if (nearest is null)
    {
      return [];
    }

    List<string> chain = Ancestors(nearest, templates);
    chain.Add(nearest);
    return chain;
  }

  private static List<string> Ancestors(string route, IReadOnlyList<string> templates)
  {
    List<string> chain = [];
    string cursor = route;
    while (ParentRoute(cursor, templates) is string parent)
    {
      chain.Add(parent);
      cursor = parent;
    }

    chain.Reverse();
    return chain;
  }

  private static bool IsStrictLiteralPrefix(string candidate, string subject)
  {
    if (candidate.Contains('{', StringComparison.Ordinal))
    {
      return false;
    }

    if (string.Equals(candidate, subject, StringComparison.OrdinalIgnoreCase))
    {
      return false;
    }

    if (candidate == "/")
    {
      return subject.Length > 1;
    }

    return subject.StartsWith(candidate + "/", StringComparison.OrdinalIgnoreCase);
  }

  private static string Normalize(string routeOrPath)
  {
    string value = routeOrPath.Trim();
    int cut = value.IndexOfAny(['?', '#']);
    if (cut >= 0)
    {
      value = value[..cut];
    }

    if (value.Length == 0)
    {
      return "/";
    }

    if (!value.StartsWith('/', StringComparison.Ordinal))
    {
      value = "/" + value;
    }

    if (value.Length > 1 && value.EndsWith('/', StringComparison.Ordinal))
    {
      value = value.TrimEnd('/');
    }

    return value;
  }

  private static string LabelOr(string? preferred, string fallback)
  {
    string? label = string.IsNullOrWhiteSpace(preferred) ? null : preferred.Trim();
    return label ?? fallback;
  }

  private static string LastSegment(string path)
  {
    int slash = path.LastIndexOf('/', StringComparison.Ordinal);
    string segment = slash >= 0 && slash < path.Length - 1 ? path[(slash + 1)..] : path;
    return Uri.UnescapeDataString(segment);
  }
}

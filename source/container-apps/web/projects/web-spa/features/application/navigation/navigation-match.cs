#region Purpose
// Nav-link match for one registry page: exact, unless [Page] also declares a parameterized route.
#endregion

#region Design
// NavMenu used NavLinkMatch.All for every item. A page whose [Page] alias contains a route
// parameter (Design System: /DesignSystem and /DesignSystem/{Part}) then matches nothing on the
// alias, and its group collapses. Prefix is derived from PageRegistryEntry.RouteTemplates,
// which the page generator fills from those [Page] templates. A page with only plain routes
// stays All, so /Admin does not light up on /Admin/Roles. Home is "/" and has no parameter,
// so it stays All: Prefix on "/" would light every page. Role detail is its own
// non-navigable page, so the Roles list item stays All and does not stay active on
// /Admin/Roles/{RoleId}. ContainsChildPath is true when the current path is under an
// item's Url + "/", so that category opens without marking the item active. "/" is
// skipped, or Home would open every group.
#endregion

namespace TimeWarp.Architecture.Features.Applications;

using Microsoft.AspNetCore.Components.Routing;

/// <summary>Match mode for a registry page's nav item.</summary>
public static class NavigationMatch
{
  /// <summary>Prefix when any declared template has a route parameter; otherwise exact.</summary>
  public static NavLinkMatch For(PageRegistryEntry entry)
  {
    ArgumentNullException.ThrowIfNull(entry);
    foreach (string template in entry.RouteTemplates)
    {
      if (template.Contains('{', StringComparison.Ordinal))
      {
        return NavLinkMatch.Prefix;
      }
    }

    return NavLinkMatch.All;
  }

  /// <summary>
  /// True when <paramref name="relativePath"/> is under one entry's Url plus a slash.
  /// The item stays inactive; the category uses this only to open.
  /// </summary>
  public static bool ContainsChildPath(IEnumerable<PageRegistryEntry> entries, string relativePath)
  {
    ArgumentNullException.ThrowIfNull(entries);
    ArgumentNullException.ThrowIfNull(relativePath);
    string path = Normalize(relativePath);
    foreach (PageRegistryEntry entry in entries)
    {
      string url = Normalize(entry.Url);
      if (url.Length <= 1)
      {
        continue;
      }

      if (path.StartsWith(url + "/", StringComparison.OrdinalIgnoreCase))
      {
        return true;
      }
    }

    return false;
  }

  private static string Normalize(string value)
  {
    int cut = value.IndexOfAny(['?', '#']);
    if (cut >= 0)
    {
      value = value[..cut];
    }

    value = value.Trim();
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
}

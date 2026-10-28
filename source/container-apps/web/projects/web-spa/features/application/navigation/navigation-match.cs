#region Purpose
// Nav-link match for one registry page: exact, unless [Page] also declares a parameterized route.
#endregion

#region Design
// NavMenu used NavLinkMatch.All for every item. A page whose [Page] alias contains a route
// parameter (Style Guide: /StyleGuide and /StyleGuide/{Part}) then matches nothing on the
// alias, and its group collapses. Prefix is derived from PageRegistryEntry.RouteTemplates,
// which the page generator fills from those [Page] templates. A page with only plain routes
// stays All, so /Admin does not light up on /Admin/Roles. Home is "/" and has no parameter,
// so it stays All: Prefix on "/" would light every page. Role detail is its own
// non-navigable page, so the Roles list item stays All and does not stay active on
// /Admin/Roles/{RoleId}.
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
}

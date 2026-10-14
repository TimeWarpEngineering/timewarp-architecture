#region Purpose
// The navigation destinations one principal may open: the policy-filtered PageRegistry, in menu order.
#endregion

#region Design
// One filter for every surface that lists pages (task 300). NavMenu renders it, CommandPaletteRoster
// turns it into Page rows, and the agent's navigate and site_map tools read it, so the menu, Ctrl-K,
// Ask and WebMCP cannot drift. The source is the generated PageRegistry.All ([Page(Navigable = true)]),
// already sorted by NavOrder, and this keeps that order. A page is kept when its Policy authorizes
// the principal through IAuthorizationService (policy name == permission id). The Anonymous policy
// always succeeds, and an empty policy string is treated the same. There is no category gate: a menu
// category shows when one of its pages does.
// Sign in is not a registry page. Signed-out surfaces add CommandPaletteRoster.SignInRow themselves.
#endregion

namespace TimeWarp.Architecture.Features.Applications;

/// <summary>Registry pages a principal may open, in menu order.</summary>
public static class NavigationDestinations
{
  /// <summary>The generated <see cref="PageRegistry.All"/> filtered for <paramref name="user"/>.</summary>
  public static Task<IReadOnlyList<PageRegistryEntry>> ForAsync
  (
    ClaimsPrincipal user,
    IAuthorizationService authorizationService
  ) =>
    ForAsync(user, authorizationService, PageRegistry.All);

  /// <summary><paramref name="pages"/> filtered for <paramref name="user"/>, order kept.</summary>
  public static async Task<IReadOnlyList<PageRegistryEntry>> ForAsync
  (
    ClaimsPrincipal user,
    IAuthorizationService authorizationService,
    IEnumerable<PageRegistryEntry> pages
  )
  {
    ArgumentNullException.ThrowIfNull(user);
    ArgumentNullException.ThrowIfNull(authorizationService);
    ArgumentNullException.ThrowIfNull(pages);
    List<PageRegistryEntry> permitted = [];
    foreach (PageRegistryEntry page in pages)
    {
      if (await IsAuthorizedAsync(user, authorizationService, page.Policy))
      {
        permitted.Add(page);
      }
    }

    return permitted;
  }

  /// <summary>True when <paramref name="policy"/> is empty or authorizes <paramref name="user"/>.</summary>
  public static async Task<bool> IsAuthorizedAsync(ClaimsPrincipal user, IAuthorizationService authorizationService, string policy)
  {
    if (string.IsNullOrEmpty(policy))
    {
      return true;
    }

    AuthorizationResult result = await authorizationService.AuthorizeAsync(user, policy);
    return result.Succeeded;
  }
}

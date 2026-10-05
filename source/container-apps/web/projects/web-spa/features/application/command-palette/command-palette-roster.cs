#region Purpose
// Builds the Ctrl-K palette rows the current principal may run: PageRegistry pages + human, parameterless catalog actions,
// plus a Sign in row while the principal is signed out.
#endregion

#region Design
// Sources (task 239): pages come from the generated PageRegistry (the same [Page(Navigable = true)]
// set NavMenu links to), commands from IActionCatalog ([CatalogAction]). Nothing is hand-listed.
// Commands: Visibility Human or Both, and no required constructor parameter — the palette has no
// argument UI, so Counter.IncrementCounter (int amount) and every Agent-only entry stay out.
// Permissions stay in code, through IAuthorizationService — the policies NavMenu's AuthorizeView
// and the page [Authorize] use (policy name == PermissionIds). Pages without a Policy carry the
// Anonymous policy (Home), which always succeeds; an empty policy string is treated the same. A command needs every one of its Permissions; a command with none still needs a
// signed-in principal, because every cataloged command acts on the user's own session (sign-out,
// passkeys) and must not be offered to an anonymous visitor.
// Command label (task 268): "Owner: Action" for every command, authored or generated — the owner is
// the catalog name's prefix (the owning state, or the slice for offer names such as "Identity.LinkMicrosoft365"),
// so rows group by that prefix whichever way their action part was written.
// The action part is ActionCatalogEntry.DisplayName ([CatalogAction(DisplayName = …)]) when set —
// labels are written deliberately ("Identity: Link Microsoft 365"); only a null DisplayName falls
// back to the catalog name made readable ("Profile.SignOut" → "Profile: Sign out", a digit run
// starts a word). Set DisplayName wherever the generated split reads poorly (brand casing, acronyms).
// The ranker matches the shown label and the description; the stable catalog name stays the Target.
// Sign in (task 259): an explicit, typed signed-out entry built from LoginPage.Title and
// LoginPage.GetPageUrl() — not a registry concept. Login stays out of PageRegistry.All (NavMenu
// must not list it) and Navigable keeps its meaning; one signed-out destination does not earn a
// new [Page] opt-in and generator surface. It is a Page-kind row, so running it takes the same
// CloseModal-then-RouteState path as every page row. Shown only when
// user.Identity?.IsAuthenticated != true; the target carries the current path as ?returnUrl
// (LoginPage validates it with GetSafeReturnUrl), omitted on "/" as RedirectToLogin does. The
// description says "Log in" and names the route so "login" and "log in" rank it as well as "sign".
// Applications is the platform tier, which must not reach a product slice; LoginPage is the one
// edge, opted out below like HomePage's first-run CTA.
#endregion

namespace TimeWarp.Architecture.Features.Applications;

using System.Text;

[CrossSliceReference(typeof(LoginPage), "Signed-out palette row navigates to Identity login (task 259).")]
public static class CommandPaletteRoster
{
  public static async Task<IReadOnlyList<CommandPaletteRow>> BuildAsync
  (
    ClaimsPrincipal user,
    IAuthorizationService authorizationService,
    IEnumerable<PageRegistryEntry> pages,
    IEnumerable<ActionCatalogEntry> actions,
    string currentPath
  )
  {
    List<CommandPaletteRow> rows = [];
    bool isAuthenticated = user.Identity?.IsAuthenticated ?? false;

    if (!isAuthenticated)
    {
      rows.Add(SignInRow(currentPath));
    }

    foreach (PageRegistryEntry page in pages)
    {
      if (await IsAuthorizedAsync(user, authorizationService, page.Policy))
      {
        rows.Add(new CommandPaletteRow(page.Title, $"Go to {page.Url}", CommandPaletteRowKind.Page, page.Url));
      }
    }

    foreach (ActionCatalogEntry action in actions)
    {
      if (!IsPaletteCommand(action) || !isAuthenticated)
      {
        continue;
      }

      if (await IsPermittedAsync(user, authorizationService, action))
      {
        rows.Add(new CommandPaletteRow(Label(action), action.Description, CommandPaletteRowKind.Command, action.Name));
      }
    }

    return rows;
  }

  /// <summary>The signed-out Sign in row; <paramref name="currentPath"/> becomes the login ?returnUrl.</summary>
  public static CommandPaletteRow SignInRow(string currentPath)
  {
    string loginUrl = LoginPage.GetPageUrl();
    string target = currentPath is "/" or ""
      ? loginUrl
      : $"{loginUrl}?returnUrl={Uri.EscapeDataString(currentPath)}";
    return new CommandPaletteRow(LoginPage.Title, $"Log in: go to {loginUrl}", CommandPaletteRowKind.Page, target);
  }

  /// <summary>Human-visible and runnable without arguments.</summary>
  public static bool IsPaletteCommand(ActionCatalogEntry action) =>
    action.Visibility is ActionVisibility.Human or ActionVisibility.Both
    && action.Parameters.All(static parameter => !parameter.IsRequired);

  /// <summary>"Owner: " + the authored <see cref="ActionCatalogEntry.DisplayName"/>, else the generated label.</summary>
  public static string Label(ActionCatalogEntry action) =>
    action.DisplayName is { } displayName
      ? WithOwner(action.Name, displayName)
      : DisplayName(action.Name);

  /// <summary>"Profile.SignOut" → "Profile: Sign out".</summary>
  public static string DisplayName(string catalogName)
  {
    int dot = catalogName.IndexOf('.', StringComparison.Ordinal);
    string action = dot < 0 ? catalogName : catalogName[(dot + 1)..];

    StringBuilder words = new();
    for (int index = 0; index < action.Length; index++)
    {
      char character = action[index];
      if (index > 0 && char.IsUpper(character))
      {
        words.Append(' ').Append(char.ToLowerInvariant(character));
      }
      else if (index > 0 && char.IsDigit(character) && !char.IsDigit(action[index - 1]))
      {
        words.Append(' ').Append(character);
      }
      else
      {
        words.Append(character);
      }
    }

    return WithOwner(catalogName, words.ToString());
  }

  private static string WithOwner(string catalogName, string label)
  {
    int dot = catalogName.IndexOf('.', StringComparison.Ordinal);
    return dot < 0 ? label : $"{catalogName[..dot]}: {label}";
  }

  /// <summary>True when <paramref name="user"/> passes every one of the action's Permissions (policy name == permission id).</summary>
  public static async Task<bool> IsPermittedAsync(ClaimsPrincipal user, IAuthorizationService authorizationService, ActionCatalogEntry action)
  {
    foreach (string permission in action.Permissions)
    {
      if (!await IsAuthorizedAsync(user, authorizationService, permission))
      {
        return false;
      }
    }

    return true;
  }

  private static async Task<bool> IsAuthorizedAsync(ClaimsPrincipal user, IAuthorizationService authorizationService, string policy)
  {
    if (string.IsNullOrEmpty(policy))
    {
      return true;
    }

    AuthorizationResult result = await authorizationService.AuthorizeAsync(user, policy);
    return result.Succeeded;
  }
}

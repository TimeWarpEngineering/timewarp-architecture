#region Purpose
// Builds the Ctrl-K palette rows the current principal may run: PageRegistry pages + human, parameterless catalog actions.
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
// Command display name is the catalog name made readable ("Profile.SignOut" → "Profile: Sign out")
// so typing words from it ranks; the stable catalog name stays the row Target.
#endregion

namespace TimeWarp.Architecture.Features.Applications;

using System.Text;

public static class CommandPaletteRoster
{
  public static async Task<IReadOnlyList<CommandPaletteRow>> BuildAsync
  (
    ClaimsPrincipal user,
    IAuthorizationService authorizationService,
    IEnumerable<PageRegistryEntry> pages,
    IEnumerable<ActionCatalogEntry> actions
  )
  {
    List<CommandPaletteRow> rows = [];

    foreach (PageRegistryEntry page in pages)
    {
      if (await IsAuthorizedAsync(user, authorizationService, page.Policy))
      {
        rows.Add(new CommandPaletteRow(page.Title, $"Go to {page.Url}", CommandPaletteRowKind.Page, page.Url));
      }
    }

    bool isAuthenticated = user.Identity?.IsAuthenticated ?? false;
    foreach (ActionCatalogEntry action in actions)
    {
      if (!IsPaletteCommand(action) || !isAuthenticated)
      {
        continue;
      }

      bool permitted = true;
      foreach (string permission in action.Permissions)
      {
        if (!await IsAuthorizedAsync(user, authorizationService, permission))
        {
          permitted = false;
          break;
        }
      }

      if (permitted)
      {
        rows.Add(new CommandPaletteRow(DisplayName(action.Name), action.Description, CommandPaletteRowKind.Command, action.Name));
      }
    }

    return rows;
  }

  /// <summary>Human-visible and runnable without arguments.</summary>
  public static bool IsPaletteCommand(ActionCatalogEntry action) =>
    action.Visibility is ActionVisibility.Human or ActionVisibility.Both
    && action.Parameters.All(static parameter => !parameter.IsRequired);

  /// <summary>"Profile.SignOut" → "Profile: Sign out".</summary>
  public static string DisplayName(string catalogName)
  {
    int dot = catalogName.IndexOf('.', StringComparison.Ordinal);
    string owner = dot < 0 ? "" : catalogName[..dot];
    string action = dot < 0 ? catalogName : catalogName[(dot + 1)..];

    StringBuilder words = new();
    for (int index = 0; index < action.Length; index++)
    {
      char character = action[index];
      if (index > 0 && char.IsUpper(character))
      {
        words.Append(' ').Append(char.ToLowerInvariant(character));
      }
      else
      {
        words.Append(character);
      }
    }

    return owner.Length == 0 ? words.ToString() : $"{owner}: {words}";
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

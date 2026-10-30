#region Purpose
// Builds the Ctrl-K palette rows the current principal may run: PageRegistry pages + human, parameterless actions,
// plus a Sign in row while the principal is signed out.
#endregion

#region Design
// Sources (task 239): pages come from NavigationDestinations (task 300), the policy-filtered
// PageRegistry that NavMenu, Ask and WebMCP read too. Design-system parts are extra Page rows
// from DeepLinks, added after the pages only when that filtered list still contains the Design
// System page. They are not menu items. They are agent destinations (task 446): AgentSiteMap reads
// the same DeepLinks, so site_map, navigate and Ask list exactly the parts Ctrl-K lists. The row
// name is DesignSystemParts.PaletteName, so the prefix stays DesignSystemPage.Title.
// Commands are derived (task 301): every *ActionSet.Action in web-spa, from the generated
// ActionRoster (ActionRosterSourceGenerator), so registering an action is what makes it a
// command. No attribute opts an action in. Nothing is hand-listed.
// Exclusion is explicit: [CommandPaletteOptOut("reason")] on the Action. A blank reason does not opt
// out (the derivation guard test fails on one). The palette has no argument UI, so an action with a
// required constructor parameter (ActionRosterEntry.Execute == null) is out by that rule, not by omission.
// [CatalogAction] is optional metadata, matched by Action type: DisplayName, Description, Visibility
// and Permissions override the defaults (Human, no permissions, generated label). Visibility must
// include Human, so Agent-only catalog entries stay out and an uncatalogued action is Human-only.
// Permissions stay in code, through IAuthorizationService — the policies the page [Authorize] uses
// (policy name == PermissionIds). Page filtering is NavigationDestinations'. A
// command needs every one of its Permissions; a command with none still needs a signed-in principal,
// because every command acts on the user's own session and must not be offered to an anonymous
// visitor (task 301 kept this rule for derived commands).
// Command label (task 268): "Owner: Action" — the owner is the catalog name's prefix (the owning
// state) split into words ("AgentSurface" → "Agent surface"), so rows group by state. The action part
// is ActionCatalogEntry.DisplayName when set, else the catalog name made readable ("Profile.SignOut"
// → "Profile: Sign out", a digit run starts a word).
// Runtime override (task 301, for task 361's admin-set assistant name): every registered
// IActionDisplayOverride is asked per command, first non-null wins; its Name replaces the whole label
// (no owner prefix) and its Description, when set, the description. It never changes the target,
// visibility or permissions. The convention label stays the placeholder; no literal is baked in here.
// An uncatalogued command has an empty description (the row omits the line): the catalog name is a
// code identifier, so it stays the Target and never becomes user-visible text or ranker input.
// The ranker matches the shown label and the description; the stable catalog name stays the Target.
// Sign in (task 259): an explicit, typed signed-out entry built from LoginPage.Title and
// LoginPage.GetPageUrl() — not a registry concept. Login stays out of PageRegistry.All (NavMenu
// must not list it) and Navigable keeps its meaning; one signed-out destination does not earn a
// new [Page] opt-in and generator surface. It is a Page-kind row, so running it takes the same
// CloseModal-then-RouteState path as every page row. Shown only when
// user.Identity?.IsAuthenticated != true; the target carries the current path as ?returnUrl
// (LoginPage validates it with GetSafeReturnUrl), omitted on "/" as RedirectToLogin does. The
// description says "Log in" and names the route so "login" and "log in" rank it as well as "sign".
// Applications is the platform tier, which must not reach a product slice. Two edges are opted
// out below. LoginPage is the signed-out Sign in row: login stays out of the registry, and one
// signed-out destination does not earn a new [Page] opt-in. DesignSystemParts is the catalog
// table: Ctrl-K lists each part from that one table, and the parts are not a second menu.
#endregion

namespace TimeWarp.Architecture.Features.Applications;

using System.Text;

using TimeWarp.Architecture.Features.DesignSystem;

[CrossSliceReference(typeof(LoginPage), "Signed-out palette row navigates to Identity login (task 259).")]
[CrossSliceReference(typeof(DesignSystemParts), "Ctrl-K lists each design-system part from the one part table (task 424).")]
public static class CommandPaletteRoster
{
  /// <summary>The palette roster: pages plus commands derived from <paramref name="actions"/> (the generated ActionRoster).</summary>
  public static Task<IReadOnlyList<CommandPaletteRow>> BuildAsync
  (
    ClaimsPrincipal user,
    IAuthorizationService authorizationService,
    IEnumerable<PageRegistryEntry> pages,
    IEnumerable<ActionRosterEntry> actions,
    IEnumerable<ActionCatalogEntry> catalog,
    IEnumerable<IActionDisplayOverride> displayOverrides,
    string currentPath
  )
  {
    Dictionary<Type, ActionCatalogEntry> metadataByAction = [];
    foreach (ActionCatalogEntry entry in catalog)
    {
      metadataByAction[entry.ActionType] = entry;
    }

    IActionDisplayOverride[] overrides = [.. displayOverrides];
    IEnumerable<Command> commands = actions
      .Select(action => (Action: action, Metadata: metadataByAction.GetValueOrDefault(action.ActionType)))
      .Where(static pair => IsPaletteCommand(pair.Action, pair.Metadata))
      .Select(pair =>
      {
        ActionDisplay? display = overrides
          .Select(displayOverride => displayOverride.Find(pair.Action))
          .FirstOrDefault(static found => found is not null);
        return new Command
        (
          pair.Action.Name,
          display?.Name ?? Label(pair.Action, pair.Metadata),
          display?.Description ?? pair.Metadata?.Description ?? "",
          pair.Metadata?.Permissions ?? []
        );
      });

    return BuildRowsAsync(user, authorizationService, pages, commands, currentPath);
  }

  /// <summary>
  /// Pages plus the cataloged commands only: the agent tool set's view of the palette, where every
  /// command row must resolve to an <see cref="ActionCatalogEntry"/> to become a tool.
  /// </summary>
  public static Task<IReadOnlyList<CommandPaletteRow>> BuildAsync
  (
    ClaimsPrincipal user,
    IAuthorizationService authorizationService,
    IEnumerable<PageRegistryEntry> pages,
    IEnumerable<ActionCatalogEntry> catalog,
    string currentPath
  ) =>
    BuildRowsAsync
    (
      user,
      authorizationService,
      pages,
      catalog
        .Where(IsPaletteCommand)
        .Select(static entry => new Command(entry.Name, Label(entry), entry.Description, entry.Permissions)),
      currentPath
    );

  private static async Task<IReadOnlyList<CommandPaletteRow>> BuildRowsAsync
  (
    ClaimsPrincipal user,
    IAuthorizationService authorizationService,
    IEnumerable<PageRegistryEntry> pages,
    IEnumerable<Command> commands,
    string currentPath
  )
  {
    List<CommandPaletteRow> rows = [];
    bool isAuthenticated = user.Identity?.IsAuthenticated ?? false;

    if (!isAuthenticated)
    {
      rows.Add(SignInRow(currentPath));
    }

    List<PageDeepLink> deepLinks = [];
    foreach (PageRegistryEntry page in await NavigationDestinations.ForAsync(user, authorizationService, pages))
    {
      rows.Add(new CommandPaletteRow(page.Title, $"Go to {page.Url}", CommandPaletteRowKind.Page, page.Url));
      deepLinks.AddRange(DeepLinks(page));
    }

    // Parts follow the pages. The same policy filter that hid the Design System page hides its parts,
    // and a caller that passed no pages (a command-only probe) does not grow a catalog.
    foreach (PageDeepLink link in deepLinks)
    {
      rows.Add(new CommandPaletteRow(link.Title, $"Go to {link.Route}", CommandPaletteRowKind.Page, link.Route));
    }

    if (!isAuthenticated)
    {
      return rows;
    }

    foreach (Command command in commands)
    {
      if (await IsPermittedAsync(user, authorizationService, command.Permissions))
      {
        rows.Add(new CommandPaletteRow(command.Label, command.Description, CommandPaletteRowKind.Command, command.Name));
      }
    }

    return rows;
  }

  private sealed record Command(string Name, string Label, string Description, IReadOnlyList<string> Permissions);

  /// <summary>A deep link into a permitted page: its title, route and what it shows.</summary>
  public sealed record PageDeepLink(string Title, string Route, string Description);

  /// <summary>The deep links under <paramref name="page"/>: each design-system part for the Design System page, else none.</summary>
  public static IReadOnlyList<PageDeepLink> DeepLinks(PageRegistryEntry page)
  {
    ArgumentNullException.ThrowIfNull(page);
    if (page.PageType != typeof(DesignSystemPage))
    {
      return [];
    }

    return
    [
      .. DesignSystemParts.PageOrder.Select(static part => new PageDeepLink
      (
        DesignSystemParts.PaletteName(part),
        DesignSystemParts.Url(part),
        DesignSystemParts.Description(part)
      ))
    ];
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

  /// <summary>True when the action carries a non-blank [CommandPaletteOptOut] reason.</summary>
  public static bool IsOptedOut(ActionRosterEntry action) => !string.IsNullOrWhiteSpace(action.OptOutReason);

  /// <summary>Not opted out, runnable without arguments, and Human-visible (no catalog metadata = Human).</summary>
  public static bool IsPaletteCommand(ActionRosterEntry action, ActionCatalogEntry? metadata) =>
    !IsOptedOut(action)
    && action.Execute is not null
    && (metadata?.Visibility ?? ActionVisibility.Human).HasFlag(ActionVisibility.Human);

  /// <summary>Human-visible and runnable without arguments (the catalog-only view).</summary>
  public static bool IsPaletteCommand(ActionCatalogEntry action) =>
    action.Visibility.HasFlag(ActionVisibility.Human)
    && action.Parameters.All(static parameter => !parameter.IsRequired);

  /// <summary>"Owner: " + the authored <see cref="ActionCatalogEntry.DisplayName"/>, else the generated label.</summary>
  public static string Label(ActionRosterEntry action, ActionCatalogEntry? metadata) =>
    metadata?.DisplayName is { } displayName
      ? WithOwner(action.Name, displayName)
      : DisplayName(action.Name);

  /// <summary>The same label for a catalog entry (agent offers name the action as the palette does).</summary>
  public static string Label(ActionCatalogEntry entry) =>
    entry.DisplayName is { } displayName
      ? WithOwner(entry.Name, displayName)
      : DisplayName(entry.Name);

  /// <summary>"Profile.SignOut" → "Profile: Sign out"; "AgentSurface.OpenAskPanel" → "Agent surface: Open ask panel".</summary>
  public static string DisplayName(string catalogName)
  {
    int dot = catalogName.IndexOf('.', StringComparison.Ordinal);
    return WithOwner(catalogName, Words(dot < 0 ? catalogName : catalogName[(dot + 1)..]));
  }

  private static string WithOwner(string catalogName, string label)
  {
    int dot = catalogName.IndexOf('.', StringComparison.Ordinal);
    return dot < 0 ? label : $"{Words(catalogName[..dot])}: {label}";
  }

  // PascalCase → sentence case: an upper-case letter or the start of a digit run starts a new word.
  private static string Words(string pascal)
  {
    StringBuilder words = new();
    for (int index = 0; index < pascal.Length; index++)
    {
      char character = pascal[index];
      if (index > 0 && char.IsUpper(character))
      {
        words.Append(' ').Append(char.ToLowerInvariant(character));
      }
      else if (index > 0 && char.IsDigit(character) && !char.IsDigit(pascal[index - 1]))
      {
        words.Append(' ').Append(character);
      }
      else
      {
        words.Append(character);
      }
    }

    return words.ToString();
  }

  /// <summary>True when <paramref name="user"/> passes every one of the catalog entry's Permissions.</summary>
  public static Task<bool> IsPermittedAsync(ClaimsPrincipal user, IAuthorizationService authorizationService, ActionCatalogEntry entry) =>
    IsPermittedAsync(user, authorizationService, entry.Permissions);

  /// <summary>True when <paramref name="user"/> passes every one of <paramref name="permissions"/> (policy name == permission id).</summary>
  public static async Task<bool> IsPermittedAsync(ClaimsPrincipal user, IAuthorizationService authorizationService, IEnumerable<string> permissions)
  {
    foreach (string permission in permissions)
    {
      if (!await NavigationDestinations.IsAuthorizedAsync(user, authorizationService, permission))
      {
        return false;
      }
    }

    return true;
  }
}

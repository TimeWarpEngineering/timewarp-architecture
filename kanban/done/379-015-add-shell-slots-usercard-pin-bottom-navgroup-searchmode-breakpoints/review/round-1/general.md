# Round 1 — general
**Date:** 2026-10-24
**Scope reviewed:** `82ef665fc..cb9a17ddd` (the task commit), plus call sites: `BaseComponent`
state accessors, `ProfileState`, `AuthenticationStateListener`, `command-palette.ts`, and the
FluentUI 5.0.1 layout and nav stylesheet.

## Summary

The commit adds `UserCard`, a default `ShellUserCard` under the menu, `NavPlacement` on `[Page]`
with a pinned second `FluentNav`, `ShellSearchMode` (Palette, Hidden, Custom), and a 68px rail
from 880px through 1279px. The default shell is unchanged. Ctrl-K is a document listener and the
trigger is optional, so Hidden and Custom keep the hotkey as claimed. I re-ran the claimed gates
in this worktree: `./bin/dev build` reported 0 warnings and 0 errors. The test classes passed:
PageSourceGenerator_Tests 33/33, UserCard_Should_ 5/5, TimeWarpPage_Should_ 13/13,
ShellBreakpoints_Should_ 1/1, and NavigationDestinations_ 7/7. The client-name scan of
`source tests skills` found 0 matches. I did not re-run template-smoke.

The main risks are in state and layout, which the HtmlRenderer tests cannot see:
- The card's name is cached at the wrong lifecycle point.
- The NavPlacement reader is narrower than its Design region says.
- The pinned column can overlap the links when the menu is tall.

## Issues

### Issue 1 — Severity: bug
- File: source/container-apps/web/projects/web-spa/components/ShellUserCard.razor:29
- Description: `Name` is computed only in `OnParametersSetAsync`. The component's only
  parameter is the cascaded `AuthenticationState`, so that method runs only when the auth task
  is replaced. On sign-in, `AuthenticationStateListener` dispatches `FetchProfileData` after the
  auth change, so the first computation sees `Alias == null` and caches the identity name.
  When the fetch lands, `ProfileState` (a subscribed `[StateAccess]` read) re-renders the card,
  but `OnParametersSetAsync` does not run. The name stays the identity name, while `Detail`
  (read in markup) updates to the email. A save on the Profile page (`UpdateProfile`) is stale
  in the same way until the next navigation rebuilds the shell. The `LoadedAlias` guard shows
  the intent to react to alias changes, but no render reaches that path.
- Suggestion: Keep only the auth-derived values (signed in, identity name, `CanOpenProfile`) in
  `OnParametersSetAsync`. Compute the display name in markup from the current
  `ProfileState.Alias`. Add a render test that changes the alias after the first render (or
  re-renders with a new ProfileState) and expects the card to show it.
- Status: open

### Issue 2 — Severity: bug
- File: source/analyzers/timewarp-architecture-analyzers/generators/page-source-generator.cs:317
- Description: `NavPlacement` is read from syntax: a `MemberAccessExpressionSyntax` whose name
  is `Bottom`. The Design region (line 59) says NavPlacement accepts "any compile-time constant
  (a const field reads like a literal)", which matches how `Description`, `NavGroup`, and
  `NavOrder` use `semanticModel.GetConstantValue`. The following forms compile, but the
  generator silently emits `Top` with no diagnostic:
  - a const field, for example `const NavPlacement Pinned = NavPlacement.Bottom;` with
    `NavPlacement = Placements.Pinned`
  - `using static ...NavPlacement;` with `NavPlacement = Bottom`
  - a cast such as `(NavPlacement)1`

  A const field whose own name is `Bottom` but whose value is `Top` would be read as `Bottom`.
- Suggestion: Use `semanticModel.GetConstantValue(arg.Expression)` and compare it with the
  underlying value 1, as the sibling arguments do. Otherwise, report a diagnostic for an
  expression the generator does not recognize, and correct the Design region. Add a generator
  test that sets the placement through a const field.
- Status: open

### Issue 3 — Severity: bug
- File: source/container-apps/web/projects/web-spa/components/NavMenu.razor.css:6
- Description: `.twe-nav-column` is `height: 100%` of the stretched nav grid item.
  `.twe-nav-menu` is `flex: 1 1 auto; min-height: 0` and has no `overflow`, and the user card
  after it is `flex: none`. When the menu is taller than the column, `.twe-nav-menu` shrinks
  and its links overflow onto the pinned user card. Previously `.twe-nav` was the only child at
  `height: 100%`, so the column scrolled. For example, an Administrator who expands Admin,
  Demos, and Developer has about 28 rows (roughly 1000px) on a 768px or 900px-tall laptop.
  This was not measured in a browser. Results say the shell was checked only at a 1264px-wide
  app bar.
- Suggestion: Give `.twe-nav-menu` `overflow-y: auto` so the menu scrolls inside its box while
  the card stays pinned. Alternatively, drop `min-height: 0` and let the column scroll as one
  unit, which means the card is not pinned. Cover it with the Playwright check in Issue 5.
- Status: open

### Issue 4 — Severity: suggestion
- File: source/container-apps/web/projects/web-spa/components/NavMenu.razor:78
- Description: In the rail, `.twe-nav__label { display: none }` removes the only text in each
  link from the accessibility tree. `UserCard.razor.css:48` does the same to the card's
  name. The Avatar is decorative, so the linked card becomes an unnamed link. A tooltip usually
  supplies a description, not an accessible name, so the rail's icon-only links probably
  have no name (WCAG 4.1.2). At full width, every item also gets a tooltip that repeats its
  visible label.
- Suggestion: Hide the label visually in the rail (a clip or visually-hidden rule) instead of
  using `display: none`, or put `aria-label` on the item and the card root. Consider showing the
  tooltip only in the rail.
- Status: open

### Issue 5 — Severity: suggestion
- File: source/container-apps/web/projects/web-spa/components/TimeWarpPage.razor.css:146
- Description: Nothing renders the rail at a real width. The file test pins the strings, but
  it cannot catch the following:
  - the overlap in Issue 3
  - nested `fluent-navsubitem` padding pushing icons past 68px
  - categories with no icon (`FluentNavCategory Title=@group` has no `IconRest`), which show
    only a chevron where the goal says "collapsed to the icon"

  Results say the rail was not measured in a browser.
- Suggestion: Extend the existing `FirstPaint_Given_PrerenderedShell` Playwright suite with
  a viewport at about 1024x700. Assert that the nav item is 68px wide, the labels are not
  visible, and the card box does not intersect the last menu link. Give each category an icon,
  or say plainly in the Design region that a rail category is a chevron.
- Status: open

### Issue 6 — Severity: nit
- File: source/container-apps/web/projects/web-spa/components/TimeWarpPage.razor.css:135
- Description: `.twe-nav-column__user` padding is written three times: once for the fragment
  wrapper (line 129), once through `::deep` (line 135), and once in `ShellUserCard.razor.css:1`.
  The `::deep` rule duplicates ShellUserCard's own isolated rule.
- Suggestion: Drop the `::deep` copy. ShellUserCard owns its root's style.
- Status: open

### Issue 7 — Severity: nit
- File: source/container-apps/web/projects/web-spa/wwwroot/css/tokens.css:95
- Description: `--twe-shell-nav-width` is declared but nothing reads it. `TimeWarpPage.razor:171`
  hard-codes `Width="240px"`, and only the file test keeps the two equal.
- Suggestion: Set `Width="var(--twe-shell-nav-width)"` on the navigation item if FluentLayoutItem
  passes the value through to an inline style. Otherwise, add a comment that the token is
  documentation only.
- Status: open

## Plan alignment

The restatement matches the task description. No misread was found.

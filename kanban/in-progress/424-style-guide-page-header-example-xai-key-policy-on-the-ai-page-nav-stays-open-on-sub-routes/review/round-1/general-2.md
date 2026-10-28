# Round 1 — general-2
**Date:** 2026-10-28
**Scope reviewed:** items 3 and 5. `page-source-generator.cs` (`RouteTemplates`),
`navigation-match.cs`, `NavMenu.razor`, `command-palette-roster.cs`, `design-system-parts.cs`, and
their tests (page-registry, page-source-generator, navigation-destinations, command-palette,
catalog-agent).

## Summary

Both items are met. Nav match comes from the generated registry: a template with `{` gets
`NavLinkMatch.Prefix` and a plain route stays `All`. FluentUI 5.0.1 `FluentNavItem` requires a
separator after the prefix, so `/DesignSystem` does not match `/DesignSystemFoo` (checked against
the decompiled package). The category opens from the same `Active` flag. Generator caching is
safe: `PageModel.RouteTemplates` was already an `ImmutableArray` with sequence equality. The
palette rows come from `DesignSystemParts`. They appear only when `NavigationDestinations` kept
`DesignSystemPage`, which is the same gate page rows use. The agent catalog skips Page rows. What
remains is a gap in what Results say about detail pages, a mutable generated property, and some
out-of-date Design regions and test names.

## Issues

### Issue 1 — Severity: suggestion
- File: source/container-apps/web/projects/web-spa/features/application/navigation/navigation-match.cs:12-14
- Description: the spec asks for "a sensible result" on the other parameterized pages. Results say
  the Roles item stays exact on `/Admin/Roles/{id}`, but they leave out the group. Each page renders
  a new `NavMenu` (inside `TimeWarpPage.razor:222`). A category only opens when it has an active
  item. So `/Admin/Roles/{id}`, `/Admin/Roles/New`, `/Admin/Principals/{id}`, and
  `/Admin/Feedback/{id}` all open with the Admin group collapsed. Opening a role from the list
  collapses its group, which is the symptom item 3 fixed for the design system.
- Suggestion: either open a category when the path is under one of its items' `Url + "/"` (without
  making the item active, derived from the registry), or record the collapsed group in Results and
  the Design region as the accepted outcome.
- Status: open

### Issue 2 — Severity: suggestion
- File: source/analyzers/timewarp-architecture-analyzers/generators/page-source-generator.cs:602, 651-675
- Description: `RouteTemplates` is emitted as `public … { get; set; }` and filled from a side
  dictionary `RouteTemplatesByPage` by a `static PageRegistry()` constructor. Any app code can write
  to the shared registry entries, and the property doesn't match `NavPlacement { get; init; }` next
  to it.
- Suggestion: emit `{ get; init; }` and set `RouteTemplates = [...]` in the existing object
  initializer. That removes the dictionary and the static constructor. Update the generator tests
  that cut the output at `RouteTemplatesByPage`.
- Status: open

### Issue 3 — Severity: suggestion
- File: source/analyzers/timewarp-architecture-analyzers/generators/page-source-generator.cs:38-40, 53-72
- Description: the Design region still says additional routes are "[Route] aliases only — never
  registry rows". It doesn't say every template is now on `PageRegistryEntry.RouteTemplates` or
  that `NavigationMatch` reads it.
- Suggestion: add one bullet saying entries carry every template (primary first) and nav match uses
  them.
- Status: open

### Issue 4 — Severity: suggestion
- File: source/container-apps/web/projects/web-spa/features/application/command-palette/command-palette-roster.cs:44-45
- Description: the Design region still says "LoginPage is the one edge". There is now a second
  platform-to-product edge, `[CrossSliceReference(typeof(DesignSystemParts), …)]`. The opt-out is
  fair given the spec's "no side channel", but the region should record it.
- Suggestion: name both edges and why the design system needs one.
- Status: open

### Issue 5 — Severity: nit
- File: tests/container-apps/web/web-spa-integration-tests/components/page-breadcrumb-trail-tests.cs:49; tests/container-apps/web/web-spa-integration-tests/features/application/command-palette-tests.cs:281; tests/container-apps/web/web-spa-integration-tests/features/application/page-registry-tests.cs:79
- Description: three test names still say Style Guide (`Name_Style_Guide_Parts_…`,
  `Rank_A_Style_Guide_Part_…`, `Keep_The_Style_Guide_On_…`). The spec's grep `style-?guide` misses
  the underscore, but item 6 asks for a complete rename. `git grep -i -E 'style[ _-]?guide'` finds
  them.
- Suggestion: rename them to Design_System, and use the wider grep as the check.
- Status: open

### Issue 6 — Severity: nit
- File: source/container-apps/web/projects/web-spa/features/application/command-palette/command-palette-roster.cs:10-11, 154; source/container-apps/web/projects/web-spa/components/NavMenu.razor:15
- Description: the `"Design system: "` prefix is typed by hand, and nothing ties it to the page title.
  The Design comments in both files have odd short lines ("They are not menu", "NavGroup names").
  One roster line is over 100 characters.
- Suggestion: rewrap the comments. Optionally pin the prefix to the page title with a test.
- Status: open

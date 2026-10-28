# Round 2 — general-2
**Date:** 2026-10-28
**Scope reviewed:** `git diff 0d475995b HEAD -- . ':!kanban'`, nav match, registry, and palette slice:
`navigation-match.cs`, `NavMenu.razor`, `page-source-generator.cs` and tests, `page-registry-tests.cs`,
`command-palette-tests.cs`, `page-breadcrumb-trail-tests.cs`, `command-palette-roster.cs`,
`design-system-parts.cs`, `breadcrumb-playwright-tests.cs`.

## Summary

M6, M7, M8, M9, M13, M16, and M18 are fixed. The M6 prefix logic handles every edge case checked:
case (OrdinalIgnoreCase), `/AdminX` against `/Admin/`, query and fragment, the Home `/` item, the
exact page, and a trailing slash. The Design System item still uses `Prefix` on `/DesignSystem/{Part}`.
No new bugs. Two nits.

## Prior findings

- **M6: fixed.** `NavigationMatch.ContainsChildPath` (`navigation-match.cs:40-62`) opens a category
  when the path is under an item's `Url + "/"`. It skips `/`, does not change `For`, and is tested by
  `Open_A_Group_On_A_Child_Of_An_Item_Without_Activating_It`.
- **M7: fixed.** The generator emits `RouteTemplates { get; init; }` (`page-source-generator.cs:605`)
  in the object initializer. `RouteTemplatesByPage` and the static constructor are gone, and the
  generator tests assert that.
- **M8: fixed.** `page-source-generator.cs:41-43` adds a `RouteTemplates` bullet.
- **M9: fixed.** `command-palette-roster.cs:45-48` names both edges, `LoginPage` and
  `DesignSystemParts`.
- **M13: fixed.** `git grep -i -E 'style[ _-]?guide' -- . ':!kanban'` is empty, and the three test
  names are renamed.
- **M16: fixed.** `breadcrumb-playwright-tests.cs:86-93` asserts exactly one menu
  `a[href='/Admin/Roles']` without `active`, and an expanded Admin category. The test was not run
  here.
- **M18: fixed.** The comments are rewrapped. `DesignSystemParts.PaletteName` builds the label from
  `DesignSystemPage.Title`.

## Issues

### Issue 1 — Severity: nit
- File: source/container-apps/web/projects/web-spa/components/NavMenu.razor:20
- Description: one line of the rewrapped Design comment is 101 characters.
- Suggestion: rewrap the paragraph.
- Status: open

### Issue 2 — Severity: nit
- File: source/container-apps/web/projects/web-spa/features/design-system/pages/DesignSystemPage.razor:30
- Description: the palette prefix comes from `DesignSystemPage.Title` (line 6), but the page markup
  still passes the literal `Title="Design System"` to `TimeWarpPage`, so the two can still drift.
- Suggestion: `Title="@Title"`.
- Status: open

## Tests run

Run with `dotnet run --project <csproj> -- --filter-class <Class>`, because `bin/dev` is stale:
- `PageSourceGenerator_Tests`: 36/36 passed.
- `PageRegistry_` (`All_Should_`): 7/7 passed.
- `CommandPalette_Should_`: 45/45 passed.
- `PageBreadcrumbTrail_Should_`: 4/4 passed.
- Not run: the breadcrumb Playwright test, and the flag-off template matrix.

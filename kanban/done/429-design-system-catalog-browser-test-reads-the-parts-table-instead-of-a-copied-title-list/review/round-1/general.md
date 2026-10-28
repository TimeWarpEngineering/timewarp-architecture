# Round 1 — general
**Date:** 2026-10-28
**Scope reviewed:** same as framework (product paths: `web-spa.csproj`, `web-spa-playwright-tests.csproj`,
`design-system-catalog-playwright-tests.cs`, `design-system-page-contract-tests.cs`)

## Summary

The Playwright catalog test now takes `PartTitles` from `DesignSystemParts.PageOrder` (`Part.Title`,
in order) and `GroupNames` from `Group` declaration order, keeping only groups that have a part.
`DesignSystemCatalog.razor:100` renders the heading as `@group`, and `VisibleGroups()` drops empty
groups, so `group.ToString()` plus the has-a-part filter is the same computation the component uses.
`DesignSystemParts` stays `internal`; `web-spa.csproj` adds `InternalsVisibleTo` for
`web-spa-playwright-tests`, matching the existing entries, and the Playwright project adds a
`web-spa` project reference (`web-server` already references `web-spa`, so the graph does not change).

The integration test `List_Every_Part_In_Page_Order` drops its 33-title copy and the fixed
`PageOrder.Length.ShouldBe(33)` roster count, and its group loop now skips a group with no parts,
which matches what the catalog renders. A search of `tests/` finds no other copied roster.
The behavior checks stay: Drawers filter (`["Layout"]`, `ds-catalog-layout`), color-tokens deep
link, page-header, brand, and the unknown slug.

Verified in this worktree:
- `dotnet build` of `web-spa-playwright-tests` and `web-spa-integration-tests`: 0 warnings, 0 errors.
- `web-spa-integration-tests --filter-method List_Every_Part_In_Page_Order`: 1/1 passed.
- `web-spa-playwright-tests --filter-method Catalog_Should_DeepLink_Filter_And_Reject_An_Unknown_Slug`: 1/1 passed.

The goal restatement matches the task. No misread.

Risk is low. The change touches only tests plus one `InternalsVisibleTo` line. No suppressions,
no weakened assertions.

## Issues

None.

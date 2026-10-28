# Design System catalog browser test reads the parts table instead of a copied title list

## Description

The Design System page lists one "part" per sample component (Brand, Color Tokens, Page header,
Drawers, and so on). The authority for which parts exist, their titles, groups, and order is
`DesignSystemParts.PageOrder` and the `DesignSystemParts.Group` enum in
`source/container-apps/web/projects/web-spa/features/design-system/design-system-parts.cs`.

The browser test
`tests/container-apps/web/web-spa-playwright-tests/design-system-catalog-playwright-tests.cs`
keeps its own hand-typed copies: `PartTitles` (every part title in page order) and `GroupNames`.
Every new part breaks `Catalog_Should_DeepLink_Filter_And_Reject_An_Unknown_Slug` until someone
edits the copy. Task 427 hit this: it added three parts, and the test expected 30 cards where 33
render.

Make the test derive the expected titles and group headings from the parts table, so the two
cannot drift. Repo rule: when two things must agree, generate one from the other.

## Requirements

- Expected part titles come from `DesignSystemParts.PageOrder` (`Part.Title`, in order).
- Expected group headings come from the groups the catalog shows, derived from the table
  (`Group` declaration order, only groups that have parts). Match whatever heading text the
  catalog renders for a group today; if that text is computed in the component, reuse the same
  computation rather than re-typing it.
- `DesignSystemParts` is `internal` to `Web.Spa`. Use the repo's existing pattern for test access:
  `web-spa.csproj` already has `InternalsVisibleTo` entries for test projects. Add the Playwright
  test project the same way, plus the project reference it needs. Do not make the type public.
- Keep the specific assertions that check behavior rather than the roster: the `Drawers` filter
  result, the `color-tokens` deep link, `page-header`, `brand`, and the unknown slug.
- Look for other copied part lists in tests (for example in
  `tests/container-apps/web/web-spa-integration-tests/features/design-system/`) and derive them
  the same way. Fixed single-part checks are fine.

## Out of scope

- Changing the parts table, the catalog component, or any part.
- Any change outside this repository. If something here seems to need one, stop and report.

## Goal (implementer restatement)

The catalog browser test must stop keeping its own copy of every part title and every group
heading. Those expectations come from `DesignSystemParts`: titles in `PageOrder`, and group
headings in `Group` declaration order for groups that actually have parts, using the same heading
text the catalog renders. The Playwright project gets `InternalsVisibleTo` plus a project
reference so it can read that internal table. `DesignSystemParts` stays internal.

Behavior checks stay as they are: Drawers filter, the color-tokens deep link, page-header, brand,
and the unknown slug. Any other test that copies a full part roster is switched to the same
table. A check of one named part can stay handwritten.

Done when `dev build` is 0/0, `dev test --group playwright` passes, and source, tests, and skills
contain no client name.

## Checklist

- [x] Playwright catalog test derives titles and groups from `DesignSystemParts`
- [x] Other copied part lists in tests derived the same way, or none found (say which)
- [x] `dev build` 0/0; `dev test --group playwright` passes
- [x] No client name in source, tests, or skills

## Session

- Created: c2bec172-f0eb-4648-9edb-30256e7756e3 (2026-10-28)
- Implementation: 01a18357-0027-7ba0-b21a-9c798017c084 (2026-10-28)
- `ganda repo audit` passed. The kanban-landed advisory names other claims (070-001, 104-024, 104-025, 104-026, 123, 141, 198, 244) and was left alone.
- Implement oracle: implemented by implementer-grok (grok, model grok-4.7), session 01a18357-0027-7ba0-b21a-9c798017c084, max-turns 500, Done — 2026-10-28T16:52:52Z

## Results

The catalog browser test takes expected titles from `DesignSystemParts.PageOrder` (`Part.Title`, in order). Expected group headings are the `Group` names in declaration order, and only groups that have a part. The catalog renders that name with `@group`, so the test uses the enum name. `DesignSystemParts` stays internal. `web-spa` exposes internals to `web-spa-playwright-tests`, and that project references `web-spa`.

The only other copied roster was the title list in `List_Every_Part_In_Page_Order`. That list now comes from `PageOrder`, and the group loop skips a group with no parts. The fixed count `PageOrder.Length.ShouldBe(33)` is gone; the rendered card count still matches `PageOrder.Length`. Breadcrumb tests that name one part stay as they are.

Drawers filter, the color-tokens deep link, page-header, brand, and the unknown slug stay as behavior checks.

The first Playwright run failed `Location_Trail_Should_Stay_The_Same_For_Every_Route_To_A_Page` while the page still said "Authorizing...". The rerun of the same group passed 32/32.

### How to validate

Smoke: `./bin/dev build` then `./bin/dev test --group playwright`. Also run `List_Every_Part_In_Page_Order` from `web-spa-integration-tests`.

Expect: build reports 0 Warning(s) and 0 Error(s). The Playwright group passes, including `Catalog_Should_DeepLink_Filter_And_Reject_An_Unknown_Slug`. `List_Every_Part_In_Page_Order` passes with titles taken from `PageOrder`.

## Notes

- Depends on task 427 (PR #589) being merged, since that adds the three parts the copy is missing.

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

## Checklist

- [ ] Playwright catalog test derives titles and groups from `DesignSystemParts`
- [ ] Other copied part lists in tests derived the same way, or none found (say which)
- [ ] `dev build` 0/0; `dev test --group playwright` passes
- [ ] No client name in source, tests, or skills

## Session

- Created: c2bec172-f0eb-4648-9edb-30256e7756e3 (2026-10-28)

## Notes

- Depends on task 427 (PR #589) being merged, since that adds the three parts the copy is missing.

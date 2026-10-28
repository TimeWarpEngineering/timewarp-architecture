# Round 2 — general-3
**Date:** 2026-10-28
**Scope reviewed:** `git diff 0d475995b HEAD -- . ':!kanban'`, page header, CSS, and skills slice:
`PageTitleBar.razor(.css)`, `TimeWarpPage.razor(.css)`, `PageHeaderSample.razor(.css)`, `tokens.css`,
the comment edits in composites and elements CSS, `skills/tw-blazor/SKILL.md`,
`design-system-page-contract-tests.cs`.

## Summary

M3, M10, M11, M12, M17, and M19 are fixed. The `PageTitleBar` extraction is sound:
- the shell has one h1;
- scoped CSS moved with the elements that `PageTitleBar` renders, and no other selector targets them;
- the breadcrumb focus-ring padding is intact;
- pages that have a title render the same layout.

No bugs. `HeadingLevel` is an unvalidated int, and a blank Title now drops Description and Actions.

## Prior findings

- **M3: fixed.** `AssertPageHeaderOwnsTheOnlyActions` (contract tests 383-398) requires exactly one
  `twe-page__actions`, after `data-qa="PageHeaderExample"`, for `all` and `page-header`, and none on
  every other part.
- **M10: fixed.** `PageTitleBar` is the one copy of the markup and CSS. The description is a `div`
  with the shell margin. The sample CSS keeps only its frame and the rule.
- **M11: fixed.** `tokens.css:75-77` gives the reason for `--twe-space-1`. The nine scale comments say
  4, 8, 12, 24, 48.
- **M12: fixed.** `tw-blazor/SKILL.md:228, 302, 316` point at `SectionSample.razor`,
  `ButtonsSample.razor`, and `FormsSample.razor`. All three files exist and hold that content.
- **M17: fixed.** The `ds-page-header` prefix replaces `ph-sample`.
- **M19: fixed.** The toast handlers and `@inherits BaseComponent` are removed. The rule says the
  buttons only show placement.

## Issues

### Issue 1 — Severity: suggestion
- File: source/container-apps/web/projects/web-spa/components/PageTitleBar.razor:15, 21-31
- Description: `HeadingLevel` is an int, but only `1` is special. Every other value, including 2–6,
  silently renders a `div`, so `HeadingLevel="2"` does not give an h2. The sample's `HeadingLevel="0"`
  is an undocumented magic value. `PageTitleBar` is a component that ships in every generated app.
- Suggestion: replace the int with a bool (for example `IsPageHeading`), or render h2–h6 for 2–6 and
  reject other values. Add a render test that the sample variant emits no h1.
- Status: open

### Issue 2 — Severity: nit
- File: source/container-apps/web/projects/web-spa/components/PageTitleBar.razor:33
- Description: the whole bar is skipped when `Title` is blank. The previous shell (`0d475995b`
  `TimeWarpPage.razor:236-244`) still rendered `twe-page__header` with Description and Actions when
  Title was blank. A blank-title page with Actions now loses them.
- Suggestion: apply the `IsNullOrWhiteSpace(Title)` check to the heading only, or record the new
  behavior in the Design block.
- Status: open

## Tests run

- `dotnet test --project tests/container-apps/web/web-spa-integration-tests/web-spa-integration-tests.csproj -c Release --filter-class "DesignSystemPageContract_.DesignSystemPage_Should_"`: 5/5 passed.
- `--no-build --filter-class "TimeWarpPageRender_.TimeWarpPage_Should_"`: 15/15 passed.
- `./bin/dev lint-skills`: the client-name scan passed. vally is not installed here, so the spec lint
  did not run. The fix pass reports `dev lint-skills` passed (9 skills).
- No Playwright, Aspire, or AppHost run.

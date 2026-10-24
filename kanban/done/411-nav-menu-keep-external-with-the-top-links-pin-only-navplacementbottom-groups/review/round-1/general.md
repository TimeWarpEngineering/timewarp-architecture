# Round 1 — general
**Date:** 2026-10-24
**Scope reviewed:** branch vs master, as in review-framework.md

## Summary

The change puts External back in the primary `FluentNav` after the top groups. `UserClaimsPage`, `ServicesPage`, `TestPage`, and `StyleGuidePage` lose `NavPlacement.Bottom`. The `twe-nav--pin` nav is wrapped in `@if (BottomGroups.Count > 0)`. `BottomGroups` comes from the policy-filtered destinations (`NavMenu.razor:68`), so a pin appears only for a permitted Bottom page. `.twe-nav-menu ::deep .twe-nav { flex: none; overflow: visible; }` overrides Fluent's `.fluent-nav` flex and scroll rules. The scoped selector is more specific than `.fluent-nav`, so `.twe-nav-menu` (flex column, `overflow-y: auto`, `min-height: 0`) stays the only scroller. The rail and drawer rules are unchanged. In the rail, labels are absolutely positioned inside `position: relative` items, so `overflow: visible` does not let them leak. The Purpose and Design text in `NavMenu.razor`, `NavMenu.razor.css`, and `TimeWarpPage.razor`, and in `skills/tw-blazor-layout`, matches the new layout. No other template page or doc still pins Developer or External; the remaining `NavPlacement.Bottom` hits are the generator and its unit tests, which keep the feature. The tests now check that External is in the primary nav after Home (signed out) and after `/StyleGuide` (admin), that the pinned nav is absent, that every registry group is Top, and that the CSS tokens are present.

The reviewer re-ran the integration tests: `dotnet run --project tests/container-apps/web/web-spa-integration-tests` gave 497 passed, 0 failed. The build ran as part of that run with warnings as errors. `dev build`, `ganda repo audit`, and the browser check were not re-run.

Risk is low. Nothing tests the branch where a Bottom group exists, because no page in the template uses Bottom. That matches the task's choice to keep the feature but not use it, and it is not raised as a finding.

## Issues

None.

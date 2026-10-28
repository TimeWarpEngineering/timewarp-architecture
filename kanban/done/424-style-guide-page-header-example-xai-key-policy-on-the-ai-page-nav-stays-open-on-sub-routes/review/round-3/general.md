# Round 3 — general
**Date:** 2026-10-28
**Scope reviewed:** the fix-pass delta `31bdd4ae7..0057d58aa` (product commit `727527b21`), excluding
`kanban/`. Eight files, 130 lines added and 18 removed.

## Summary

The pass closes the four open round-2 findings. `PageTitleBar` swaps the int `HeadingLevel` for a
bool `IsPageHeading`, and a blank title now drops only the heading. `DesignSystemPage` passes its own
`Title`. Two new draft tests cover the branch where an edited draft keeps its old version. The risk
is low. The new tests fail against the regressions they guard, and the three affected suites pass.
No new issues.

## Prior findings re-checked

- M20 (fixed): `Keep_The_Old_Byok_Version_When_The_Server_Mode_Changes` and
  `Keep_The_Old_Name_Version_When_The_Server_Name_Changes`. I traced each one through
  `SyncUnedited*Draft` (`AiPage.razor:166-234`). The server field differs from the synced value and
  the draft is edited, so the early return keeps the old version. A regression that always adopts the
  version would render `data-xai-byok-version="11"` (or `"9"`), and the tests assert that string is
  absent. Each test also checks that the other draft still adopts the new version.
- M23 (fixed): `DesignSystemPage.razor:30` is `Title="@Title"`.
  `Render_The_Page_Header_Sample_Without_An_H1` asserts the document title is "Design System".
- M24 (fixed): `PageTitleBar.razor:17` is `bool IsPageHeading = true`. Both callers pass it
  explicitly (`TimeWarpPage.razor:237`, `PageHeaderSample.razor:11`). No `HeadingLevel` reference to
  `PageTitleBar` remains in source, tests, or skills. `ChartCard` and `EmptyState` keep their own,
  separate `HeadingLevel`. The new contract test asserts that the sample renders `twe-page__title`,
  no `<h1` inside the example, and one `h1` on the page.
- M25 (fixed): the outer guard (`PageTitleBar.razor:38`) renders when Title, Description, or
  Actions is set. The blank-title check moved into `Heading`. With a blank title and no slots,
  nothing renders, which matches the earlier `TimeWarpPage` behavior. The Design block states this.
  `Keep_Description_And_Actions_When_The_Title_Is_Blank` covers a whitespace title with both slots,
  and an empty title with actions only.
- M21 and M22 stay wontfix. The delta does not touch them.

## Verification

- `dotnet build tests/container-apps/web/web-spa-integration-tests/web-spa-integration-tests.csproj -c Release`:
  0 warnings, 0 errors.
- `dotnet run --project <csproj> -c Release --no-build -- --filter-class <Class>`:
  `AiPage_Should_` 7/7, `TimeWarpPage_Should_` 16/16, `DesignSystemPage_Should_` 6/6.

## Issues

None.

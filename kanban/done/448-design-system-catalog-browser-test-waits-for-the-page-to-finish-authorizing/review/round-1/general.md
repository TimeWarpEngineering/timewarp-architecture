# Round 1 — general
**Date:** 2026-10-30
**Scope reviewed:** commit fd411af74 (`test(playwright): wait for the page to finish authorizing after a full load`) against its parent ef4f86a8b; product files under `tests/container-apps/web/web-spa-playwright-tests/`.

## Summary

The change adds `PlaywrightPageLoad.GotoAsync` / `ReloadAsync`, which do the same `DOMContentLoaded` load (120 s) as the two private helpers they replace and then wait up to 30 s for `.twe-appbar`. The catalog, breadcrumb, and clear-key tests route their signed-in full-page loads through it. Risk is low: only the wait before each assertion changed, and the landmark is sound.

Verified against the repo:

- `.twe-appbar` is authored only in `components/TimeWarpPage.razor` (line 205). No layout renders it. `Routes.razor` puts every routed page inside `AuthorizeRouteView` with the default `Authorizing...` content, so the app bar can only appear after authorization finishes and the page has rendered. `CounterPage`, `DesignSystemPage`, `RolesListPage`, and `AuthenticationPage` all render `TimeWarpPage`, so every route the changed tests load has the landmark. The unknown-slug case still renders `TimeWarpPage` (the test asserts the `Design System` `h1` there and passes).
- Diff check: in all three test files the only product lines touched are the `GotoAsync` / `ReloadAsync` calls, the removed private helpers, and one Design comment. Every assertion is byte-for-byte unchanged.
- The helper's `catch (TimeoutException)` binds to `System.TimeoutException`. Playwright 1.63 defines no `Microsoft.Playwright.TimeoutException` (a throwaway runfile referencing the package fails to resolve that name), and the suite already relies on the same catch in `ask-surface-playwright-tests.cs:114` and `clear-xai-api-key-playwright-tests.cs:61`.
- Other full-page loads (requirement 2): a read-only scan of all 37 non-helper `GotoAsync` / `ReloadAsync` call sites in the project found none followed by a default-timeout expect. Each is followed by an explicit wait of 60–120 s, or is a first-paint / MCP probe that must not wait for the shell. The implementer's "checked, no change needed" list holds.
- Hidden product delay (requirement 4): on this machine at load average 5–7 the whole `DesignSystemCatalog` test (host start, passkey sign-in, five full loads, all assertions) runs in 22 s, `Breadcrumb` in 26 s, `ClearXaiApiKey` (two tests) in 30 s. The per-load wait is therefore a few seconds at most, consistent with the implementer's 2.3–4.2 s under load 13. Nothing to report.
- Gates re-run here: `dev build` 0 warnings / 0 errors; the three changed classes pass (4/4 tests); `ganda repo audit` exit 0 with the same pre-existing `kanban-landed` warning about eight other tasks' claims.
- Restatement vs task: the `## Goal (implementer restatement)` matches the task. The task offered the `Design System` `h1` as an example landmark; the implementer chose the app bar so other pages can share the helper, which the task's "prefer one shared helper" wording invites. No misread.

## Issues

None. No issues were raised.

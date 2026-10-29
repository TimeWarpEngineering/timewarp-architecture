# Design System catalog browser test waits for the page to finish authorizing

## Description

`Catalog_Should_DeepLink_Filter_And_Reject_An_Unknown_Slug` in
`tests/container-apps/web/web-spa-playwright-tests/design-system-catalog-playwright-tests.cs`
has failed intermittently twice. The second time was a local full browser-test run on the
task 434 branch, 2026-10-28; that branch changed only the drawer test. It passed on re-runs and in
CI. (A third failure, on task 427, was a real stale title list and is fixed.)

The failing step is the unknown-slug navigation:

```
await GotoAsync(page, "/DesignSystem/no-such-part");
await Assertions.Expect(page.Locator("[data-qa=DesignSystemUnknown]")).ToContainTextAsync("No such part.");
```

Playwright's aria snapshot at the 5-second timeout showed only `Authorizing...`. The page was
still in the authorization placeholder. `GotoAsync` waits only for `DOMContentLoaded` (with a
120-second navigation timeout), and the next assertion uses Playwright's default 5-second expect
timeout. On a loaded machine (another worker's browser tests were running at the same time),
WASM start plus authorization took longer than 5 seconds after a full page load.

## Requirements

- Make the test wait for the page to be ready after each full-page `GotoAsync` before asserting.
  For example, wait for `Authorizing...` to be gone, or for a stable page landmark such as the
  `Design System` `h1`, with a generous timeout like the other waits in this file (30 seconds).
  Prefer one change in `GotoAsync` or one shared helper over per-step timeouts.
- Check the other Playwright tests that do full-page `GotoAsync` followed by a default-timeout
  expect, and apply the same pattern if a shared helper exists or is easy to share. List what you
  changed.
- Keep every assertion as strict as it is. Only the wait before it changes.
- If the wait hides a real product delay (authorization taking many seconds on an idle machine),
  stop and report it instead.

## Out of scope

- Changing authorization or page code.
- Any change outside this repository. If something here seems to need one, stop and report.

## Goal (implementer restatement)

The browser tests run with prerender off, so after a full-page navigation the DOM is empty, then
shows the router's `Authorizing...` placeholder, then the page. `GotoAsync` returns at
`DOMContentLoaded`, and the next assertion only has Playwright's 5-second default. On a loaded
machine WASM start plus authorization can take longer, and the test fails on a correct page.

Done means:

- One shared helper navigates (full page load or reload) and then waits up to 30 seconds for the
  shell's app bar, which renders only after authorization finishes (the whole page is inside
  `AuthorizeRouteView`). The Design System catalog test uses it for every full-page navigation.
- Other tests that do a full-page navigation followed by a default-timeout expect use the same
  helper. Every assertion stays as strict as before; only the wait before it changes.
- An idle-machine run shows the wait is short (no hidden product delay); if it is not, report it.
- `dev build` 0/0, `dev test --group playwright` passes, `ganda repo audit` clean.

## Checklist

- [x] Full-page navigation waits for the page to finish authorizing before asserting
- [x] Other tests with the same pattern checked; list changed or "none"
- [x] `dev build` 0/0; `dev test --group playwright` passes
- [x] No client name in source, tests, or skills

## Session

- Created: c2bec172-f0eb-4648-9edb-30256e7756e3 (2026-10-28)
- Implemented 2026-10-30: shared `PlaywrightPageLoad` helper; catalog, breadcrumb, and clear-key
  tests use it. Gates green (see Results).
- Implement oracle: implemented by implementer-claude (claude, model claude-opus-5-5), session bf8cf537-22e2-47cb-b81a-d0e5d5e7172f, max-turns 500, Done — 2026-10-29T19:18:13Z
- Review oracle: reviewed by reviewer-claude (claude, model claude-fable-5-1), effort 2, roster general, round 1, disposition clean — 2026-10-30
- Review oracle: review by reviewer-claude-fable (claude, model claude-fable-5-1), session a60dbd64-7824-4559-9796-d9720c40ef2f, max-turns 120 — 2026-10-29T19:25:44Z

## Results

New file `tests/container-apps/web/web-spa-playwright-tests/playwright-page-load.cs`:
`PlaywrightPageLoad.GotoAsync(page, path)` and `PlaywrightPageLoad.ReloadAsync(page)` load the
document (`DOMContentLoaded`, 120 s navigation timeout as before), then wait up to 30 s for the
shell's `.twe-appbar`. The whole page sits inside `AuthorizeRouteView` and the suite runs with
prerender off, so the app bar appears only after `Authorizing...` is gone and the page has
rendered. On timeout it throws with the URL and page text. The app bar, not the `Design System`
`h1`, is the landmark because every `TimeWarpPage` renders it whatever its title, so other tests
can share the helper.

Tests changed (assertions unchanged; only the wait before them):

- `design-system-catalog-playwright-tests.cs`: the private `GotoAsync` is gone; all five document
  loads (`/DesignSystem/drawers`, `/brand`, `/page-header`, `/DesignSystem`, `/no-such-part`) use
  the helper. The Design region says so.
- `breadcrumb-playwright-tests.cs`: same private `GotoAsync` removed; `/DesignSystem/color-tokens`,
  both `/Counter` loads, and `/Admin/Roles` use the helper, and the `ReloadAsync` before
  `ExpectTrailAsync` uses `PlaywrightPageLoad.ReloadAsync`.
- `clear-xai-api-key-playwright-tests.cs`: the `/Admin/Authentication` load (followed by a
  default-timeout `ToBeVisibleAsync`) uses the helper.

Checked, no change needed: every other full-page load in the project already waits on a landmark
with a 30–120 s timeout before asserting (sign-in helpers, Ask tests, drawer, feedback, admin AI,
passkey, shell-nav, first-paint, state dev tools, web MCP, agent links).

No hidden product delay: temporary timing (removed before commit) showed the new wait at
2.3–4.2 s per load with load average about 13 (other work running). That is close to the old 5 s
default, which explains the flake, and it is not many seconds on an idle machine.

Gates (2026-10-30):

- `dev build`: 0 warnings, 0 errors.
- `dev test --group playwright`: 33/33 passed.
- `dev check-client-names`: passed.
- `ganda repo audit`: exit 0, 34 passed. One warning, `kanban-landed`: 8 live claims for other
  tasks (070-001, 104-024, 104-025, 104-026, 123, 141, 198, 244) have no worktree on this machine.
  Its fix would release other workers' claims, which is out of scope for this task, so it is left
  as is.

### Review

- Rounds: 1. Roster: general (effort 2 by diff size, 287 lines).
- Final counts: bug 0 / suggestion 0 / nit 0; open 0, fixed 0, wontfix 0.
- Disposition: clean. No findings were raised.
- Reviewer re-ran `dev build` (0/0), the `DesignSystemCatalog`, `Breadcrumb`, and `ClearXaiApiKey`
  classes (4/4 pass), and `ganda repo audit` (exit 0, same pre-existing `kanban-landed` warning).
  A read-only scan of all 37 other `GotoAsync` / `ReloadAsync` call sites confirmed none is
  followed by a default-timeout expect. The three changed classes run in 22–30 s each including
  host start and sign-in, so the new wait hides no product delay.
- Paths: `review/review-framework.md`, `review/round-1/general.md`, `review/round-1/merged.md`,
  `review/disposition.md`.

### How to validate

Smoke:

```
./bin/dev build
./bin/dev test --group playwright
```

Or one class at a time, after a build:

```
tests/container-apps/web/web-spa-playwright-tests/bin/Release/net11.0/web-spa-playwright-tests --filter-class DesignSystemCatalog
tests/container-apps/web/web-spa-playwright-tests/bin/Release/net11.0/web-spa-playwright-tests --filter-class Breadcrumb
tests/container-apps/web/web-spa-playwright-tests/bin/Release/net11.0/web-spa-playwright-tests --filter-class ClearXaiApiKey
```

Expect: build 0/0; the playwright group passes 33/33, including
`Catalog_Should_DeepLink_Filter_And_Reject_An_Unknown_Slug`. Under load (for example, another
worker's browser tests running) the unknown-slug step waits for the shell instead of failing on
the `Authorizing...` placeholder after 5 s. If the page never finishes authorizing, the failure
reads "The page did not finish authorizing at <url>" with the page text.

## Review

- Round 1: Done

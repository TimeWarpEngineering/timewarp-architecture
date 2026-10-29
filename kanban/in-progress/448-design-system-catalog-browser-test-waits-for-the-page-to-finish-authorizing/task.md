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

## Checklist

- [ ] Full-page navigation waits for the page to finish authorizing before asserting
- [ ] Other tests with the same pattern checked; list changed or "none"
- [ ] `dev build` 0/0; `dev test --group playwright` passes
- [ ] No client name in source, tests, or skills

## Session

- Created: c2bec172-f0eb-4648-9edb-30256e7756e3 (2026-10-28)

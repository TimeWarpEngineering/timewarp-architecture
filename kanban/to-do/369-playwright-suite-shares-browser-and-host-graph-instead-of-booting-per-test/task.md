# Playwright suite shares the browser and host graph instead of booting per test

## Why
Baseline (2026-10-15, run starting 07:17): `ci` job ~14 min = runfile compile 43s + `dev build` 89s + `dev test` ~9m45s (25 projects, one at a time) + 3 publishes ~2m. `template-smoke` ~6.5 min runs in parallel. Part of the CI speed-up stack 365 → 366 → 367 → 368 → 369 → 370.

`web-spa-playwright-tests` takes **3m35s**, about 37% of `dev test`. Each test calls
`Playwright.CreateAsync()` and `Chromium.LaunchAsync()` (see
`ask-panel-design`, `ask-sign-in`, `ask-surface`, `feedback-attachment`) and boots its own
host.

## Requirements
- **One browser per test class or session**, with a fresh **browser context** per test (isolation
  stays per test: cookies, storage).
- **Host lifetime:** C-create is the default. The suite may opt into **C-share** with a Jaribu
  session fixture (`SessionHostFixture<TInner>` / `RegisterSessionFixture`), which the repo rules
  allow for expensive, multi-class, closed-box suites, using the exemplar `SpaSessionFixture` in
  `web-spa-integration-tests`.
  - Tests that need special host configuration (forced InteractiveServer, real cookie auth, a
    specific render mode) keep their own host or a separate keyed fixture.
  - Never use a process-static `Lazy`.
- Keep every test's assertions intact. The flaky-test fixes (351 armed signal, the 356
  `aria-expanded` wait) must stay deterministic.
- Before/after suite timing; run the suite 5× in a loop to show no new flakiness.

## Checklist
- [x] Shared browser + per-test context
- [x] Session host fixture(s) for compatible classes; special-config tests isolated (no class is
  C-share compatible; see Results. Passkey class shares its host per class.)
- [x] Timing before/after; 5× loop green
- [x] Gates: `dev build` 0/0, `dev test`, `ganda repo audit`

## Session
- Created: 2026-10-15 (cockpit, CI speed-up analysis per Steve)
- 2026-10-15 implement (ganda task work, Claude): shared browser session fixture, per-class host
  for the passkey class, timing and 5× loop, gates.
- Implement oracle: implemented by implementer-claude (claude, model claude-opus-5-5), session not reported, max-turns 500, Done — 2026-10-15T10:22:28Z

## Results

### What changed
- New `tests/container-apps/web/web-spa-playwright-tests/playwright-browser-session.cs`:
  `PlaywrightBrowserSession : SessionHostFixture<IBrowser>`. It is a Jaribu session fixture
  registered once from its own ModuleInitializer, the same shape as `SpaSessionFixture`. The first
  `GetAsync` installs Chromium once (with the ubuntu24.04 fallback), starts the driver, and launches
  headless Chromium. The Jaribu session hook disposes the browser and then the driver. No
  process-static `Lazy`.
- Every Playwright test now opens its own `IBrowserContext` on that shared browser and disposes only
  the context. Cookies, storage, HTTP cache, and the WebAuthn virtual authenticator stay per test.
  The six copies of `InstallChromium` and the per-test `Playwright.CreateAsync` and `LaunchAsync`
  calls are gone. The `Session` helpers in ask-sign-in and passkey-session take the shared browser.
- `PasskeySession_Given_InteractiveServer` boots its forced-InteractiveServer host once in
  `SetupOnce` and disposes it in `CleanUpOnce` (C-create per class). Neither test needs a fresh host.
- No assertions changed. The 351 armed-signal and 356 `aria-expanded` waits are untouched.

### Why no C-share host
`HostGraphFactory` binds fixed in-proc ports, so only one host can be up at a time. A
session-scoped host would block every class with its own config. No two classes share a host
config: ask-panel-design needs RecordChats and PrivacyUrl, ask-surface boots two configs,
first-paint forces InteractiveAuto with prerender, and passkey forces InteractiveServer with real
cookie auth. Within classes, most tests need a fresh host. The first passkey account on a host
claims Administrator (the feedback admin proofs and the AdminRename test), the sort proof asserts
exactly two rows, and AdminRename changes site settings. Sharing those hosts would mean weakening
assertions. The per-test timings also show host boot is not the cost. The cost is WASM boot and
passkey flows in each fresh context, which per-test isolation requires.

### Timing (local, `web-spa-playwright-tests`, 17 tests)
- Before: 2m 21.5s (`dev test -g playwright`).
- After: 2m 09.1s (`dev test -g playwright`). The 5× loop on the built test exe took 2m 08.1s,
  2m 10.1s, 2m 10.8s, 2m 09.5s, and 2m 30.5s. All 5 runs passed 17/17.
- Per test (after): the WASM tests take 4 to 19s each, and the InteractiveServer passkey tests take
  under 1s. Most of the time is in each test body, not in browser or host setup.

### Gates
- `dev build`: 0 warnings, 0 errors.
- `dev test`: the fast, integration, and playwright groups all pass.
- `ganda repo audit`: clean.

### How to validate
- Smoke: `dev build && dev test -g playwright`, then
  `grep -rn "Chromium.LaunchAsync\|Playwright.CreateAsync" tests/container-apps/web/web-spa-playwright-tests/*.cs`
- Expect: 17/17 pass. The grep finds only `playwright-browser-session.cs`, so Chromium is
  launched once per session and every test opens its own context.

### Review disposition
- Rounds: 1. Roster: general. Effort: 2 (by-diff budget).
- Final counts: bug 0, suggestion 0, nit 2 fixed (0 open, 0 wontfix).
- Disposition: **clean**. Both nits were comment-only Design-region fixes
  (feedback-attachment wrap; playwright-browser-session host-ownership wording).
- Paths: `review/review-framework.md`, `review/round-1/merged.md`, `review/disposition.md`.

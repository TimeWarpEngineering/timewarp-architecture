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
- [ ] Shared browser + per-test context
- [ ] Session host fixture(s) for compatible classes; special-config tests isolated
- [ ] Timing before/after; 5× loop green
- [ ] Gates: `dev build` 0/0, `dev test`, `ganda repo audit`

## Session
- Created: 2026-10-15 (cockpit, CI speed-up analysis per Steve)

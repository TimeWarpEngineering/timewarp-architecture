# Write real E2E tests for sunny-day money paths (primary use cases + payment flow)

## Why

The old `EndToEnd.Playwright.Tests` was a quickstart stub (an `Exe` that opened a visible browser
and navigated to `playwright.dev/dotnet` — it never touched our app). It was **dropped**. This task
replaces it with E2E tests that actually exercise the running application.

Scope it like a production app would: cover the **sunny-day "money paths"** — the handful of
primary user journeys the product exists to deliver, and (always) the **payment/checkout flow**.
E2E is expensive; keep it to the critical happy paths, not exhaustive coverage (unit/integration
tests cover the breadth).

## Candidate paths (refine for this template's actual features)

- [ ] Primary journey(s): the core use case(s) a user signs in and completes end-to-end.
- [ ] **Payment / checkout flow** — the money path, end-to-end (the one that must never break).
- [ ] Auth happy path (sign in → land on the app) as a prerequisite for the above.

## Technical setup (depends on the host strategy from 058)

- [ ] Decide the harness: real Playwright test project (headless, CI-friendly) — NOT the old
      `Exe`/`Headless=false` quickstart. Likely `Microsoft.Playwright.*` + a test runner.
- [ ] Stand up the app under test: point Playwright at the running Aspire app (reuse the
      integration/host strategy decided in 058 — how tests get a running host).
- [ ] `playwright install` (browsers) wired into the dev flow / CI.
- [ ] A dedicated `dev` command (e.g. `dev test-e2e`) and/or tag filter — E2E shouldn't run in the
      default `dev test` loop (slow, needs browsers + a running app).
- [ ] Place under `tests/` (kebab-case) and decide slnx/CI inclusion.

## Notes

- Blocked-ish on 058 slice 3 (integration/host strategy): E2E needs a running app to point at.
- Keep assertions on outcomes a user/business cares about (order placed, payment captured,
  confirmation shown), not implementation details.

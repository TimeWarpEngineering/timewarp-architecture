# E2E sunny paths human passkey agent paid call and tip

## Parent

104

## Description

Three sunny paths automated or harnessed: (1) human passkey onboard, (2) agent register+pay+call, (3) voluntary tip. Program exit criterion.

## Requirements

- All three green in CI or documented harness
- Fixie/Playwright as appropriate

## Checklist

- [x] Human path
- [x] Agent paid path
- [x] Tip path
- [x] Pipeline wiring

## Notes

Definition of Done for program 104.

### Depends on

104-014, 104-016, 104-020

## Session

- Created: 2026-07-16
- Implement + close: 2026-08-04

## Results

### Summary

Program exit criterion automated as a **named Jaribu suite** (not Playwright):

**File:** `tests/container-apps/web/web-server-integration-tests/features/program-104-sunny-paths-tests.cs`

| # | Story | What the test proves |
|---|--------|----------------------|
| 1 | Human passkey onboard | Start/Complete registration via `IntegrationSoftwareAuthenticator` → PrincipalId + identity-session cookie → `GetCurrentSession` authenticated |
| 2 | Agent register+pay+call | Agent key register → bearer (`demo:invoke`) → unpaid metered **402** → mock settle → **200** + `TrustTier.Funded` |
| 3 | Voluntary tip | Anonymous unpaid **402** → mock settle → **200** thank-you + `PAYMENT-RESPONSE` |

**Harness design (CI-safe):**

- Host: `HostGraphFactory` C-create Web(+Api), real HTTP + cookie middleware
- Payment: `MockFacilitatorClient` replaces `IFacilitatorClient` — no live chain
- Tip + metered surfaces forced `Enabled` + dead `PayTo` via `PostConfigure` (test host config layering)
- Human path uses software WebAuthn authenticator (same HTTP ceremonies as SPA `/Login` CTA)
- **Playwright deferred:** browser WebAuthn needs CDP virtual authenticator or real hardware — flaky/heavy in CI; software authenticator is the product-story equivalent. Manual Proton Pass smoke remains 104-016 dogfood.

**Pipeline:** suite is part of `web-server-integration-tests` (already under `dev test` globs).

```bash
cd tests/container-apps/web/web-server-integration-tests
dotnet test -c Release -- --filter-class Program104Sunny
# or: -- --filter-tag Program104Sunny
```

**104-014 note:** path 2 is the automated agent money sequence for the program exit bar. Concurrent 014 may still add CLI/script documentation (`agent money-path`); this suite does not block on that.

Related deeper/negative coverage remains in co-located `invoke-metered-capability-tests`, `submit-tip-tests`, and identity passkey/agent suites.

### Verification

- `dotnet build` web-server-integration-tests: **0/0**
- `dotnet test … --filter-class Program104Sunny`: **3/3** passed

### Disposition

- **Done.** Program 104 exit criterion green in CI via mock-facilitator HTTP suite.
- Playwright browser e2e not required for exit; documented in suite Design region.
- Epic checklist item 104-022 marked complete; Wave 4 remaining: 021 only (014 is Wave 3).

### How to validate

**Automated (program exit criterion)**
```bash
cd tests/container-apps/web/web-server-integration-tests
dotnet test -c Release -- --filter-class Program104Sunny
# or: -- --filter-tag Program104Sunny
# expect: 3/3
#   1) Human passkey onboard → principal + session
#   2) Agent register + pay (mock) + metered 200 + Funded
#   3) Voluntary tip mock settle
```

**Depends on:** in-proc web host; mock facilitator; no live chain; software authenticator (not Playwright).

**Not in scope:** browser WebAuthn via Playwright virtual authenticator (documented deferral).


# Round 5 — merged findings
**Date:** 2026-10-29
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 1 | 0 |
| suggestion | 0 | 2 | 0 |
| nit | 0 | 2 | 0 |

## Issues

### M1 — Severity: suggestion — Status: fixed
- File: source/container-apps/web/platform/payment/open-api-payment-responses-server.cs:85
- Description: The documented challenge `resource.url` was a fixed route, not the configured
  `TipOptions.Resource` / `MeteredCapabilityOptions.Resource`.
- Source: general (round 1)
- Disposition notes: Fixed in `0264769f3`; unchanged since. Carried from round 4.

### M2 — Severity: suggestion — Status: fixed
- File: tests/container-apps/web/web-server-integration-tests/open-api-payment-responses-tests.cs:240
- Description: The test stubs hard-coded the routes instead of using the contract `RouteTemplate`
  constants.
- Source: general (round 1)
- Disposition notes: Fixed in `0264769f3`; the stubs still use the contract constants after the M4 fix.

### M3 — Severity: nit — Status: fixed
- File: source/container-apps/web/platform/payment/open-api-payment-responses-server.cs:36
- Description: The dead `?? "/api/tip"` fallback.
- Source: general (round 1)
- Disposition notes: Removed in `0264769f3`; unchanged since.

### M4 — Severity: bug — Status: fixed
- File: source/container-apps/api/projects/api-server/program.cs:105
- Description: The four public stub endpoints in the shared integration-test assembly were picked up by
  api-server's FastEndpoints auto-discovery, so every in-process api-server host failed at startup with
  `Duplicate routes detected!` and PR #600 `ci (integration)` failed twice.
- Suggestion: Mirror web-server and set `DisableAutoDiscovery = true` with explicit `Assemblies`.
- Source: general (round 3, Issue 1; round 4, Issue 1; round 5 re-verify)
- Disposition notes: Fixed in `b1a6cb150`. api-server now scans only `Api.Server` and `Api.Contracts`,
  as web-server already does; no api-server endpoint lives outside those assemblies. Round 5 proof:
  `dotnet run tools/dev-cli/dev.cs -- test --group integration` passed 8 of 8 projects with no
  `Duplicate routes detected!` in the log; `dev build` 0 warnings (2026-10-29).

### M5 — Severity: nit — Status: fixed
- File: source/container-apps/web/platform/payment/open-api-payment-responses-server.cs:39
- Description: `FreeRoutesNever402` repeated the two routes as literals next to `TipRoute` /
  `MeteredRoute`.
- Suggestion: Build the sentence from the route constants.
- Source: general (round 3, Issue 2; round 4, Issue 2; round 5 re-verify)
- Disposition notes: Fixed in `b1a6cb150`. The field is `static readonly` and interpolates `/{TipRoute}`
  and `/{MeteredRoute}`; declaration order keeps the initializers safe, and the rendered sentence is
  unchanged, so `Info_Should_SayFreeRoutesNeverReturn402` still passes.

## Duplicates / conflicts

- None. Round 5 raised no new findings; M4 and M5 move from open to fixed.

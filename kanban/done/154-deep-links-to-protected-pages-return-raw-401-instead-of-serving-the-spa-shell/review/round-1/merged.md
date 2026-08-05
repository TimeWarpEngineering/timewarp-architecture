# Round 1 — merged findings
**Date:** 2026-08-05
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 0 | 0 |
| suggestion | 0 | 0 | 0 |
| nit | 0 | 1 | 0 |

## Issues

### M1 — Severity: nit — Status: fixed
- File: source/container-apps/web/platform/identity-host/identity-session-cookie-challenge-server.cs:47-62 (pre-fix)
- Description: After the `/api` hard-stop, `ShouldRedirectToLogin` always returned `true`. The `Sec-Fetch-Dest` / `Accept` branches were behaviorally dead; runtime was path-only while Design described multi-step classification.
- Suggestion: Simplify to path-only non-`/api` → redirect; document that Accept/fetch metadata are intentionally not required for curl smoke.
- Source: general
- Disposition notes: Simplified `ShouldRedirectToLogin` to `!Path.StartsWithSegments("/api")`; Design region updated to state path-only classification and reserved finer negotiation. Integration tests (HTML Accept + Accept-less) still green by construction.

## Duplicates / conflicts

- None (single reviewer).

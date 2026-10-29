# Round 2 — merged findings
**Date:** 2026-10-29
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 2 | 0 | 0 |
| suggestion | 0 | 0 | 0 |
| nit | 0 | 0 | 0 |

## Issues

### M1 — Severity: bug — Status: open
- File: source/foundation/foundation-server/open-api-discovery-coverage.cs:283; ingress: source/container-apps/yarp/program.cs:84, source/container-apps/aspire/projects/aspire-app-host/program.cs:837
- Description: Carried from round 1, unchanged. OpenAPI advertises `GET /api/health`, but ingress routes `/api/health` to Api.Server (404).
- Suggestion: See round-1 merged M1.
- Source: general
- Disposition notes: The fix pass 1 (implement-fix-2) made no product change.

### M2 — Severity: bug — Status: open
- File: source/foundation/foundation-server/open-api-discovery-coverage.cs:311; ingress: source/container-apps/yarp/program.cs:104-117, source/container-apps/aspire/projects/aspire-app-host/program.cs:857-861
- Description: Carried from round 1, unchanged. The documented `GET|POST /api` alias returns 500 AmbiguousMatchException at the public origin (duplicate `/api` and `/api/` routes).
- Suggestion: See round-1 merged M2.
- Source: general
- Disposition notes: The fix pass 1 (implement-fix-2) made no product change.

## Duplicates / conflicts

- None.

## Process note

- The implement-fix-2 oracle ended after 1 model call and returned `Done` with no commit. The next fix pass must actually change the ingress builders (or remove the operations) and add a test for each finding.

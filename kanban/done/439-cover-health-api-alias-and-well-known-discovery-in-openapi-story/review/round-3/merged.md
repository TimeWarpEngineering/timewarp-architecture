# Round 3 — merged findings
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
- Description: Carried from round 1, unchanged. OpenAPI advertises `GET /api/health`, but ingress routes `/api/health` to Api.Server, which returns 404.
- Suggestion: See round-1 merged M1.
- Source: general
- Disposition notes: Fix pass 2 (implement-fix-3) made no product change.

### M2 — Severity: bug — Status: open
- File: source/foundation/foundation-server/open-api-discovery-coverage.cs:311; ingress: source/container-apps/yarp/program.cs:104-117, source/container-apps/aspire/projects/aspire-app-host/program.cs:857-861
- Description: Carried from round 1, unchanged. The documented `GET|POST /api` alias returns 500 `AmbiguousMatchException` at the public origin, because the `/api` and `/api/` routes are duplicates.
- Suggestion: See round-1 merged M2.
- Source: general
- Disposition notes: Fix pass 2 (implement-fix-3) made no product change.

## Duplicates / conflicts

- None.

## Process note

- Two fix passes in a row (implement-fix-2 and implement-fix-3, both implementer-grok on grok-4.7-build) each ended after one model call (`num_turns: 1`) and returned `Done` with no commit. In the fix-3 log, the model reasons that "the final response must be exactly the JSON object". That suggests it reads the structured-output instruction as a stop signal before it does any work. Another fix pass on the same implementer profile will probably no-op again. The operator should look at the implementer-grok headless profile, or send the fix pass to a different implementer.

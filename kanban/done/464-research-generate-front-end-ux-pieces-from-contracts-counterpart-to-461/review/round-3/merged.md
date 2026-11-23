# Round 3 — merged findings
**Date:** 2026-11-05
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 0 | 0 |
| suggestion | 0 | 3 | 0 |
| nit | 0 | 2 | 0 |

## Resolved prior (re-verified against `29d388aff..dfcd31272`)

- **M1 — suggestion — fixed** (round 1). The fix pass did not touch it. Option D still adds
  `OnProblem` before `HandleError`, and the A-only check now covers the `OnStarted` / `Succeeded` /
  `OnProblem` legs.
- **M2 — suggestion — fixed** (round 1). Untouched by the delta.
- **M3 — nit — fixed** (round 1). Untouched by the delta.
- **M4 — nit — fixed** (round 1). Untouched by the delta.
- **M5 — suggestion — fixed** (round 2). `Running` moves to an `OnStarted(TRequest)` virtual
  called after the null skip at `api-handler.cs:71`. The `catch (Exception)` at `:87-92` calls
  `OnProblem` with a synthetic problem before `throw;`. That covers the transport
  `HttpRequestException` (not caught in `HttpApiService`), the `ValidationException` from `:123`,
  and a throw inside `GetRequest`. The two paths outside the `try` (`HandleFileResponse` and a
  hand-written `HandleSuccess` that throws) are named as accepted coding-bug gaps. The scope
  line, the A-only check, the B example, and `task.md` Results all match.

## Issues

No new findings.

## Duplicates / conflicts

- None (single reviewer, no findings).

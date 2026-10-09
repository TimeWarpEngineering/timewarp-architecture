# Disposition — task 293

**Date:** 2026-10-10
**Outcome:** accepted-exceptions
**Rounds:** 2
**Final open count:** 0

## Summary

Effort 3, general reviewer. Round 1 raised 1 bug, 2 suggestions and 1 nit. The bug was the Ask probe running ahead of the identity work in `AuthenticationStateListener`; the probe now runs last. The test race was fixed and confirmed by a reproduced flake, then 3 green runs. `ChatProbeCompleted` was removed. Mapping transport exceptions to Error was declined as a cross-cutting `ApiHandler` policy change. Round 2 found nothing new.

## Exception log

| ID | Severity | Rationale | Decided by |
|----|----------|-----------|------------|
| M2 | suggestion | `ApiHandler`/`HttpApiService` rethrow transport exceptions for every SPA handler by design; changing that is outside task 293's scope (401 masking + re-probe). After M1, a throw no longer skips the identity pipeline. | orchestrator (review oracle) |

## Escalations

- None.

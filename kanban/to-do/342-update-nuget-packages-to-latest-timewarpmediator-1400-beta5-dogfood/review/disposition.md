# Disposition — task 342

**Date:** 2026-10-13
**Outcome:** accepted-exceptions
**Rounds:** 1
**Final open count:** 0

## Summary

The review raised one bug and two nits. M1, the bug, was fixed: ApiHandler semaphores are now shared per (store, state type) across all handler types through `ApiHandlerSemaphores`. M2 was fixed with a comment change. M3 is wontfix with the rationale below. After the fixes, `dev build` shows 0 warnings and 0 errors, and `dev test` passes all projects. No round 2: the fix delta is small and the gates were re-run on it.

## Exception log (if accepted-exceptions)

| ID | Severity | Rationale | Decided by |
|----|----------|-----------|------------|
| M3 | nit | No code mutates the shared List<T>. Contract type changes are out of scope. The ICloneable shims go away with timewarp-state#646. | orchestrator (review oracle) |

## Escalations

- None

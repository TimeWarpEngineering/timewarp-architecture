# Disposition — task 296

**Date:** 2026-10-10
**Outcome:** accepted-exceptions
**Rounds:** 2
**Final open count:** 0

## Summary

The aside dock, full-screen rule, and geometry tests are correct. One bug (icon-only Ask buttons
rendered as specks because of the global svg max-width reset) was fixed on this task with a scoped
CSS rule and an icon-size assertion. One nit (task-folder screenshot path) is accepted as the
established evidence pattern.

## Exception log (if accepted-exceptions)

| ID | Severity | Rationale | Decided by |
|----|----------|-----------|------------|
| M2 | nit | Per-task screenshot folder is the shared pattern in the Playwright project and Requirement 5 asks for shots beside this task | orchestrator |

## Escalations

- None.

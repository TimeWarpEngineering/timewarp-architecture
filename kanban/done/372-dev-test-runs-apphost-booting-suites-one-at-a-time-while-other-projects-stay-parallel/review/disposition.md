# Disposition — task 372

**Date:** 2026-10-15
**Outcome:** accepted-exceptions
**Rounds:** 1
**Final open count:** 0

## Summary

The general reviewer (effort 2) raised 1 bug, 1 suggestion and 1 nit. The bug (false IPv6 port
collision on hosts without `::1`) and the suggestion (unbounded Postgres probe connect, plus an
undisposed container on the final attempt) are fixed on this task. The nit is wontfix. Gates after
the fixes: `dev build` reported 0 warnings and 0 errors. `dev test -q` passed 23 of 23 projects.
`dev test -g integration` passed 8 of 8 on a fresh Release build. `dev test -g fast` passed 14 of 14.

## Exception log (if accepted-exceptions)

| ID | Severity | Rationale | Decided by |
|----|----------|-----------|------------|
| M3 | nit | A false positive only serializes a suite, and the monorepo guard pins the exact AppHost set | orchestrator |

## Escalations

- None.

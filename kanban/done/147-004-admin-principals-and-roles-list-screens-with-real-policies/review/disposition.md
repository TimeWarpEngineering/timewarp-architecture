# Disposition — task 147-004

**Date:** 2026-08-04
**Outcome:** accepted-exceptions
**Rounds:** 1
**Final open count:** 0

## Summary

Round-1 general review found one SPA bug (effective vs stored role drafts after Save) and several suggestions. Fixes: re-fetch principals after Set, NotifySessionChanged for self-edit, explicit checkbox selection, EffectiveRolesResolver Jaribu coverage, Principals page lockout/bootstrap copy. One wontfix: bootstrap remains all-environment with empty default (plan D3 break-glass).

## Exception log

| ID | Severity | Rationale | Decided by |
|----|----------|-----------|------------|
| M5 | suggestion | Bootstrap empty-by-default in all envs is intentional break-glass; Development-only would block Production recovery | orchestrator |

## Escalations

- None

# Disposition — task 478

**Date:** 2026-11-05
**Outcome:** accepted-exceptions
**Rounds:** 1
**Final open count:** 0

## Summary

One general reviewer (effort 2) reviewed commit 5e6bb6281. The change meets the task and the
implementer restatement: `Role.AgentScopes` reads back in `RoleCatalog.AgentScopes` order
independent of set order and EF load order (EF uses field access on `AgentScopeList`), the three
Roles handlers no longer call `AgentScopeOrder.OrderKnown`, `AgentScopeOrder` keeps only the
Principals and `AgentScopeApproval` callers, and unknown scopes stay rejected as documented.
web-domain-tests re-run by the reviewer: 57/57 passed. No bugs or suggestions; one cosmetic nit
accepted as wontfix.

## Exception log (if accepted-exceptions)

| ID | Severity | Rationale | Decided by |
|----|----------|-----------|------------|
| M1 | nit | Unreflowed 144-char Design comment line in role-domain.cs:16; cosmetic, no enforced line-length rule, gates pass | review oracle |

## Escalations

- None.

# Disposition — task 147-006

**Date:** 2026-08-04
**Outcome:** accepted-exceptions
**Rounds:** 1
**Final open count:** 0

## Summary

EF dual-mode principal→role store is correct. Fixed stale comments and scoped resolution in authz tests. One accepted exception: EnsureCreated will not alter existing Aspire volumes — operators drop volume or create `identity.principal_roles` once (documented in How to validate).

## Exception log

| ID | Severity | Rationale | Decided by |
|----|----------|-----------|------------|
| M1 | suggestion | EnsureCreated upgrade gap is host ops for existing volumes; migrations not in template yet | orchestrator |

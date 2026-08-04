# Disposition — task 147-006

**Date:** 2026-08-04
**Outcome:** accepted-exceptions
**Rounds:** 1
**Final open count:** 0

## Summary

EF dual-mode principal→role store is correct. Fixed stale comments and scoped resolution in authz tests. M1 (EnsureCreated no-op on existing volumes) was **fixed after disposition** via `PostgresModelSchemaBootstrap` — startup creates missing model tables automatically (template automation, no hand DDL).

## Exception log

| ID | Severity | Rationale | Decided by |
|----|----------|-----------|------------|
| M1 | suggestion | Fixed post-review: bootstrap creates missing tables on existing volumes | orchestrator (follow-up) |

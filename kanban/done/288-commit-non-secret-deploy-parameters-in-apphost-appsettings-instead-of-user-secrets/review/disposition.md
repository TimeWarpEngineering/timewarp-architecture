# Disposition — task 288

**Date:** 2026-10-08
**Outcome:** accepted-exceptions
**Rounds:** 1
**Final open count:** 0

## Summary

One general reviewer (effort 2) found no bugs: generated `Contoso.Shop` app gets `contoso-shop` for the three identity parameters, `registry-endpoint` stays `localhost:5001`, no stray rewrites; dev-cli-tests 152/152. One nit fixed (program.cs comment), one suggestion accepted as wontfix.

## Exception log

| ID | Severity | Rationale | Decided by |
|----|----------|-----------|------------|
| M1 | suggestion | `onlyIf`-scoped bare-name replacement keeps the monorepo's committed values deployable; smoke asserts guard spacing drift; decision recorded in task.md Results | orchestrator (review oracle) |

## Escalations

- none

# Round 2 — general (orchestrator re-verification)
**Date:** 2026-10-05
**Scope reviewed:** fix delta for M1, M3, M5 (CommandPalette.razor, command-palette-ranker.cs, contextual-action-arguments.cs, hypermedia-lab-tests.cs)

## Summary

M1: `@key=row` — the record's value equality makes the keys unique for distinct contextual rows (different Target or ArgumentsJson), and roster Page/Command rows were already unique. M3: the Design region matches the enum order. M5: a required parameter given JSON null is refused before deserialization; the new theory input passes. `dev build` 0/0; web-spa lab suite 24/24, CommandPalette 35/35. No new issues on the fix delta. Prior IDs carried: M1/M3/M5 fixed; M2/M4/M6/M7/M8 wontfix (rationale in round-1/merged.md).

## Issues

None.

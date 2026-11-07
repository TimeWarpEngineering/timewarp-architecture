# Round 1 — merged findings
**Date:** 2026-11-07
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 0 | 0 |
| suggestion | 0 | 0 | 0 |
| nit | 0 | 0 | 1 |

## Issues

### M1 — Severity: nit — Status: wontfix
- File: source/container-apps/web/projects/web-spa/state-json-context.cs:23
- Description: 13 of the 18 feature `using` lines repeat web-spa global usings, including the `#if grpc` / `#if api`
  using blocks. Only 5 (`Admin.Ai`, `Admin.Feedback`, `Admin.Principals`, `Admin.Roles`, `DesignSystem`) are new.
- Suggestion: keep only the non-global usings and drop the two conditional using blocks.
- Source: general
- Disposition notes: wontfix (review oracle). This is cosmetic only. It has no effect on behavior, the build, or the
  template matrix: IDE0005 is not a build rule, and the repeated conditional usings use the same symbols as the
  template excludes, so every variant compiles. A fix pass would only reorder imports. Task 483 rewrites this State
  surface (`[ContractState]`) and can drop the redundant lines when it edits the file.

## Duplicates / conflicts

- None (single reviewer).

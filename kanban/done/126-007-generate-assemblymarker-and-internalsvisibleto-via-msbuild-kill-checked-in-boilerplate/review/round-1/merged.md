# Round 1 — merged findings
**Date:** 2026-07-27
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 1 | 0 |
| suggestion | 0 | 1 | 0 |
| nit | 0 | 0 | 0 |

## Issues

### M1 — Severity: bug — Status: fixed
- File: source/container-apps/web/web-spa/components/Routes.razor:4-5
- Description: Bare `typeof(AssemblyMarker)` bound to TimeWarp.State.AssemblyMarker after SPA class removal.
- Suggestion: Use `typeof(IAssemblyMarker)`.
- Source: general
- Disposition notes: Fixed to `typeof(IAssemblyMarker)` for AppAssembly and AdditionalAssemblies. Re-grep under web-spa clean.

### M2 — Severity: suggestion — Status: fixed
- File: Directory.Build.targets
- Description: Compile only inside Inputs/Outputs target could drop include on incremental rebuilds.
- Suggestion: Split write vs always-include targets.
- Source: general
- Disposition notes: Split into `GenerateAssemblyMarker` (write) + `IncludeGeneratedAssemblyMarker` (always include when enabled). Full build 0/0 after fix.

## Duplicates / conflicts

None.

# Round 1 — merged findings
**Date:** 2026-07-22
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 0 | 0 |
| suggestion | 0 | 2 | 0 |
| nit | 0 | 1 | 0 |

## Issues

### M1 — Severity: suggestion — Status: fixed
- File: source/container-apps/web/msbuild/feature-membership.targets
- Description: Membership guard hardcodes layer globs/match/nesting; generated props only partly consumed (SSOT gap).
- Suggestion: Drive hybrid Compile ItemGroups, zero-match match arms, and nesting from the registry.
- Source: general
- Disposition notes: Generator now emits hybrid Compile ItemGroups + `FeatureFilenameLayerSuffixRegex`; nesting rejected at gen time; membership targets only import props and use the regex. Drift test asserts globs per layer and that membership targets do not re-list `*-contracts.cs`.

### M2 — Severity: suggestion — Status: fixed
- File: source/analyzers/timewarp-architecture-convention-analyzers/feature-filename-grammar-analyzer.cs
- Description: After collapse, bare `features/` is treated as cohesive; SPA project-relative paths can be mis-scoped.
- Suggestion: Prefer affirmative cohesive markers; add SPA-relative regression tests.
- Source: general
- Disposition notes: Scope uses affirmative markers only (`../features/`, absolute `/web/features/`); bare `features/` only after `../features/` traversal. Added SPA-relative + absolute web-spa tests with grammar-shaped names (silent).

### M3 — Severity: nit — Status: fixed
- File: skills/tw-web-api-contracts/SKILL.md
- Description: Workflow steps still show pre-grammar filenames.
- Suggestion: Align with `*-contracts.cs` grammar.
- Source: general
- Disposition notes: Workflow examples updated to `get-*-contracts.cs` / `role-details-contracts.cs`.

## Duplicates / conflicts

- None

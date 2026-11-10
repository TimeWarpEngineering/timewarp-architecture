# Round 2 — merged findings
**Date:** 2026-11-10
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 0 | 0 |
| suggestion | 0 | 1 | 0 |
| nit | 0 | 1 | 0 |

## Issues

### M1 — Severity: suggestion — Status: fixed
- File: AGENTS.md:52, documentation/developer/guides/analyzer-package-split.md:16
- Description: The docs repeated the exact SourceGenerators pin from `Directory.Packages.props`.
- Source: general (round 1)
- Disposition notes: Fixed in b4001294e. Both files say "1.0.0-beta.13 and later; pin in `Directory.Packages.props`". Verified by grep in round 2.

### M2 — Severity: nit — Status: fixed
- File: source/analyzers/timewarp-architecture-attributes/type-stem-identifier-attribute.cs:13
- Description: The Design region named convention-analyzers as the simple-name matcher.
- Source: general (round 1)
- Disposition notes: Fixed in b4001294e. The region names TimeWarp.SourceGenerators (TWA0023). Verified in round 2.

## Duplicates / conflicts

- None. Round 2 raised no new findings.

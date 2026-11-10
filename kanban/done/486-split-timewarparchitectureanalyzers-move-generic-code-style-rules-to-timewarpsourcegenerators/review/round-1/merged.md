# Round 1 — merged findings
**Date:** 2026-11-10
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 0 | 0 |
| suggestion | 1 | 0 | 0 |
| nit | 1 | 0 | 0 |

## Issues

### M1 — Severity: suggestion — Status: open
- File: AGENTS.md:52 (also documentation/developer/guides/analyzer-package-split.md:17)
- Description: Both files repeat the exact SourceGenerators pin (1.0.0-beta.13) from `Directory.Packages.props`. The next bump makes them wrong, and no check catches it. This breaks the AGENTS.md hard rule "When two things must agree, generate one from the other or add a build check."
- Suggestion: Write "1.0.0-beta.13 and later" (the minimum version) and point at `Directory.Packages.props` for the pin, in both places.
- Source: general
- Disposition notes:

### M2 — Severity: nit — Status: open
- File: source/analyzers/timewarp-architecture-attributes/type-stem-identifier-attribute.cs:13
- Description: The Design region says "convention-analyzers match by simple name". The TWA0023 matcher is now TimeWarp.SourceGenerators.
- Suggestion: Name TimeWarp.SourceGenerators (TWA0023) as the analyzer that matches by simple name and does not reference this assembly.
- Source: general
- Disposition notes:

## Duplicates / conflicts

- None. There was one reviewer.

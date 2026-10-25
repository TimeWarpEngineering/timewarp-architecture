# Round 1 — merged findings
**Date:** 2026-10-25
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 1 | 0 | 0 |
| suggestion | 1 | 0 | 0 |
| nit | 0 | 0 | 0 |

## Issues

### M1 — Severity: bug — Status: open
- File: tests/tools/dev-cli-tests/entra-tenant-tests.cs:96 (also :148)
- Description: `Union_Should_DedupeCaseInsensitivePreserveFirstCasing` and
  `MatchById_DifferentGuidCasing_Should_Select` no longer test case-insensitivity. The new GUID
  `00000000-0000-0000-0000-000000000001` is all digits, so its upper-case and lower-case forms are
  the same string. Both tests would pass with an ordinal comparison. The task asked that
  assertions keep their shape.
- Suggestion: Use a made-up GUID with hex letters for the case variants (for example
  `aaaaaaaa-0000-0000-0000-000000000001` / `AAAAAAAA-0000-0000-0000-000000000001`), and keep the
  first-casing assertion.
- Source: general
- Disposition notes:

### M2 — Severity: suggestion — Status: open
- File: tools/dev-cli/services/client-name-scan.cs:103
- Description: Scan roots are hardcoded (`source`, `tests`, `skills`, plus a `tools` probe). These
  packed paths are not scanned: `msbuild/**`, `.template.config/**`, the root build and solution
  files, the kanban skeleton, and the package `readme.md`. A name added there ships and passes the
  gate. That breaks the AGENTS.md hard rule "generate one from the other or add a build check".
- Suggestion: Build the scan set from the pack csproj `Content`/`None Pack=true` items, with their
  `Exclude` plus `skills/**/analysis/**`. Or list the missing roots and files, and add a test that
  fails when the allow-list gains an `Include` the scan does not cover.
- Source: general
- Disposition notes:

## Duplicates / conflicts

- None (single reviewer).

# Round 1 — merged findings
**Date:** 2026-10-15
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 0 | 0 |
| suggestion | 0 | 0 | 0 |
| nit | 0 | 0 | 1 |

## Issues

### M1 — Severity: nit — Status: wontfix
- File: tools/dev-cli/services/aspire-publish.cs:130
- Description: `Fail` returns `IReadOnlyList<string>?` (shaped by `PublishAndCheckAsync`); other callers discard it.
- Suggestion: `void Fail` + explicit `return null`.
- Source: general
- Disposition notes: wontfix (orchestrator) — private-to-dev-cli helper, same "return Fail(...)" idiom the file used before (it returned `string?` for the old single caller); purely cosmetic, no behaviour impact.

## Duplicates / conflicts

- None (single reviewer).

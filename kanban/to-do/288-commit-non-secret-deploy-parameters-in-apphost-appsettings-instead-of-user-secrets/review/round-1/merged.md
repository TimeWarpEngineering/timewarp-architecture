# Round 1 — merged findings
**Date:** 2026-10-08
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 0 | 0 |
| suggestion | 0 | 0 | 1 |
| nit | 0 | 1 | 0 |

## Issues

### M1 — Severity: suggestion — Status: wontfix
- File: .template.config/template.json:140
- Description: `appNameKebab` replaces the bare `timewarp-architecture` scoped by `onlyIf` `after` matchers instead of a dedicated placeholder token; depends on the exact `"key": "` spacing.
- Suggestion: accept and record rationale, or switch to a unique token.
- Source: general
- Disposition notes: wontfix (orchestrator). The deviation is already recorded in task.md Results (Placeholder decision): a unique token would make the monorepo's own committed values non-deployable. Template-smoke asserts `Contoso.Shop` → `contoso-shop` for all three keys, so a spacing drift fails loudly; the reviewer confirmed no other rewrite in the generated tree.

### M2 — Severity: nit — Status: fixed
- File: source/container-apps/aspire/projects/aspire-app-host/program.cs:248
- Description: over-wide comment line; omitted registry parameters as value-less/committed.
- Suggestion: re-wrap and name all committed parameters.
- Source: general
- Disposition notes: fixed — comment re-wrapped and now names namespace, release name and the registry parameters.

## Duplicates / conflicts

- none (single reviewer)

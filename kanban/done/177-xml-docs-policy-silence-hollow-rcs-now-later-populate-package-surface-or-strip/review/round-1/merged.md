# Round 1 — merged findings
**Date:** 2026-09-21
**Sources:** general, orchestrator re-verification

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 0 | 0 |
| suggestion | 0 | 0 | 0 |
| nit | 0 | 1 | 0 |

## Issues

### M1 — Severity: nit — Status: fixed
- File: source/analyzers/timewarp-architecture-convention-analyzers/contract-nullability-validator-analyzer.cs:37
- Description: This diff stacked a second `<summary>` (the house one-liner used on other analyzers) immediately after the existing multi-paragraph class summary. Two `<summary>` elements in one comment are malformed XML docs; consumers see the first block. The file's Design region already names that first block as the SSOT for the two contradictions and skipped shapes.
- Suggestion: Delete the one-line second `<summary>`. Keep the existing multi-paragraph summary.
- Source: orchestrator (merge pass; general.md raised none)
- Disposition notes: Deleted the stacked one-liner; kept the multi-paragraph class summary. Convention-analyzers Release build 0/0. Duplicate-summary rg now empty.

## Duplicates / conflicts

- None. General raised zero issues; M1 is an additional finding from orchestrator re-verification of the analyzer XML placement.

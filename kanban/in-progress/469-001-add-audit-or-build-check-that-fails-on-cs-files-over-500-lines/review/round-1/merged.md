# Round 1 — merged findings
**Date:** 2026-11-04
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 0 | 0 |
| suggestion | 0 | 0 | 1 |
| nit | 0 | 0 | 2 |

## Issues

### M1 — Severity: suggestion — Status: wontfix
- File: source/analyzers/timewarp-architecture-convention-analyzers/file-length-analyzer.cs:6
- Description: TWA0038 does not reach `tools/`, which has files at 494 and 496 lines.
- Suggestion: File a follow-up task to wire the analyzer into `tools/` and decide its exemptions.
- Source: general
- Disposition notes: wontfix on this id (decided by the review orchestrator). Covering `tools/`
  needs an operator decision on which convention rules apply there and how to exempt NuGet
  content files. The implementer tried it and backed it out, and recorded that in Results. That
  scope is outside 469-001's acceptance list. Follow-up: the host or operator should file a kanban
  task for `tools/` coverage, because the reviewer cannot create tasks.

### M2 — Severity: nit — Status: wontfix
- File: source/analyzers/timewarp-architecture-convention-analyzers/file-length-analyzer.cs:29
- Description: "Matches `wc -l`" is inaccurate when a file has no trailing line break.
- Suggestion: Reword the Design region sentence.
- Source: general
- Disposition notes: wontfix (orchestrator). Comment wording only, with no behavior impact. The
  repo's files end with a line break, so the two counts agree in practice. Fix it the next time
  this file is edited.

### M3 — Severity: nit — Status: wontfix
- File: source/analyzers/timewarp-architecture-convention-analyzers/file-length-analyzer.cs:19
- Description: Typo "an auto-generated comment headers".
- Suggestion: Singular "header".
- Source: general
- Disposition notes: wontfix (orchestrator). A comment typo does not justify a fix pass. Fix it
  the next time this file is edited.

## Duplicates / conflicts

- None.

# Disposition — task 210-005

**Date:** 2026-09-12
**Outcome:** accepted-exceptions
**Rounds:** 2
**Final open count:** 0

## Summary

Round 1 (effort 1, general) raised 7 findings against the documentation-tree retirement. Six were fixed on this task id (dangling how-to/ADR citations, analysis-exclude smoke that only probed one skill, inventory count typo). One suggestion (M5) is wontfix: this repo keeps `[ganda.audit] directory-structure.severity = warning` because `documentation/` remains in ganda `RequiredDirectories`. Round 2 re-verified the fix delta; no new issues, no reopened IDs.

## Exception log (if accepted-exceptions)

| ID | Severity | Rationale | Decided by |
|----|----------|-----------|------------|
| M5 | suggestion | Local `.editorconfig` warning is the intended remedy so `ganda repo audit` stays non-blocking. Dropping `documentation/` from `RequiredDirectories` is a ganda-repo change, not this template. Advisory `directory-structure` warning accepted until ganda ships an exemption. | orchestrator |

## Escalations

- Task 214 resolved M5: `directory-structure.severity = warning` replaced with `directory-structure.exclude = documentation` (ganda 278 / PR #157).

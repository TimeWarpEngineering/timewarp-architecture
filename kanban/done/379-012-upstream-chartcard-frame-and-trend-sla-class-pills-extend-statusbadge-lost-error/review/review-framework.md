# Review framework

## Budget (by-diff)

- Lines changed: 1316
- Effort: 3
- TCB hits: none
- Roster axes: general
- Turn cap: 200 (--max-turns; cursor uncapped)

## Scope

**Date:** 2026-10-24
**Host task:** kanban/to-do/379-012-upstream-chartcard-frame-and-trend-sla-class-pills-extend-statusbadge-lost-error/
**Diff scope:** branch `task/379-012-upstream-chartcard-frame-and-trend-sla-class-pills` (3d5e22546) vs master merge-base 907b056ab, `source/` and `tests/` (26 files, +1226 / -3)
**Plan / brief:** task.md Description, Checklist, and `## Goal (implementer restatement)`: ChartCard frame, ChartLegendItem, TrendPill, SlaChip, ClassPill, `BadgeStatus.Error`, two `--twe-on-*` tokens, style-guide cards, HtmlRenderer tests
**Effort:** 3 (by-diff budget); roster axis general only
**Reviewer roster:** general (review oracle, Claude Opus 5.5, headless print mode)
**Session IDs:** review oracle session not exposed in print mode

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`

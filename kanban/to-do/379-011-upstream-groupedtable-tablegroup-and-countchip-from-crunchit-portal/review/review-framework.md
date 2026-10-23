# Review framework

## Budget (by-diff)

- Lines changed: 677
- Effort: 2
- TCB hits: none
- Roster axes: general
- Turn cap: 120 (--max-turns; cursor uncapped)

## Scope (task 379-011)

**Date:** 2026-10-24
**Host task:** kanban/to-do/379-011-upstream-groupedtable-tablegroup-and-countchip-from-crunchit-portal/
**Diff scope:** branch `task/379-011-upstream-groupedtable-tablegroup-and-countchip-fro` vs `master` (merge base 2ed781812, head 1a1cdf8c5); product paths under `source/` and `tests/`
**Plan / brief:** task.md Goal (implementer restatement), Decision, Checklist, and Accessibility sections: native `GroupedTable`, `TableGroup`, and `CountChip` leaves, plus the style guide sample, overview rows, and HtmlRenderer render tests
**Effort:** 2 (general only, per the by-diff budget)
**Reviewer roster:** general
**Session IDs:** review oracle (Claude Code, claude-opus-5-5), session id not exposed in print mode

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`

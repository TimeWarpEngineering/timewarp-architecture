# Review framework

## Budget (by-diff)

- Lines changed: 502
- Effort: 2
- TCB hits: none
- Roster axes: general
- Turn cap: 120 (--max-turns; cursor uncapped)

# Review framework — task 469-001

**Date:** 2026-11-04
**Host task:** kanban/in-progress/469-001-add-audit-or-build-check-that-fails-on-cs-files-over-500-lines/
**Diff scope:** branch `task/469-001-add-audit-or-build-check-that-fails-on-cs-files-ov` vs `master` (commits 2ab9f7d30..63d296e91; product commit a0707c152)
**Plan / brief:** TWA0038 `FileLengthAnalyzer` in the convention analyzers, tests, version bump 2.0.0-beta.30 → beta.31, one skill line.
**Effort:** 2 (by-diff budget, 502 lines)
**Reviewer roster:** general
**Session IDs:** reviewer-claude (claude, anthropic, claude-opus-5-5), ganda task work review oracle, session id not exposed to the oracle

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`

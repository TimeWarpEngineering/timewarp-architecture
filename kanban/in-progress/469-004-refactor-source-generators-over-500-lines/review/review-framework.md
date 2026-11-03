# Review framework

## Budget (by-diff)

- Lines changed: 4627
- Effort: 3
- TCB hits: none
- Roster axes: general
- Turn cap: 200 (--max-turns; cursor uncapped)

## Framework — task 469-004

**Date:** 2026-11-04
**Host task:** kanban/in-progress/469-004-refactor-source-generators-over-500-lines/
**Diff scope:** branch `task/469-004-refactor-source-generators-over-500-lines` (HEAD 2486db61a) vs
merge-base de6a1a998 (master). 17 product files under
`source/analyzers/timewarp-architecture-analyzers/generators/` and
`source/foundation/foundation-contracts-generators/`, plus kanban files.
**Plan / brief:** split the page, contracts, fast-endpoint, and typed-id generators into a
generator (pipeline wiring), collector, model, and emitter. Each file is 500 lines or fewer, the
output is byte-identical, and the pipeline shape is unchanged (task.md Goal).
**Effort:** 3 (by-diff budget), roster axis general. The general lens ran as two read-only
helper passes split by generator (page + typed-id; contracts + fast-endpoint). The orchestrator
reran the gates independently.
**Reviewer roster:** general
**Session IDs:** reviewer-claude (claude-opus-5-5, headless ganda task work); helper subagents
a2c620598e626ca44, ac3ebd1ecae20eb58 (sonnet). An earlier reviewer-opencode attempt exited 1
before writing any round (oracle-review-20261103t204827313z.log).

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`

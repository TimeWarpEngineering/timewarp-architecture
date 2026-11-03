# Review framework

## Budget (by-diff)

- Lines changed: 5626
- Effort: 3
- TCB hits: none
- Roster axes: general
- Turn cap: 200 (--max-turns; cursor uncapped)

## Scope

**Date:** 2026-11-04
**Host task:** kanban/in-progress/469-005-refactor-web-feature-and-platform-source-files-over-500-lines/
**Diff scope:** branch `task/469-005-refactor-web-feature-and-platform-source-files-ove` vs merge-base
`ebac73588` (head `a91c4721f`), excluding `kanban/`. 20 product files, +2906 / -2517.
**Plan / brief:** task.md `## Goal (implementer restatement)`: split `XaiChatUpstream` and
`EntraTicketProcessor` by responsibility, split four co-located Jaribu runfiles by scenario,
every non-migration web `.cs` file at 500 lines or fewer, behavior unchanged.
**Effort:** 3 (by-diff budget), roster axis general only.
**Reviewer roster:** general (review oracle, Opus 5.5), with one read-only helper subagent that
compared the runfile splits member by member.
**Session IDs:** review oracle (claude-opus-5-5), headless `ganda task work`; helper agent a119196f51de86fd2.

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`

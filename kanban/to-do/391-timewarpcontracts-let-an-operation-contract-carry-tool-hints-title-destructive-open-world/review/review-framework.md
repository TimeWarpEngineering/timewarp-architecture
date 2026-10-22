# Review framework

## Budget (by-diff)

- Lines changed: 158
- Effort: 1
- TCB hits: none
- Roster axes: general
- Turn cap: 80 (--max-turns; cursor uncapped)

## Round 1 scope

- **Date:** 2026-10-22
- **Host task:** kanban/to-do/391-timewarpcontracts-let-an-operation-contract-carry-tool-hints-title-destructive-open-world/
- **Diff scope:** branch `task/391-timewarpcontracts-let-an-operation-contract-carry` vs `master` (head `0c50a297a`): `source/`, `skills/`, `tests/`
- **Plan / brief:** task.md `## Decision` and `## Goal (implementer restatement)`
- **Effort:** 3 (by-diff), roster axis general
- **Reviewer roster:** general (review oracle, claude-opus-5-5) plus an independent general read (sub-agent, sonnet), merged into `round-1/general.md`
- **Session IDs:** review oracle session not reported; sub-agent `abb85a851a96d8e80`

## Ground rules

- Reviewers are read-only on product code. They write only under `review/round-N/`
- Severity is bug, suggestion, or nit. Status starts as open
- Do not invent issues. Zero issues is a valid outcome
- Address the diff and the call sites around it. Re-verify every falsifiable claim against the repo
- Prior rounds are immutable. New work goes in `round-(N+1)/`

## Round 2 scope

- **Date:** 2026-10-22
- **Diff scope:** the fix delta `598e03177..13dd8cef6` (docs in `source/foundation/timewarp-contracts/` and `skills/tw-web-api-contracts/`, tests in `tests/analyzers/timewarp-architecture-sourcegenerator-tests/`, and `task.md`), plus a re-check of round 1 M1–M4
- **Effort:** 1 (by-diff, 158 lines), roster axis general
- **Reviewer roster:** general (review oracle, claude-opus-5-5). No sub-agent
- **Session IDs:** review oracle session not reported

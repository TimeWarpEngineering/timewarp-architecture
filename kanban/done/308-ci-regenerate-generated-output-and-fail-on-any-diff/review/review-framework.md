# Review framework

## Budget (by-diff)

- Lines changed: 594
- Effort: 2
- TCB hits: none
- Roster axes: general
- Turn cap: 120 (--max-turns; cursor uncapped)

## Scope — task 308

**Date:** 2026-10-17
**Host task:** kanban/to-do/308-ci-regenerate-generated-output-and-fail-on-any-diff/
**Diff scope:** commit `ee13467a9` vs its parent `8afdf4fbe` (merge of #533). The branch is stacked on
`feature/agent-surface-hardening`, so `master...HEAD` also carries tasks 343-001, 371, and 378. Those
were reviewed on their own ids and are out of scope here.
**Plan / brief:** `## Goal (implementer restatement)` and the task Requirements in `task.md`.
**Effort:** 2 (by-diff budget), roster general only
**Reviewer roster:** general (review oracle, single pass, no sub-agents)
**Session IDs:** review oracle claude-opus-5-5, session not exposed in print mode

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`

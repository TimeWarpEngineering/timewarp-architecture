# Review framework

## Budget (by-diff)

- Lines changed: 327
- Effort: 2
- TCB hits: none
- Roster axes: general
- Turn cap: 120 (--max-turns; cursor uncapped)

# Review framework — task 283

**Date:** 2026-10-06
**Host task:** kanban/to-do/283-update-timewarpnuru-to-300-beta79-and-amuru-to-200-beta1-audit-nuru-check/
**Diff scope:** branch task/283-… vs master (commit 390f732a5)
**Plan / brief:** bump Nuru 3.0.0-beta.79 + Amuru 2.0.0-beta.1 in CPM; adapt dev-cli and agent-identity-cli handlers to the Mediator `Task<Unit>` / `TimeWarp.Mediator.Unit` surface
**Effort:** 2 (by-diff budget); roster axes: general
**Reviewer roster:** general (review oracle, headless claude)
**Session IDs:** headless ganda task-work review oracle

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`

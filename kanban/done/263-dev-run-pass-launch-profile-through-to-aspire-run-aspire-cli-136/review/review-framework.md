# Review framework

## Budget (by-diff)

- Lines changed: 357
- Effort: 2
- TCB hits: none
- Roster axes: general
- Turn cap: 120 (--max-turns; cursor uncapped)

# Review framework — task 263

**Date:** 2026-10-01
**Host task:** kanban/to-do/263-dev-run-pass-launch-profile-through-to-aspire-run-aspire-cli-136/
**Diff scope:** branch task/263-dev-run-pass-launch-profile-through-to-aspire-run vs master (770ec808)
**Plan / brief:** task.md Requirements 1–6 (`dev run -lp` pass-through, profile validation, CLI version guard, env precedence in Design region)
**Effort:** 2 (budget by-diff); roster axes: general
**Reviewer roster:** general (review oracle, claude-opus-5-5, headless)
**Session IDs:** headless ganda task-work review oracle (no vendor id surfaced)

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`

# Review framework

## Budget (by-diff)

- Lines changed: 575
- Effort: 2
- TCB hits: none
- Roster axes: general
- Turn cap: 120 (--max-turns; cursor uncapped)

# Review framework — task 355

**Date:** 2026-10-14
**Host task:** kanban/to-do/355-ask-composer-edit-mode-pill-with-a-two-option-popover-replacing-the-edit-behavior-row/
**Diff scope:** commit 5db62ea8b vs 5db62ea8b~1 (branch task/355-… vs master)
**Plan / brief:** task.md Requirements: edit-mode pill + FluentMenu radio popover replacing the Edit behavior row; keyboard/ARIA; SetEditMode reused; tests.
**Effort:** 2 (by-diff budget); roster axes: general
**Reviewer roster:** general (Claude Sonnet subagent), orchestrator verification (review oracle, claude-opus-5-5)
**Session IDs:** review oracle session not reported; general subagent afd88d563babaf134

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`

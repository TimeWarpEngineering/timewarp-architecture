# Review framework

## Budget (by-diff)

- Lines changed: 80
- Effort: 1
- TCB hits: none
- Roster axes: general
- Turn cap: 80 (--max-turns; cursor uncapped)

# Review framework — task 285

**Date:** 2026-10-08
**Host task:** kanban/to-do/285-update-aspire-to-1361/
**Diff scope:** branch task/285-update-aspire-to-1361 vs master (commit 59e6d7434)
**Plan / brief:** bump CPM Aspire pins, AppHost SDK and CI Aspire.Cli 13.6.0 → 13.6.1; previews → 13.6.1-preview.1.26506.6
**Effort:** 1 (general only)
**Reviewer roster:** general
**Session IDs:** ganda task work review oracle (Claude Opus 5.5, headless)

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`

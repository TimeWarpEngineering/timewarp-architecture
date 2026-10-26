# Review framework

## Budget (by-diff)

- Lines changed: 207
- Effort: 2
- TCB hits: none
- Roster axes: general
- Turn cap: 120 (--max-turns; cursor uncapped)

# Review framework — task 419

**Date:** 2026-10-26
**Host task:** kanban/in-progress/419-update-aspire-to-1364/
**Diff scope:** branch `task/419-update-aspire-to-1364` vs `master` (merge-base `c9f5011f9`), commit `2a0df5417`
**Plan / brief:** patch bump Aspire 13.6.2 → 13.6.4 through `<AspireVersion>`, the preview suffixes move to `-preview.1.26523.11`, the aca-neon reference is re-verified and re-cited, and the AppHost comments are updated
**Effort:** 2 (roster axes: general)
**Reviewer roster:** general
**Session IDs:** review oracle (claude-opus-5-5), session not reported

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`

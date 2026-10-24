# Review framework

## Budget (by-diff)

- Lines changed: 2163
- Effort: 3
- TCB hits: none
- Roster axes: general
- Turn cap: 200 (--max-turns; cursor uncapped)

# Review framework — task 410

**Date:** 2026-10-25
**Host task:** kanban/in-progress/410-server-enforces-the-sign-in-mode-refuse-passkey-sign-in-and-account-creation-outside-passkey-mode/
**Diff scope:** branch `task/410-server-enforces-the-sign-in-mode-refuse-passkey-si` vs merge-base `ab3e326e8` (master); product commit `becbd6929`
**Plan / brief:** task.md Description, Decisions, Checklist, and `## Goal (implementer restatement)`
**Effort:** 3 (by-diff budget above); roster axis general only
**Reviewer roster:** general (review oracle, claude-opus-5-5)
**Session IDs:** review oracle session not reported by host

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`

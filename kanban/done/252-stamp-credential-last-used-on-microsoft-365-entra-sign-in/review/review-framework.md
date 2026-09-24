# Review framework — task 252

**Date:** 2026-09-24
**Host task:** kanban/to-do/252-stamp-credential-last-used-on-microsoft-365-entra-sign-in/
**Diff scope:** branch `task/252-stamp-credential-last-used-on-microsoft-365-entra` vs `master` (commit f9bd432d)
**Plan / brief:** task.md Requirements — stamp `LastUsedAt` via `CredentialUsageRecorder.RecordAsync` on Entra sign-in and link, with a single write shared with the task-250 AccountHint refresh
**Effort:** 1 (general only)
**Reviewer roster:** general
**Session IDs:** headless `ganda task work` review oracle (2026-09-24)

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`

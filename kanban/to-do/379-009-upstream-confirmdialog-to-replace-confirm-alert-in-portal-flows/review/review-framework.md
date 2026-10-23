# Review framework

## Budget (by-diff)

- Lines changed: 750
- Effort: 2
- TCB hits: none
- Roster axes: general
- Turn cap: 120 (--max-turns; cursor uncapped)

## Scope — task 379-009

**Date:** 2026-10-23
**Host task:** kanban/to-do/379-009-upstream-confirmdialog-to-replace-confirm-alert-in-portal-flows/
**Diff scope:** branch `task/379-009-upstream-confirmdialog-to-replace-confirm-alert-in` vs `master`
(commit d6f54b5f6), `source/` and `tests/`
**Plan / brief:** `task.md` Goal (implementer restatement) + Decision + Checklist. ClearXaiApiKey
asks through `IConfirmationService` (FluentUI 5.0.1 `ShowMessageBoxAsync`) from an
`ApiHandler.ConfirmAsync` hook that runs before the per-state semaphore. Also adds a style-guide
Confirmations sample.
**Effort:** 2. Roster axes: general.
**Reviewer roster:** general (review oracle, claude-opus-5-5, inline)
**Session IDs:** review oracle under ganda task work, session id not reported

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`

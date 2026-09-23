# Review framework — task 246

**Date:** 2026-09-23
**Host task:** kanban/in-progress/246-disable-revoke-on-the-last-active-credential-with-an-inline-reason/
**Diff scope:** branch `task/246-disable-revoke-on-the-last-active-credential-with` vs merge-base `ac506993` (origin/master) — implementation commit `c3cc64bf` (10 files: SPA `CredentialList` / `PasskeysPage` / `SettingsPage`, `CredentialsState` + revoke action, new SPA integration test host + guard tests, deep-link prerender facts)
**Plan / brief:** task.md Requirements — disable Revoke on the last active credential (count = server count across all `CredentialType`s), visible inline hint, Delete → Revoke vocabulary, SPA coverage (1 → disabled, 2 mixed → enabled, flips after revoke), server 409 guard untouched
**Effort:** 1 (general only)
**Reviewer roster:** general
**Session IDs:** review oracle — headless `ganda task work 246` (Claude Fable 5.1), 2026-09-23; general reviewer — Claude subagent spawned by the oracle

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`

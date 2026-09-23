# Review framework — task 248-001

**Date:** 2026-09-23
**Host task:** kanban/to-do/248-001-credential-nickname-registration-context-and-fingerprint/
**Diff scope:** branch `task/248-001-credential-nickname-registration-context-and-finge` vs merge-base `e3bb24b7` (origin/master); commits `4ff46167`, `e8428537` — 55 files
**Plan / brief:** task.md Requirements — credential `Nickname` + `RenameCredential` endpoint, registration context (`RegisteredWith`: attachment/browser/OS, raw UA never stored), `Fingerprint` on `GetCredentials.CredentialSummary` (no material on the wire), `CredentialList` row + inline rename + restating revoke confirmation, co-located Jaribu + integration + SPA presenter tests, EF mapping + postgres migration.
**Effort:** 1 (general only)
**Reviewer roster:** general
**Session IDs:** review oracle — ganda task work (Claude Fable 5.1), 2026-09-23; reviewer subagent spawned from that session

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`

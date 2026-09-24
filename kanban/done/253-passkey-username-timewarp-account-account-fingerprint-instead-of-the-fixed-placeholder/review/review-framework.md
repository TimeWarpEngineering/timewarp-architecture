# Review framework — task 253

**Date:** 2026-09-24
**Host task:** kanban/to-do/253-passkey-username-timewarp-account-account-fingerprint-instead-of-the-fixed-placeholder/
**Diff scope:** branch task/253-passkey-username-timewarp-account-account-fingerpr, commit 188466a1 vs 36723499 (source/ + tests/)
**Plan / brief:** task.md Requirements — PrincipalFingerprint, "TimeWarp account · <fp>" WebAuthn user name, principal id pre-allocated with the registration challenge, ForCurrentAccount for Settings add-passkey, Settings signed-in line.
**Effort:** 1 (general only)
**Reviewer roster:** general
**Session IDs:** ganda task work review oracle (Claude Opus 5.5, headless)

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`

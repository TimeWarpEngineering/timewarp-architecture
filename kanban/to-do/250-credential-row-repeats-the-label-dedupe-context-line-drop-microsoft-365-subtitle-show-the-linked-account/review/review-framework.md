# Review framework — task 250

**Date:** 2026-09-24
**Host task:** kanban/to-do/250-credential-row-repeats-the-label-dedupe-context-line-drop-microsoft-365-subtitle-show-the-linked-account/
**Diff scope:** branch task/250-credential-row-repeats-the-label-dedupe-context-li vs master (da44809e)
**Plan / brief:** task.md — dedupe credential row title/context/confirmation, drop the Settings
Microsoft 365 subtitle, store Entra preferred_username as display-only Credential.AccountHint
(EF + migration), expose on GetCredentials.CredentialSummary.
**Effort:** 1 (general only)
**Reviewer roster:** general
**Session IDs:** review oracle (Claude Opus 5.5, ganda task work); general reviewer subagent af5d2b13e3299e363

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`

# Review framework — task 248-002

**Date:** 2026-09-23
**Host task:** kanban/in-progress/248-002-credential-last-used-timestamp-written-on-every-successful-authentication/
**Diff scope:** branch `task/248-002-credential-last-used-timestamp-written-on-every-su` vs `master` (commits 126762e6 feat + ce6b8061 docs); product code, tests, EF migration, SPA presenter/list
**Plan / brief:** task.md Requirements — `Credential.LastUsedAt` + `MarkUsed(now)`; write points at passkey sign-in, agent-token issuance, and coalesced per-request bearer validation (5-minute window via `CredentialUsageRecorder`); advisory last-used never fights revoke (lost race dropped); EF mapping + migration + in-memory parity; `GetCredentials.CredentialSummary.LastUsedAt`; `CredentialList` "Last used <relative>" / "Never used" + revoke confirmation; co-located Jaribu tests
**Effort:** 1 (general only)
**Reviewer roster:** general
**Session IDs:** review oracle — `ganda task work 248-002` headless (Claude Fable 5.1); general reviewer — Claude sub-agent (sonnet)

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`

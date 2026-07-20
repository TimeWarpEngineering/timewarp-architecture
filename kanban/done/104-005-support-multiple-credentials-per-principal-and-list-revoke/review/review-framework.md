# Review framework — task 104-005

**Date:** 2026-07-20
**Host task:** kanban/in-progress/104-005-support-multiple-credentials-per-principal-and-list-revoke/
**Diff scope:** commit 3b0b52b4 ("feat(identity): multiple credentials per principal — list, revoke, authenticated add (104-005)") vs its parent, on branch dev — 21 files
**Plan / brief:** task.md Notes "Implementation plan (2026-07-20)" — either-scheme credential-management policy + credential:manage scope, ICurrentPrincipalAccessor, list/revoke/add-passkey/add-agent-key, IDOR ownership rule, last-credential guard, 104-028 revoke retry loop
**Effort:** 2 — general + security (self-service credential management is the most security-sensitive endpoint set in the program; IDOR, key-material leakage, privilege escalation, lockout, and concurrency all in scope)
**Reviewer roster:** general, security
**Session IDs:** orchestrator 78b9f414/00b5f23f; build agent a2ef2354b0fc976e7; reviewers a31739ea747756530 (general), security-reviewer-104-005 (fresh security agent)

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`

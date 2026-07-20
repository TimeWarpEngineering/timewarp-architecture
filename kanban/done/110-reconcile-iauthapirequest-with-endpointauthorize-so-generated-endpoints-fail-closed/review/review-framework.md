# Review framework — task 110

**Date:** 2026-07-20
**Host task:** kanban/in-progress/110-reconcile-iauthapirequest-with-endpointauthorize-so-generated-endpoints-fail-closed/
**Diff scope:** commit 44fd802f ("feat(analyzers): fail-closed generated endpoint auth with explicit posture markers (110)") vs its parent, on branch dev — 39 files
**Plan / brief:** task.md Notes "Implementation plan (2026-07-20)" — [EndpointAllowAnonymous(reason)], generator fail-closed default, TWA0013/0014, identity-session-authenticated policy on roles CRUD, 20 contracts annotated, raw-HTTP auth tests
**Effort:** 2 — general + security (auth posture across every generated endpoint warrants the security lens)
**Reviewer roster:** general, security
**Session IDs:** orchestrator 78b9f414/00b5f23f; build agent a2ef2354b0fc976e7; reviewers a31739ea747756530 (general), security-reviewer-104-003 (security, continuing agent)

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`

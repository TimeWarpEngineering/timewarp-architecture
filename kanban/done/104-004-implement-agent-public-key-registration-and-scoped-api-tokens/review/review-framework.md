# Review framework — task 104-004

**Date:** 2026-07-20
**Host task:** kanban/in-progress/104-004-implement-agent-public-key-registration-and-scoped-api-tokens/
**Diff scope:** commit 16beaa46 ("feat(identity): agent public-key registration and scoped API tokens (104-004)") vs its parent, on branch dev — 50 files
**Plan / brief:** task.md Notes "Implementation plan (2026-07-20)" — ES256/SPKI agent-key proof ceremonies, opaque store-backed scoped bearer tokens, named agent-token scheme + scope policy, challenge-store core refactor
**Effort:** 2 — general + security (signature verifier + bearer auth scheme warrant the security lens, same rationale as 104-003)
**Reviewer roster:** general, security
**Session IDs:** orchestrator 78b9f414-b92e-4554-9795-d8fa114bdb26; build agent a2ef2354b0fc976e7; reviewers a31739ea747756530 (general), security-reviewer-104-003 (security, continuing agent)

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`

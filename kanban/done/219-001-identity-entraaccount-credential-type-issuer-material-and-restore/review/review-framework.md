# Review framework — task 219-001

**Date:** 2026-09-14
**Host task:** kanban/in-progress/219-001-identity-entraaccount-credential-type-issuer-material-and-restore/
**Diff scope:** branch `task/219-001-identity-entraaccount-credential-type-issuer-mater` vs `origin/master` (commit `51bed9ff` — `feat(identity): add EntraAccount credential type, issuer material, and Restore`). Product files under `source/libraries/timewarp-identity/` and `tests/libraries/timewarp-identity-tests/`. Kitchen `task.md` is in the same commit.
**Plan / brief:** Fold-in of RFC 219 Decision 2 A′: `CredentialType.EntraAccount = 3`, `EntraAccountHandle` / `EntraIssuerMaterial`, type-dependent `PublicMaterial` Design, `Credential.Restore()`. No Graph, OIDC, or template ceremony (219-002). Store port unchanged.
**Effort:** 1 (general only)
**Reviewer roster:** general
**Session IDs:** Review oracle: grok session 01a0a037-48fd-7190-9f39-8fea7589d855 (2026-09-14). General reviewer: grok session 01a0a038-ce46-7380-a110-8516afbabdc0 (2026-09-14).

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`

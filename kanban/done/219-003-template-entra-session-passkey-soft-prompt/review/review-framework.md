# Review framework — task 219-003

**Date:** 2026-09-15
**Host task:** kanban/in-progress/219-003-template-entra-session-passkey-soft-prompt/
**Diff scope:** branch `task/219-003-template-entra-session-passkey-soft-prompt` vs `origin/master` (commit `f62f386c` — `feat(identity): Entra-session passkey soft-prompt after login`). Product files under `source/container-apps/web/projects/web-spa/` and `tests/container-apps/web/web-spa-integration-tests/features/identity/passkey-soft-prompt-tests.cs`. Kitchen `task.md` is in the same commit.
**Plan / brief:** Fold-in of RFC 219 **D8** (098-006 shape). After an Entra-issued identity-session, if `GetCredentials` has no `Passkey`, show a dismissible “Add a passkey” prompt that drives existing `AddPasskey`. Never a gate. Detect via `GetCredentials` Type list — not a new TrustTier, not quarantine. Banner on post-login Home/Profile via `TimeWarpPage`. Tests: Entra-only sees prompt; principal with passkey does not; dismissing does not block routes.
**Effort:** 1 (general only)
**Reviewer roster:** general
**Session IDs:** Review oracle: grok session 01a0a361-b864-7282-8b40-1de5e59f768a (2026-09-15). General reviewer (round 1): grok session 01a0a364-3754-7742-9398-20e07617c514 (2026-09-15).

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`

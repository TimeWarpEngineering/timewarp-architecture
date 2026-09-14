# Review framework — task 219-002

**Date:** 2026-09-14
**Host task:** kanban/in-progress/219-002-template-named-entra-scheme-challenge-bootstrap-and-link-retire-useentra-default/
**Diff scope:** branch `task/219-002-template-named-entra-scheme-challenge-bootstrap-an` vs `origin/master` (commit `82d5b714` — `feat(identity): named entra OIDC scheme, challenge bootstrap/link, retire UseEntra default`). Product files under `source/container-apps/web/` and `tests/container-apps/web/web-server-integration-tests/features/identity/`. Kitchen `task.md` is in the same commit. `Directory.Packages.props` adds `Microsoft.AspNetCore.Authentication.OpenIdConnect`.
**Plan / brief:** Fold-in of RFC 219 fork 1 mechanics + D4 + D10. Register Entra as a named OIDC scheme (`entra`), never DefaultScheme. identity-session stays default. Challenge for bootstrap (anonymous, trusted tenant) and link (authenticated). Retire the 104-021 `Authentication:UseEntra` default-scheme branch. No WASM MSAL session. Tests without a live tenant (fake OIDC handler).
**Effort:** 1 (general only)
**Reviewer roster:** general
**Session IDs:** Review oracle: grok session 01a0a073-d0b7-7181-ae75-f6fb461d33dc (2026-09-14). General reviewer (round 1): grok session 01a0a077-309b-71b2-b3e0-cf099835b798 (2026-09-14). Fix-loop implementer: grok session 01a0a07c-ad2c-79c2-89e4-b6b2ea849ca2 (2026-09-14). General reviewer (round 2): grok session 01a0a084-1831-7792-8a7d-d7ca486fcb83 (2026-09-14).

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`

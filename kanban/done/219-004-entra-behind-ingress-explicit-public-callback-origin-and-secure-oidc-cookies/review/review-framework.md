# Review framework — task 219-004

**Date:** 2026-09-15
**Host task:** kanban/to-do/219-004-entra-behind-ingress-explicit-public-callback-origin-and-secure-oidc-cookies/
**Diff scope:** branch `task/219-004-entra-behind-ingress-explicit-public-callback-orig` vs `origin/master` (product commit `d39c0dcb` — `feat(identity): Entra PublicOrigin and always-secure OIDC cookies behind ingress`; kitchen Results in `70a32ecc`). Product files under `source/container-apps/` and `tests/container-apps/web/web-server-integration-tests/features/identity/`. Kitchen `task.md` is in the same branch (do not treat kitchen-only edits as product defects).
**Plan / brief:** Named `entra` OIDC scheme behind YARP/ACA without `UseForwardedHeaders`. Optional `Authentication:Entra:PublicOrigin` overrides challenge and code-redemption `redirect_uri`; correlation and nonce cookies are `CookieSecurePolicy.Always`. Direct launch with PublicOrigin unset stays request-derived. Do not consume `X-Forwarded-*`. Do not default from AppHost `Ingress:PublicUrl`.
**Effort:** 1 (general only)
**Reviewer roster:** general
**Session IDs:** Review oracle: grok session 01a0a4f1-e56d-7471-b0e1-fb58eb45d657 (2026-09-15). General reviewer (round 1): grok session 01a0a4f4-43f1-7ec2-9373-d01399b7f0f6 (2026-09-15). General reviewer (round 2): grok session 01a0a4fc-ae9c-7920-8d7d-fc8d17f650bc (2026-09-15).

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`

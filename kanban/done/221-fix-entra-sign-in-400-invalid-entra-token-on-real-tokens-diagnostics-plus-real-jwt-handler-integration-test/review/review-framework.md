# Review framework — task 221

**Date:** 2026-09-16
**Host task:** kanban/to-do/221-fix-entra-sign-in-400-invalid-entra-token-on-real-tokens-diagnostics-plus-real-jwt-handler-integration-test/
**Diff scope:** branch `task/221-fix-entra-sign-in-400-invalid-entra-token-on-real` vs `origin/master` (commits `f4d99c41` fix + `5978b428` Results). Product: keep `iss` on the named `entra` OIDC principal (`ClaimActions.Remove("iss")` + `OnTokenValidated` copy from `SecurityToken.Issuer`), stash local return path on `AuthenticationProperties.Items`, `TryRead` first-failing-check reasons, named 400 details, types-only Warning logs, boot informational-version stamp, real-JWT `OpenIdConnectHandler` round-trip test.
**Plan / brief:** Real Microsoft 365 `/signin-oidc` returned 400 Invalid Entra token. Root cause recorded in Results: default `ClaimActions.DeleteClaim("iss")` strips issuer after `OnTokenValidated`. Diagnostics ship regardless. See `task.md` Requirements.
**Effort:** 1 (general only)
**Reviewer roster:** general
**Session IDs:** Review oracle: grok session 01a0a7a3-58b6-7202-b7ab-7bb30d15542c (2026-09-16). General reviewer (round 1): grok session 01a0a7a5-e6ed-7a13-b145-4509db992e40 (2026-09-16).

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`

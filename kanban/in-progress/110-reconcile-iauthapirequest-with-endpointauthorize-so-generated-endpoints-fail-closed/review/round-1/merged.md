# Round 1 — merged findings
**Date:** 2026-07-20
**Sources:** general, security

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 1 | 0 | 0 |
| suggestion | 2 | 0 | 0 |
| nit | 2 | 0 | 0 |

Full descriptions: `general.md` / `security.md`. No overlap between reviewers (different lenses).
**Security verdict: the original 109 finding (admin CRUD generated-anonymous) is GENUINELY CLOSED** —
all 20 [ApiEndpoint] contracts carry exactly one posture marker, roles CRUD emit
`Policies("identity-session-authenticated")`, fail-closed default + both-markers precedence +
scheme isolation + no analyzer-evasion path all verified; 401/200 roles tests non-vacuous.
General: cleanest commit of the series (generator↔analyzer nested-request discovery identical, no
drift; docs accurate; suites green 82/40/56+1).

## Issues

### M1 — Severity: bug — Status: open
- File: source/container-apps/web/web-contracts/features/auth/queries/get-sign-in-token.cs:23 (handler get-sign-in-token-handler.cs:33-40)
- Description: `get-sign-in-token` is now stamped `[EndpointAllowAnonymous]` but its handler mints a real Passwordless sign-in token for an **arbitrary caller-supplied `UserId` with no proof of identity** — an account-takeover primitive, live in any configured instance (AddPasswordlessSdk throws at boot without a secret, so not merely dormant). The anonymous marker blesses an auth-bypass endpoint as intentionally public. Pre-existing (not a 110 regression) but the diff must not silently accept it.
- Consumer check (orchestrator): NO live consumer — the SPA `PasswordlessService` calls the Passwordless.dev SaaS (`ApiUrl/create-token`), not this `/api/signin-token` route; grep finds zero callers of the contract/route in web-spa. Safe to remove from the server surface.
- Suggestion (fix in 110, security-in-scope): remove `[ApiEndpoint]`, mark `[ClientOnlyContract(reason)]` (satisfies TWA0006, stops generation); remove the now-dead `/api/signin-token` YARP route in aspire-app-host/program.cs; leave the handler for 104-016/021 full legacy retirement with a note. Do NOT leave a reachable anonymous token-minting endpoint.
- Source: security
- Disposition notes:

### M2 — Severity: suggestion — Status: open
- File: source/container-apps/web/web-contracts/features/admin/roles/commands/create-role.cs:23 (+ update/delete/get-role, get-roles; constant at web-server/configuration/identity-session-defaults.cs:30)
- Description: Policy name `"identity-session-authenticated"` now lives as six comment-coordinated string literals across two assemblies (contract literal + server constant), agreeing only by convention — fail-closed and test-caught for the current five, but a prefer-analyzers-directive candidate for a build-time agreement check on future policies.
- Suggestion: File a follow-up task for a TWA policy-name-agreement analyzer (do not expand 110's scope with a new analyzer). Document the coupling in the meantime.
- Source: general
- Disposition notes:

### M3 — Severity: suggestion — Status: open
- File: tests/container-apps/web/web-server-integration-tests/Features/Admin/Roles/Roles_Authorization_Tests.cs:44-90
- Description: No test proves the cross-scheme negative — an agent-token bearer must be REJECTED by the identity-session cookie policy on api/Roles. Isolation holds by construction (AddAuthenticationSchemes restricts to the cookie scheme) but 104-004 tested this property; the new consumer should too.
- Suggestion: Add a test: mint an agent bearer token (agent ceremony helper), call api/Roles with `Authorization: Bearer …` and no cookie → 401 (bearer does not satisfy the cookie policy).
- Source: security
- Disposition notes:

### M4 — Severity: nit — Status: open
- File: source/container-apps/web/web-server/program.cs:199-234 (ConfigureAuthentication) vs the fail-closed default
- Description: The bare fail-closed default (no marker → no auth config) challenges via the dormant Entra default scheme (302/500, not a clean 401). Deny still holds and it's unreachable under TWA0013; the clean 401 is a property of the roles policy's explicit scheme restriction, not the bare default.
- Suggestion: One Design-region sentence recording that a no-marker endpoint (only reachable if TWA0013 is suppressed) denies via the default scheme, not a clean 401 — clean 401 requires an explicit scheme-restricted policy. Optional: no code change.
- Source: security
- Disposition notes:

### M5 — Severity: nit — Status: open
- File: source/analyzers/timewarp-architecture-convention-analyzers/endpoint-auth-posture-analyzer.cs:64-74 (messageFormat line 69)
- Description: TWA0014's `messageFormat: "{0}"` passthrough deviates from every sibling analyzer's structured-format convention.
- Suggestion: Use a structured messageFormat with named placeholders like the sibling analyzers.
- Source: general
- Disposition notes:

## Duplicates / conflicts

- None — general and security lenses did not overlap.

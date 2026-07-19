# Round 1 — merged findings
**Date:** 2026-07-20
**Sources:** general, security

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 2 | 0 | 0 |
| suggestion | 1 | 0 | 0 |
| nit | 2 | 0 | 0 |

Full descriptions: `general.md` / `security.md`. One overlap collapsed (null-scopes NRE:
general#1 + security#1 → M1, bug severity kept). Security verdict: no auth bypass, no
verification gap — SPKI guards, domain separation, DER-only signatures, token hygiene, scheme
isolation all verified sound with honest negative tests. General verified the challenge-store
refactor behavior-preserving and every 104-003 lesson genuinely applied; suites re-run green
(168 / 21 / 52+1).

## Issues

### M1 — Severity: bug — Status: open
- File: web-contracts/features/identity/commands/complete-agent-token-issuance.cs:50-51 (via foundation FluentValidationBehavior; handler re-deref at complete-agent-token-issuance-handler.cs:79)
- Description: `"scopes": null` from any anonymous caller → STJ overwrites the `= []` initializer; FluentValidation default Continue cascade runs `.Must(scopes => scopes.Count <= 16)` after NotEmpty fails → NullReferenceException → unhandled 500 (machine-readable-errors violation; dev stack-trace leak; same defect class as 104-003 M9). Reproduced empirically on FluentValidation 12.1.1.
- Suggestion: `.NotNull()` + null-safe predicate (or `RuleFor(...).Cascade(CascadeMode.Stop)`); handler defense-in-depth null check; integration test posting `"scopes": null` expecting 400.
- Source: general, security
- Disposition notes:

### M2 — Severity: bug — Status: open
- File: source/libraries/timewarp-identity/tokens/i-agent-token-store.cs:12-15 (echoed at agent-token-authentication-handler.cs:21-23)
- Description: Port Design region claims "Validate re-reads the principal on every call" — false (store has no IPrincipalStore access); quarantine cutoff lives solely in the web-server auth handler. Load-bearing misattribution: the same paragraph names 104-013 settle-time as a future direct-port consumer, which would silently inherit NO cutoff.
- Suggestion: Rewrite the Design region to state the real division (store = expiry/grant only; liveness/quarantine = caller's responsibility) and flag it explicitly for 104-013.
- Source: general
- Disposition notes:

### M3 — Severity: suggestion — Status: open
- File: source/libraries/timewarp-identity/tokens/in-memory-agent-token-store.cs:52-55, 97-115 (Design region :17-20)
- Description: At-cap EvictOldest can silently drop a still-VALID token under flood — within the accepted rate-limit-deferred posture, but the Design region doesn't state that valid grants can be evicted.
- Suggestion: One Design-region sentence making the valid-token-eviction consequence explicit (agent recovers by re-running the token ceremony).
- Source: security
- Disposition notes:

### M4 — Severity: nit — Status: open
- File: web-application/features/identity/complete-agent-token-issuance-handler.cs:100-103
- Description: Duplicate scope entries propagate uncanonicalized into grant/claims/response.
- Suggestion: Distinct() (ordinal) before Issue; document canonical form.
- Source: general
- Disposition notes:

### M5 — Severity: nit — Status: open
- File: tests/.../Agent_Token_Tests.cs:49-61 (vs :64-90)
- Description: Unknown-KeyId test asserts only Status 400, not Title — the no-enumeration-oracle equivalence with the bad-signature 400 is only half-pinned (code paths are identical today).
- Suggestion: Assert Title equality across the two rejection shapes.
- Source: security
- Disposition notes:

## Duplicates / conflicts

- general#1 + security#1 → M1 (bug kept — general's severity; security rated suggestion on bounded impact but agreed on the defect).
- No other overlaps.

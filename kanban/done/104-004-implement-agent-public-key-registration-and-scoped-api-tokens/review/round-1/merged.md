# Round 1 — merged findings
**Date:** 2026-07-20
**Sources:** general, security

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 2 | 0 |
| suggestion | 0 | 1 | 0 |
| nit | 0 | 2 | 0 |

Full descriptions: `general.md` / `security.md`. One overlap collapsed (null-scopes NRE:
general#1 + security#1 → M1, bug severity kept). Security verdict: no auth bypass, no
verification gap — SPKI guards, domain separation, DER-only signatures, token hygiene, scheme
isolation all verified sound with honest negative tests. General verified the challenge-store
refactor behavior-preserving and every 104-003 lesson genuinely applied; suites re-run green
(168 / 21 / 52+1).

## Issues

### M1 — Severity: bug — Status: fixed
- File: web-contracts/features/identity/commands/complete-agent-token-issuance.cs:50-51 (via foundation FluentValidationBehavior; handler re-deref at complete-agent-token-issuance-handler.cs:79)
- Description: `"scopes": null` from any anonymous caller → STJ overwrites the `= []` initializer; FluentValidation default Continue cascade runs `.Must(scopes => scopes.Count <= 16)` after NotEmpty fails → NullReferenceException → unhandled 500 (machine-readable-errors violation; dev stack-trace leak; same defect class as 104-003 M9). Reproduced empirically on FluentValidation 12.1.1.
- Suggestion: `.NotNull()` + null-safe predicate (or `RuleFor(...).Cascade(CascadeMode.Stop)`); handler defense-in-depth null check; integration test posting `"scopes": null` expecting 400.
- Source: general, security
- Disposition notes: Fixed with all three layers, not just one: `RuleFor(x => x.Scopes)` now chains
  `Cascade(CascadeMode.Stop).NotNull().NotEmpty().Must(scopes => scopes is null || scopes.Count <= 16)`
  — Cascade.Stop means a failed NotNull short-circuits the rest of the chain, AND the Must predicate
  is independently null-safe as defense-in-depth. Confirmed `RuleForEach` already handles a null
  collection gracefully (no fix needed there). Added a handler-level null/empty guard in
  `complete-agent-token-issuance-handler.cs` before the `.Where` call (belt-and-suspenders in case
  the validation pipeline is ever bypassed). Added integration test
  `Agent_Token_Tests.ValidationError_Given_Null_Scopes` posting a Command with `Scopes = null!`
  through the real serializer (confirmed `ContractSerializationDefaults` has no
  `DefaultIgnoreCondition`, so this reaches the wire as literal `"scopes":null`, not an omitted
  property) and asserting a clean 400 via `ConfirmEndpointValidationError`.
  **Cross-command audit (as requested):** grepped every identity contract in both `web-contracts/
  features/identity/commands/` and `queries/` for `Must(` and collection-typed request properties —
  the pattern exists in EXACTLY ONE place, this rule. The other three 104-004 commands
  (`complete-agent-key-registration`, `start-agent-key-registration`, `start-agent-token-issuance`)
  have no collection properties at all. All four 104-003 passkey commands were also checked: none
  declare a `List<>`/collection property or a `Must()` predicate — the only nullable field among them
  (`CompletePasskeyAuthentication.Command.UserHandle`, a `string?`) uses `MaximumLength`, which
  FluentValidation's length validators treat as valid on null by design (confirmed by both
  reviewers). No fix was needed in the passkey contracts.

### M2 — Severity: bug — Status: fixed
- File: source/libraries/timewarp-identity/tokens/i-agent-token-store.cs:12-15 (echoed at agent-token-authentication-handler.cs:21-23)
- Description: Port Design region claims "Validate re-reads the principal on every call" — false (store has no IPrincipalStore access); quarantine cutoff lives solely in the web-server auth handler. Load-bearing misattribution: the same paragraph names 104-013 settle-time as a future direct-port consumer, which would silently inherit NO cutoff.
- Suggestion: Rewrite the Design region to state the real division (store = expiry/grant only; liveness/quarantine = caller's responsibility) and flag it explicitly for 104-013.
- Source: general
- Disposition notes: Rewrote `i-agent-token-store.cs`'s Design region: states plainly that Validate
  does not and cannot re-read the principal (no IPrincipalStore dependency by design), that the
  liveness re-read lives one layer up in `AgentTokenAuthenticationHandler`, and explicitly warns any
  FUTURE caller (named: 104-013 settle-time, a future api-server bearer validator) that calling
  `Validate` alone gets no quarantine cutoff — it must independently re-read the principal. Added the
  same warning to the `Validate` XML doc comment. Echoed a corrected, cross-referencing line in
  `agent-token-authentication-handler.cs`'s Design region (no longer claims the store does the
  re-read; points to the corrected port doc for the full division of responsibility).

### M3 — Severity: suggestion — Status: fixed
- File: source/libraries/timewarp-identity/tokens/in-memory-agent-token-store.cs:52-55, 97-115 (Design region :17-20)
- Description: At-cap EvictOldest can silently drop a still-VALID token under flood — within the accepted rate-limit-deferred posture, but the Design region doesn't state that valid grants can be evicted.
- Suggestion: One Design-region sentence making the valid-token-eviction consequence explicit (agent recovers by re-running the token ceremony).
- Source: security
- Disposition notes: Added the consequence to the Design region: at-cap eviction drops the
  soonest-to-expire entry AMONG STILL-VALID grants (PruneExpired already swept anything actually
  expired first), so a legitimate agent can get an early 401 before its stated ExpiresInSeconds;
  recovery is re-running the token-issuance ceremony (the key still works); real capacity limits are
  104-015's job.

### M4 — Severity: nit — Status: fixed
- File: web-application/features/identity/complete-agent-token-issuance-handler.cs:100-103
- Description: Duplicate scope entries propagate uncanonicalized into grant/claims/response.
- Suggestion: Distinct() (ordinal) before Issue; document canonical form.
- Source: general
- Disposition notes: Added `command.Scopes.Distinct(StringComparer.Ordinal).ToList()` immediately
  after the null/empty guard, before the unknown-scope check — the resulting canonical `scopes`
  local (not `command.Scopes`) is what flows into the unknown-scope check, `TokenStore.Issue`, and
  the `Response`. Documented in the handler's Design region.

### M5 — Severity: nit — Status: fixed
- File: tests/.../Agent_Token_Tests.cs:49-61 (vs :64-90)
- Description: Unknown-KeyId test asserts only Status 400, not Title — the no-enumeration-oracle equivalence with the bad-signature 400 is only half-pinned (code paths are identical today).
- Suggestion: Assert Title equality across the two rejection shapes.
- Source: security
- Disposition notes: Added `result.AsT2.Title.ShouldBe("Token issuance failed")` to
  `BadRequest_Given_Unknown_KeyId`, matching the assertion already present in
  `BadRequest_Given_Bad_Signature_Identical_To_Unknown_KeyId` — both branches now pin the identical
  problem shape, not just the status code.

## Duplicates / conflicts

- general#1 + security#1 → M1 (bug kept — general's severity; security rated suggestion on bounded impact but agreed on the defect).
- No other overlaps.

# Round 1 — merged findings
**Date:** 2026-07-19
**Sources:** general, security

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 1 | 0 |
| suggestion | 0 | 5 | 0 |
| nit | 0 | 2 | 0 |

Full descriptions: `general.md` / `security.md`. Two overlapping findings collapsed (dup-registration
race: general#2 + security#3; size caps: general#4 + security#2). Security verified the crypto core
sound (per-ceremony type check, decoded-byte challenge compare, origin/rpIdHash/UP enforcement,
correct signed-data construction, algorithm bound to STORED key — no confusion path, no client-trusted
challenge). General verified contracts/TWA0005-0006, empty-body POST binding, problem-details status
mapping, no-Update* rule, middleware order, cookie flags, Entra untouched, snapshot store usage, SPA
interop rules; suites re-run green (124 / 14 / 31+1).

## Issues

### M1 — Severity: bug — Status: fixed
- File: source/container-apps/web/web-server/appsettings.json:29 (vs web-authn-options.cs:24, program.cs:281)
- Description: appsettings section is "WebAuthn" but AddFluentValidatedOptions binds by type name "WebAuthnOptions" — shipped config never binds (masked because JSON values equal C# defaults). A template consumer's production RpId/AllowedOrigins would be silently dropped, keeping the permissive dev origin-fallback active.
- Suggestion: Rename the JSON section to "WebAuthnOptions" (match SampleOptions precedent) + an integration assertion that bound options reflect appsettings.
- Source: general
- Disposition notes: Renamed appsettings.json section "WebAuthn" → "WebAuthnOptions". Added
  tests/.../WebAuthnOptions_Binding_Tests.cs: test appsettings.json now sets
  WebAuthnOptions:RpName to "Integration Test RP" (distinct from the C# default "TimeWarp
  Architecture") and the test resolves IOptions&lt;WebAuthnOptions&gt; from the running host's DI
  container and asserts the bound value matches — this test fails on the old section name. RpId
  intentionally left unset in test config (must still default to "localhost", asserted too) so
  the fixed test host origin every other identity test relies on is unaffected. Documented the
  binding-key convention in WebAuthnOptions's Design region.

### M2 — Severity: suggestion — Status: fixed
- File: source/container-apps/web/web-application/features/identity/complete-passkey-authentication-handler.cs:79-88 (Design region :10-17)
- Description: Quarantine !IsActive → 403 runs BEFORE signature verification while the Design region claims possession was already proven — factually wrong, and a pre-auth oracle distinguishing quarantined vs active accounts without the key. Latent until 104-005 implements quarantine, but ships now.
- Suggestion: Move the IsActive check after WebAuthnAuthentication.Verify; fix the Design region.
- Source: security
- Disposition notes: Moved `if (!principal.IsActive) return Quarantined();` to run after
  `WebAuthnAuthentication.Verify` succeeds (was before it). Rewrote the Design region to state the
  real ordering and why it matters (the distinct-403 premise "possession already proven" is only
  true once Verify has actually run).

### M3 — Severity: suggestion — Status: fixed
- File: source/container-apps/web/web-application/features/identity/complete-passkey-registration-handler.cs:79-99
- Description: Duplicate-credential check-then-act race: concurrent same-credential registers pass the Find check, then the store's uniqueness throw surfaces as unhandled 500 AND leaves an orphan Principal — contradicting both the contract's documented 409 and the handler Design region's no-orphan claim.
- Suggestion: Catch the store's duplicate-handle exception at AddCredentialAsync, translate to the 409 problem details, and remove the orphan principal (or reorder credential-first with documented rationale).
- Source: general, security
- Disposition notes: Confirmed the exact exception (InMemoryPrincipalStore.AddCredentialAsync throws
  `InvalidOperationException` from the `HandleIndex.TryAdd` collision branch). Wrapped the
  `AddCredentialAsync` call in a `try/catch (InvalidOperationException)` that returns the same
  `CredentialAlreadyRegistered()` 409 the sequential check-then-act path returns — the race can no
  longer surface as a 500. Compensating removal of the orphaned Principal was NOT implemented:
  `IPrincipalStore` has no delete method (adding one is 104-005-scale store-lifecycle work, out of
  this task's bounds) — took the review's offered third option ("accept and document") for the
  orphan half specifically, combined with the catch-and-409 fix for the primary bug. Design region
  now states this precisely (no longer claims a blanket "never leaves an orphan" invariant). A true
  concurrent-race integration test would need real thread interleaving on a shared in-memory store
  and would be inherently flaky; the store-level exception-type contract this catch depends on is
  already deterministically pinned by the existing `Duplicate_type_and_handle_fails` test in
  in-memory-principal-store-tests.cs (same store method, same exception type — that test just uses
  one principal for both credentials rather than two, which the store's HandleIndex logic treats
  identically since it is keyed by (Type, handle) independent of PrincipalId).

### M4 — Severity: suggestion — Status: fixed
- File: web-contracts/features/identity/commands/complete-passkey-authentication.cs:34-44; complete-passkey-registration.cs:28-36
- Description: Size-ceiling validation inconsistent: only registration's AttestationObject is capped; authentication command caps nothing (ClientDataJson/AuthenticatorData/Signature/CredentialId unbounded), registration's other fields also uncapped.
- Suggestion: Apply consistent MaximumLength caps to every base64url payload field in both commands (CredentialId small, e.g. 2KB; payloads 64KB).
- Source: general, security
- Disposition notes: Added `MaximumLength` to every base64url field on both completion commands:
  CredentialId 2KB on both; ClientDataJson/AttestationObject 64KB on registration;
  ClientDataJson/AuthenticatorData/Signature 64KB and UserHandle 2KB on authentication. Added one
  `ValidationError_Given_Oversized_CredentialId` integration test per command (2049-char CredentialId
  → 400 via the existing `ConfirmEndpointValidationError` helper) pinning the new caps; existing
  happy-path tests are unaffected since real WebAuthn payload sizes are far under either ceiling.

### M5 — Severity: suggestion — Status: fixed
- File: source/libraries/timewarp-identity/ceremonies/webauthn/internal/cose-key.cs:142-149
- Description: RSA COSE keys accepted with no minimum modulus size — a 512-bit RSA key registers successfully (self-inflicted weak credential; defense-in-depth).
- Suggestion: Reject RSA moduli < 2048 bits at registration parse (UnsupportedAlgorithm/MalformedData reason); add a vector test.
- Source: security
- Disposition notes: Added `MinimumRsaModulusBits = 2048` constant and a `GetModulusBitLength`
  helper (exact bit length, ignoring leading zero bytes/bits — not a naive `Length * 8`) checked in
  `TryCreateVerifier` before importing an RS256 key; maps to `WebAuthnFailureReason.UnsupportedAlgorithm`
  (same reason class as any other "parseable but this verifier won't use it" key). Added a fixed
  512-bit RSA public-key fixture (`SoftwareAuthenticator.BuildWeakRsaCoseKey`, public components
  only) and a `Weak_rsa_modulus_fails` vector test in webauthn-registration-tests.cs.

### M6 — Severity: suggestion — Status: fixed
- File: tests/container-apps/web/web-server-integration-tests/Features/Identity/Passkey_Registration_Tests.cs:13-17
- Description: Test file's "fresh instance per test method" Design claim is contradicted by Fixie per-class fixture sharing (the singleton stores + the CredentialId-collision fix this very commit documents); practical consequence: HttpClient cookie-container leakage across test methods could mask session assertions.
- Suggestion: Fix the Design comment; isolate cookies per test (fresh HttpClient or explicit cookie clearing) where session state is asserted.
- Source: general
- Disposition notes: Rewrote the Design region in both Passkey_Registration_Tests.cs and
  Passkey_Authentication_Tests.cs to correctly state per-class fixture/HttpClient/cookie-jar
  sharing (referencing the sibling fixture's region that documents the same observation). Added a
  `GetCurrentSessionWithCookie` helper to both files: builds a fresh, disposed-per-call `HttpClient`
  carrying ONLY the specific `Set-Cookie` value the test's own completion response returned, and
  used it in both happy-path tests (registration + authentication) in place of the shared
  ambient-cookie-jar `WebTestServerApplication.GetResponse` call — session assertions no longer
  depend on run order within the class.

### M7 — Severity: nit — Status: fixed
- File: web-contracts/features/identity/queries/get-current-session.cs:29-37
- Description: Response documents IsAuthenticated⇔PrincipalId-pairing invariant its ctor doesn't enforce.
- Suggestion: Enforce in ctor (throw on mismatch) or soften the doc.
- Source: general
- Disposition notes: Enforced in the ctor — throws `ArgumentException` when
  `isAuthenticated != (principalId is not null)`. Updated the Design region to describe the
  enforcement instead of an unenforced claim. Existing round-trip tests (Authenticated/Unauthenticated)
  already construct only agreeing pairs, so no test changes needed.

### M8 — Severity: nit — Status: fixed
- File: tests/.../Passkey_Registration_Tests.cs:132
- Description: Leftover "fixed CredentialId" comment from before the per-instance-random fix.
- Suggestion: Delete/update the comment.
- Source: general
- Disposition notes: Reworded to "Same authenticator instance (same per-instance CredentialId — see
  IntegrationSoftwareAuthenticator's Design region)".

## Duplicates / conflicts

- general#2 + security#3 → M3 (kept suggestion severity; strongest shared framing: 500 + orphan).
- general#4 + security#2 → M4.
- No severity conflicts.

# Round 1 — merged findings
**Date:** 2026-07-19
**Sources:** general, security

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 1 | 0 | 0 |
| suggestion | 5 | 0 | 0 |
| nit | 2 | 0 | 0 |

Full descriptions: `general.md` / `security.md`. Two overlapping findings collapsed (dup-registration
race: general#2 + security#3; size caps: general#4 + security#2). Security verified the crypto core
sound (per-ceremony type check, decoded-byte challenge compare, origin/rpIdHash/UP enforcement,
correct signed-data construction, algorithm bound to STORED key — no confusion path, no client-trusted
challenge). General verified contracts/TWA0005-0006, empty-body POST binding, problem-details status
mapping, no-Update* rule, middleware order, cookie flags, Entra untouched, snapshot store usage, SPA
interop rules; suites re-run green (124 / 14 / 31+1).

## Issues

### M1 — Severity: bug — Status: open
- File: source/container-apps/web/web-server/appsettings.json:29 (vs web-authn-options.cs:24, program.cs:281)
- Description: appsettings section is "WebAuthn" but AddFluentValidatedOptions binds by type name "WebAuthnOptions" — shipped config never binds (masked because JSON values equal C# defaults). A template consumer's production RpId/AllowedOrigins would be silently dropped, keeping the permissive dev origin-fallback active.
- Suggestion: Rename the JSON section to "WebAuthnOptions" (match SampleOptions precedent) + an integration assertion that bound options reflect appsettings.
- Source: general
- Disposition notes:

### M2 — Severity: suggestion — Status: open
- File: source/container-apps/web/web-application/features/identity/complete-passkey-authentication-handler.cs:79-88 (Design region :10-17)
- Description: Quarantine !IsActive → 403 runs BEFORE signature verification while the Design region claims possession was already proven — factually wrong, and a pre-auth oracle distinguishing quarantined vs active accounts without the key. Latent until 104-005 implements quarantine, but ships now.
- Suggestion: Move the IsActive check after WebAuthnAuthentication.Verify; fix the Design region.
- Source: security
- Disposition notes:

### M3 — Severity: suggestion — Status: open
- File: source/container-apps/web/web-application/features/identity/complete-passkey-registration-handler.cs:79-99
- Description: Duplicate-credential check-then-act race: concurrent same-credential registers pass the Find check, then the store's uniqueness throw surfaces as unhandled 500 AND leaves an orphan Principal — contradicting both the contract's documented 409 and the handler Design region's no-orphan claim.
- Suggestion: Catch the store's duplicate-handle exception at AddCredentialAsync, translate to the 409 problem details, and remove the orphan principal (or reorder credential-first with documented rationale).
- Source: general, security
- Disposition notes:

### M4 — Severity: suggestion — Status: open
- File: web-contracts/features/identity/commands/complete-passkey-authentication.cs:34-44; complete-passkey-registration.cs:28-36
- Description: Size-ceiling validation inconsistent: only registration's AttestationObject is capped; authentication command caps nothing (ClientDataJson/AuthenticatorData/Signature/CredentialId unbounded), registration's other fields also uncapped.
- Suggestion: Apply consistent MaximumLength caps to every base64url payload field in both commands (CredentialId small, e.g. 2KB; payloads 64KB).
- Source: general, security
- Disposition notes:

### M5 — Severity: suggestion — Status: open
- File: source/libraries/timewarp-identity/ceremonies/webauthn/internal/cose-key.cs:142-149
- Description: RSA COSE keys accepted with no minimum modulus size — a 512-bit RSA key registers successfully (self-inflicted weak credential; defense-in-depth).
- Suggestion: Reject RSA moduli < 2048 bits at registration parse (UnsupportedAlgorithm/MalformedData reason); add a vector test.
- Source: security
- Disposition notes:

### M6 — Severity: suggestion — Status: open
- File: tests/container-apps/web/web-server-integration-tests/Features/Identity/Passkey_Registration_Tests.cs:13-17
- Description: Test file's "fresh instance per test method" Design claim is contradicted by Fixie per-class fixture sharing (the singleton stores + the CredentialId-collision fix this very commit documents); practical consequence: HttpClient cookie-container leakage across test methods could mask session assertions.
- Suggestion: Fix the Design comment; isolate cookies per test (fresh HttpClient or explicit cookie clearing) where session state is asserted.
- Source: general
- Disposition notes:

### M7 — Severity: nit — Status: open
- File: web-contracts/features/identity/queries/get-current-session.cs:29-37
- Description: Response documents IsAuthenticated⇔PrincipalId-pairing invariant its ctor doesn't enforce.
- Suggestion: Enforce in ctor (throw on mismatch) or soften the doc.
- Source: general
- Disposition notes:

### M8 — Severity: nit — Status: open
- File: tests/.../Passkey_Registration_Tests.cs:132
- Description: Leftover "fixed CredentialId" comment from before the per-instance-random fix.
- Suggestion: Delete/update the comment.
- Source: general
- Disposition notes:

## Duplicates / conflicts

- general#2 + security#3 → M3 (kept suggestion severity; strongest shared framing: 500 + orphan).
- general#4 + security#2 → M4.
- No severity conflicts.

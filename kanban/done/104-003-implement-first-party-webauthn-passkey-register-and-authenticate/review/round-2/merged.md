# Round 2 — merged findings
**Date:** 2026-07-19
**Sources:** security (re-verify of round-1 M1–M8 fixes + defect scan of the fix delta)

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 2 | 0 |
| suggestion | 0 | 5 | 0 |
| nit | 0 | 2 | 0 |

Round-1's M1–M8 (1 bug, 5 suggestions, 2 nits) are carried forward here as **fixed/confirmed**: security.md
re-verified all eight against the post-fix code and found no regressions (M1 binding test genuinely
asserts a non-default value; M2's quarantine check now runs strictly after `Verify`, and no other
pre-verify branch leaks account state; M3's catch uses the correct exception type at the correct
scope; M4's caps are present on every field on both commands; M6's Design regions and cookie
isolation hold; M7's ctor guard is in place with no new untrusted-input exception surface; M8's
comment is reworded). The orchestrator (this pass) independently re-built and re-ran the full suite
against the current tree to confirm M6–M8 hold at the test-execution level, not just by inspection.
Full round-1 descriptions/dispositions: `../round-1/merged.md` (general.md/security.md untouched, as
instructed). Full round-2 description: `security.md`.

Round-2's own scan surfaced one new bug, introduced by the M5 fix: **M9**, fixed in this pass, plus
one sibling gap (empty RSA exponent) found while auditing the neighborhood M9 flagged — both closed
by the same guard.

## Issues

### M1 — Severity: bug — Status: fixed (carried from round 1)
- Disposition: Confirmed fixed by round-2 security re-verify. No change this round.

### M2 — Severity: suggestion — Status: fixed (carried from round 1)
- Disposition: Confirmed fixed by round-2 security re-verify (quarantine check strictly post-Verify;
  the pre-Verify revoked-credential early-out returns the same generic 400 as unknown/bad-signature,
  so it discloses no account state — behavior-equivalent, not a new oracle). No change this round.

### M3 — Severity: suggestion — Status: fixed (carried from round 1)
- Disposition: Confirmed fixed by round-2 security re-verify (correct exception type, tight catch
  scope — the subsequent `IssueAsync` call is outside the try, so unrelated `InvalidOperationException`s
  are not swallowed). No change this round.

### M4 — Severity: suggestion — Status: fixed (carried from round 1)
- Disposition: Confirmed fixed by round-2 security re-verify (every base64url field capped on both
  commands; `MaximumLength` on the optional `UserHandle` correctly tolerates null). No change this
  round.

### M5 — Severity: suggestion — Status: fixed (carried from round 1)
- Disposition: Confirmed fixed for the stated concern by round-2 security re-verify — the 2048-bit
  floor is correct and inclusive (a proper 2048-bit modulus passes, 2047 is rejected), leading
  zero-byte padding is handled, and the weak-key vector test is honest (fails if the check is
  removed). The fix introduced a new defect handled as M9 below.

### M6 — Severity: suggestion — Status: fixed (carried from round 1)
- Disposition: Confirmed by round-2 security re-verify (general-scope) and by the orchestrator
  re-running `web-server-integration-tests` this round (34 passed, 1 skipped, 0 failed, including the
  isolated-cookie session assertions). No change this round.

### M7 — Severity: nit — Status: fixed (carried from round 1)
- Disposition: Confirmed fixed by round-2 security re-verify — ctor throws on a disagreeing pair; no
  new untrusted-input exception surface since `GetCurrentSession.Response` is server-produced only
  (never deserialized from attacker input server-side). No change this round.

### M8 — Severity: nit — Status: fixed (carried from round 1)
- Disposition: Confirmed reworded by round-2 security re-verify. No change this round.

### M9 — Severity: bug — Status: fixed (new; introduced by the M5 fix)
- File: source/libraries/timewarp-identity/ceremonies/webauthn/internal/cose-key.cs (`GetModulusBitLength`
  ~:181/188 pre-fix, call site in `TryCreateVerifier` ~:154, catch ~:165)
- Description: `GetModulusBitLength` dereferenced `modulus[index]` with no guard for a zero-length
  array. `CoseKey.TryParse`'s RSA branch only null-checks the modulus it reads (a zero-length CBOR
  byte string decodes to a non-null, empty `byte[]`, which passes), so an attacker-supplied RS256
  COSE key with an empty `n` reached `GetModulusBitLength` and threw `IndexOutOfRangeException` —
  not caught by `TryCreateVerifier`'s `catch (CryptographicException)`, so it propagated out of
  `WebAuthnRegistration.Verify` into the ASP.NET pipeline as an unhandled 500. Breaks the module's
  documented "verification stays exception-free on the adversarial path" invariant.
- Neighborhood audit (per dispatch instruction): independently reproduced on this platform via a
  throwaway script exercising `RSA.ImportParameters`/`ECDsa.Create` directly with empty inputs.
  Findings:
  - **Empty RSA Exponent (same family, independently reachable) — genuine sibling gap, now fixed
    by the same guard.** With a real, sufficiently large Modulus but an empty Exponent,
    `RSA.ImportParameters(...)` throws `System.IndexOutOfRangeException` on this platform's
    OpenSSL-backed RSA — **not** `CryptographicException` — so it was equally uncaught by
    `TryCreateVerifier`'s catch clause. This is a second, independent crash in the same call, not
    covered by the modulus-only guard the dispatch's literal suggested fix (`if (modulus.Length == 0)
    return false;`) would have addressed alone.
  - **Empty EC2 X/Y — verified safe, no change needed.** Both an empty-X/empty-Y `ECPoint` and an
    empty-X-only `ECPoint` throw `CryptographicException` from `ECDsa.Create` on this platform
    (`Interop+Crypto+OpenSslCryptographicException`, which is a `CryptographicException` subtype, for
    the both-empty case; a plain `CryptographicException` — "Q.X, Q.Y must be the same length" — for
    the X-only case). Both are already caught by the existing `catch (CryptographicException)`.
- Fix: Added `if (Modulus.Length == 0 || Exponent.Length == 0) return false;` in `TryCreateVerifier`'s
  RS256 branch, before `GetModulusBitLength` is called and before `RSA.Create()`/`ImportParameters`
  are reached — covers both the reported modulus case and the newly-found exponent case in one
  guard. Also added a defense-in-depth `if (modulus.Length == 0) return 0;` at the top of
  `GetModulusBitLength` itself (per the dispatch's literal suggested shape), so the helper is safe
  to call standalone even though the caller-side guard already excludes empty input today. Design
  region rewritten to document both crash vectors, the empirical platform verification, and why the
  EC2 path needed no change.
- Tests: Added `SoftwareAuthenticator.BuildEmptyModulusRsaCoseKey()` (empty `n`, reuses the existing
  weak-key exponent) and `BuildEmptyExponentRsaCoseKey()` (real 2048-bit modulus, empty `e`) fixture
  builders, plus `Empty_rsa_modulus_fails_without_throwing` and
  `Empty_rsa_exponent_fails_without_throwing` vector tests in webauthn-registration-tests.cs — both
  assert `IsValid == false` / `FailureReason == UnsupportedAlgorithm` (would have thrown before the
  fix, which Shouldly/Fixie would report as a test error, not a clean assertion failure — the
  distinction that pins "does not throw" specifically).
- Status: fixed

## Fix-delta scan — other files

No further new defects found. The M9 fix touches only cose-key.cs (guard + defensive helper guard +
Design region) and its test fixture/vector siblings; no other round-1 fix file was touched this
round.

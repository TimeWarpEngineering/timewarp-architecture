# Disposition — task 104-003

**Date:** 2026-07-19
**Outcome:** clean
**Rounds:** 2
**Final open count:** 0

## Summary

Round 1 ran at effort 2 (general + security — the security specialist was warranted by the
hand-rolled WebAuthn verifier). 8 merged findings against implementation commit 56882153: 1 bug
(WebAuthnOptions config section never bound — silent no-op masked by matching defaults), 5
suggestions (pre-verify quarantine oracle, duplicate-registration race → 500 + orphan, missing
size caps, weak RSA moduli accepted, test-fixture Design honesty), 2 nits. Two findings were
independently reported by both reviewers. All 8 fixed in commit d2c16a74. The security round-1
pass verified the crypto core sound: correct §7.1/§7.2 verification steps, algorithm bound to the
stored key (no confusion path), server-sourced one-time challenges consumed before verification.

Round 2 (security re-verify of the fix delta): M1–M8 confirmed, none reopened; the M3
orphaned-Principal documented-acceptance was explicitly judged SOUND (orphan is Provisional,
credential-less, unreachable, unauthenticable, non-escalatable). One NEW bug (M9): the M5 fix
introduced an uncaught IndexOutOfRangeException on an empty RSA modulus. Fixing it, the
implementer's neighborhood audit found and closed a second independent crash vector (empty
exponent throws IndexOutOfRangeException from RSA.ImportParameters itself) and empirically
verified the EC path safe. Fixed in commit d75af08b with two adversarial vectors.

Final verification: dev build 0/0; timewarp-identity-tests 127/127, web-contracts-tests 14/14,
web-server-integration-tests 34 passed / 1 skipped. Docker-dependent suites not runnable
(pre-existing environment issue, unrelated).

## Exception log

None. (The M3 orphan acceptance is recorded inside M3's fixed disposition — the actionable
defect (500) was fixed; the residual orphan possibility was security-reviewed and accepted with
rationale in the handler Design region, pending 104-005 store-lifecycle work.)

## Escalations

None.

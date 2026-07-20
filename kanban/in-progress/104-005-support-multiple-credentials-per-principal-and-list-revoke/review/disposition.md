# Disposition — task 104-005

**Date:** 2026-07-20
**Outcome:** clean
**Rounds:** 1 (+ orchestrator verification of the fix delta)
**Final open count:** 0

## Summary

Round 1 ran at effort 2 (general + security) — self-service credential management is the most
security-sensitive surface in the 104 program. **Zero bugs from either lens**, and the security
reviewer explicitly certified the IDOR/ownership model SOUND: verified by counterfactual (the
foreign-credential test flips 404→409 if the ownership check is removed), key material structurally
omitted, identity:read→403 least-privilege proven live, the revoke retry loop never leaks
ConcurrencyConflictException as 500, and cross-principal handle collision yields an identical 409
with no attach and no oracle. 5 findings total (2 suggestions, 3 nits), all documentation /
test-hardening / precision — no code-behavior change was needed to a model already correct. All 5
fixed in commit 60a6a84e:
- M1: accurate merged-identity claim-resolution wording (doc-only; fails safe in the dual-credential case).
- M2: fixed-by-tracking — the third instance of the contract-vs-server policy-name coupling, recorded on task 111 (the policy-name-agreement analyzer follow-up).
- M3: cross-principal duplicate-handle regression tests.
- M4: structural (reflection) key-material-omission assertions + wire-level belt-and-suspenders.
- M5: confused-deputy-safety Design note on the shared Registration ceremony reuse.

Final verification: dev build 0/0; timewarp-identity-tests 169, web-contracts-tests 38,
web-server-integration-tests 80 passed / 1 skipped (up from 78 — the two new cross-principal tests
confirmed running and green). Docker-dependent suites not runnable (pre-existing environment issue).

## Exception log

None. (M2 resolved by tracking task 111, not deferral.)

## Escalations

None.

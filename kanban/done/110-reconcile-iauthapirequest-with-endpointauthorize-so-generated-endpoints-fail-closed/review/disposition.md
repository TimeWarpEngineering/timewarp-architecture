# Disposition — task 110

**Date:** 2026-07-20
**Outcome:** clean
**Rounds:** 1 (+ orchestrator verification of the fix delta, incl. direct check of the security-critical M1)
**Final open count:** 0

## Summary

Round 1 ran at effort 2 (general + security). 5 findings against implementation commit 44fd802f:
1 bug (M1), 2 suggestions (M2, M3), 2 nits (M4, M5). **Both reviewers independently confirmed the
original task-109 finding — admin roles CRUD generated as public `AllowAnonymous()` — is now
GENUINELY CLOSED**: all 20 `[ApiEndpoint]` contracts carry exactly one posture marker, roles CRUD
emit `Policies("identity-session-authenticated")`, fail-closed generator default + both-markers
precedence + cross-scheme isolation + no analyzer-evasion path all verified, and the 401/200 roles
tests are non-vacuous.

The one bug (M1) was a **pre-existing account-takeover primitive** the security reviewer caught
being blessed-as-public by 110's first-pass anonymous marker: `get-sign-in-token` mints a sign-in
token for an arbitrary caller-supplied `UserId` with no proof of identity, and is live in any
configured instance. The orchestrator verified it has **zero live consumers** (the SPA's legacy
passwordless flow calls the Passwordless.dev SaaS, not this route), so the fix removed it from the
server surface entirely — `[ClientOnlyContract]` instead of a generated endpoint, dead YARP route
deleted — rather than deferring it with an anonymous marker. All 5 fixed in commit 109af985 (M2 as
fixed-by-tracking → task 111, a policy-name-agreement analyzer). The security-critical M1 removal
was verified directly by the orchestrator (no generated endpoint remains, TWA0006 satisfied,
route gone).

Final verification: dev build 0/0; analyzers-tests 82, sourcegen-tests 40, web-contracts-tests 26,
web-server-integration-tests 57 passed / 1 skipped. Docker-dependent suites not runnable
(pre-existing environment issue).

## Exception log

None. (M2 resolved by tracking, not deferral — task 111 filed; M1 was pre-existing and is now
fixed, not accepted.)

## Escalations

None. The M1 disposition (remove from server surface rather than defer to 104-016/021 legacy
retirement) was an evidence-backed orchestrator call — no live consumer, security-in-scope for a
task about generated-endpoint auth posture.

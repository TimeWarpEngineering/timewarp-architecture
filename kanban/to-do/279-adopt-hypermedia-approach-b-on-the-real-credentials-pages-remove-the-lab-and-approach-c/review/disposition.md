# Disposition — task 279

**Date:** 2026-10-05
**Outcome:** accepted-exceptions
**Rounds:** 2
**Final open count:** 0

## Summary

Round 1 used three parallel reviewers (general, tests and security, at effort 3). They raised 0 bugs, 5 suggestions and 6 nits. Nine findings were fixed on this task:
- Design-region accuracy (M1, M2, M4);
- the SPA scripted BFF now uses the real server offer rule (M5);
- new and stronger tests: API-level Link Microsoft 365 offered and linked (M6), server offers contradicting the count (M7), the unauthenticated M4 refusal (M8), the cross-page refusal assertions (M9), and the mock offers round-trip (M10).

Round 2 re-verified the fixes against the build and the affected suites and found nothing new. Two nits are accepted as wontfix.

## Exception log

| ID | Severity | Rationale | Decided by |
|----|----------|-----------|------------|
| M3 | nit | The `FetchCredentials` follow-up after Link Microsoft 365's forceLoad navigation is wasted but harmless, since a cancellation is never painted. A per-offer follow-up would complicate the runner's row-equality check for no user-visible gain. | orchestrator |
| M11 | nit | The SPA accepts any permitted Human/Both catalog action a server offer names. This is deliberate under task 279's M4 decision (catalog metadata, not a second offerable-names list). The server is the trust boundary, and each action keeps its own server-side authorization. | orchestrator |

## Escalations

- None.

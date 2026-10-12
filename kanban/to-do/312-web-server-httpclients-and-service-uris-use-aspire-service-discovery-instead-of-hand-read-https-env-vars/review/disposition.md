# Disposition — task 312

**Date:** 2026-10-12
**Outcome:** accepted-exceptions
**Rounds:** 2
**Final open count:** 0

## Summary

Round 1 (general + security) raised 7 merged findings. One was a bug: the self-call's port-stripped `X-Forwarded-Host` broke mail permalinks. There were 2 suggestions (gRPC channel tests, an ingress `/grpc` drift check) and 4 nits. Security found no widening of RP-ID or cookie exposure. Removing `X-TimeWarp-Circuit-Host` actually shrinks the attack surface, and the RP ID stays allowlist-bound. Fixes landed in 140ab824f. The round-2 re-review confirmed M1–M3 and M5–M7 fixed, with no new defects. M4 is wontfix.

## Exception log

| ID | Severity | Rationale | Decided by |
|----|----------|-----------|------------|
| M4 | nit | The test host mirrors the `AddServiceDefaults` discovery calls because `WebApplicationHost` does not run `Main`. Production wiring is proven by the publish-output shape and the Aspire suites. | review orchestrator |

## Escalations

- None.

# Disposition — task 442

**Date:** 2026-10-29
**Outcome:** accepted-exceptions
**Rounds:** 2
**Final open count:** 0

## Summary

Round 1 (general reviewer plus orchestrator document dump) raised five findings: no bug, M1 refuted
against the real document and closed wontfix, M2 and M3 (suggestion) and M5 (nit) sent to a fix pass,
M4 (nit) wontfix. The fix pass (`0b63e2c89`) made both transformer guards throw with the type or
endpoint named, added an endpoint-driven test that every problem-arm operation publishes
`default → SharedProblemDetails` plus probes for the explicit 4xx rewrite and the no-problem-arm union,
and re-wrapped the Design comments. Round 2 verified all three as fixed on the delta and raised one nit
(N1, a sanity bound in the new test), closed wontfix because a sibling test on the same host already
fails in the case it names. Gates re-run by the review oracle on the final commit: build 0 warnings
and 0 errors, OpenApiOneOf_ 9 passed, full web-server integration suite 418 passed, 1 skipped, 0 failed.

## Exception log

| ID | Severity | Rationale | Decided by |
|----|----------|-----------|------------|
| M1 | suggestion | Predicted early-return shape (explicit `Produces(200)` from a request-class `<summary>`) does not occur for any operation in the generated document; the loud-guard concern was carried by M3 and is now fixed. | review oracle (round 1) |
| M4 | nit | Nullable OneOf property cannot be on the wire: no OneOf converter in the contract JSON options; Design region records the property rule as wrapper hygiene only. Revisit if a converter is added. | review oracle (round 1) |
| N1 | nit | The `> 1` bound on scanned operations is met by the probes alone, but contracts and probes share one discovery mechanism, and the TrackEvent contract test on the same host fails if a contract drops out of the document. | review oracle (round 2) |

## Escalations

- None. No human decision was required.

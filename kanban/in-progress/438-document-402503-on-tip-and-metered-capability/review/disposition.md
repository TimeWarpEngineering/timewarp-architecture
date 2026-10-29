# Disposition — task 438

**Date:** 2026-10-29
**Outcome:** reopened (fix pass requested; final disposition follows round 4)
**Rounds:** 3
**Final open count:** 2 (M4 bug, M5 nit)

## Summary

Round 1 (general, effort 2) raised 2 suggestions and 1 nit: M1 (hard-coded challenge
`resource.url`), M2 (the test hard-coded routes), and M3 (a dead fallback). The fix commit
`0264769f3` resolved all three and round 2 found nothing new, so this file read `clean` on
2026-10-29 08:51. PR #600 then failed `ci (integration)` twice with 199 failures: the payment
test's public stub endpoints are auto-discovered by every in-process api-server host and register
duplicate routes. Round 3 (general, effort 3) confirmed it locally and opened M4 (bug) for it, plus
M5 (nit, route literals in the info description). The `clean` outcome is withdrawn until M4 is fixed
and re-reviewed. Live ledger: `review/round-3/merged.md`.

## Exception log (if accepted-exceptions)

None yet.

## Escalations

None.

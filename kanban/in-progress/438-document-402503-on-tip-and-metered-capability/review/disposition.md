# Disposition — task 438

**Date:** 2026-10-29
**Outcome:** reopened (fix pass requested; final disposition follows round 5)
**Rounds:** 4
**Final open count:** 2 (M4 bug, M5 nit)

## Summary

Round 1 (general, effort 2) raised 2 suggestions and 1 nit: M1 (hard-coded challenge
`resource.url`), M2 (the test hard-coded routes), and M3 (a dead fallback). The fix commit
`0264769f3` resolved all three and round 2 found nothing new, so this file read `clean` on
2026-10-29 08:51. PR #600 then failed `ci (integration)` twice with 199 failures: the payment
test's public stub endpoints are auto-discovered by every in-process api-server host and register
duplicate routes. Round 3 (general, effort 3) confirmed it locally and opened M4 (bug) for it, plus
M5 (nit, route literals in the info description). Round 4 (general, effort 1) found the product tree
unchanged since round 3: the implement-fix-2 oracle returned Done in 26 ms without a fix, so M4 and
M5 are carried open. The `clean` outcome stays withdrawn until M4 is fixed and re-reviewed. Live
ledger: `review/round-4/merged.md`.

## Exception log (if accepted-exceptions)

None yet.

## Escalations

None. If the next implement-fix oracle again returns Done without a product change, an operator should
look at the host journal replay (same implementer session id `01a18528-…` and 26 ms on both passes)
rather than requesting a further fix pass.

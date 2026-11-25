# Disposition — task 521

**Date:** 2026-11-25
**Outcome:** clean
**Rounds:** 3
**Final open count:** 0

## Summary

Three rounds, general reviewer only. Round 1 raised nothing. Round 2 raised M1 (bug): a second open of `/Feedback` made `RouteState` drop the page just left, so a later filing stored an older page. Fix `0beb777fe` records visits on a scoped `NavigationHistory` and the submit reads that list. Round 3 re-checked that delta and found no new issues. M1 is fixed. Nothing is open and nothing was wontfix.

## Exception log (if accepted-exceptions)

None.

## Escalations

None.

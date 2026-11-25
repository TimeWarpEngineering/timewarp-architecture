# Disposition — task 523

**Date:** 2026-11-26
**Outcome:** clean
**Rounds:** 2
**Final open count:** 0

## Summary

Round 1 found one bug: `dev test <file>` could not isolate a class whose `--filter-class` value is a substring of another class FullName in the same project. The fix pass refuses that file before any run and pins `Equality` / `Equality_Operators`. Round 2 re-reviewed that delta and found no new issues. M1 is fixed.

## Exception log (if accepted-exceptions)

None.

## Escalations

None.

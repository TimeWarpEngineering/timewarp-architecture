# Disposition — task 503

**Date:** 2026-11-24
**Outcome:** clean
**Rounds:** 2
**Final open count:** 0

## Summary

Round 1 found one bug: a push took `ci` from the event-level build flag, so a skills-only or evals-only push ran ci-group. The fix pass sets `ci` from the push diff with the pull request build rule. Round 2 reviewed that delta, confirmed M1 is still fixed, and raised nothing new.

## Exception log (if accepted-exceptions)

| ID | Severity | Rationale | Decided by |
|----|----------|-----------|------------|

## Escalations

- None.

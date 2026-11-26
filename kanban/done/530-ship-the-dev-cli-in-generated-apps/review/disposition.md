# Disposition — task 530

**Date:** 2026-11-26
**Outcome:** clean
**Rounds:** 5
**Final open count:** 0

## Summary

Round 1 found one bug (M1: `dev deploy migrate` named postgres when web was off) and one suggestion (M2: `dev deploy` still ran without web). The fix pass gives postgres a required web feature, checks that feature first, refuses `dev deploy` without web before preflight, and pins both with tests and the readme. Rounds 2 and 3 re-reviewed and found nothing new. Round 4 re-reviewed the origin/master merge: yarp is refused only on compose and kubernetes, the feedback OpenAPI test is excluded with the other postgres-off feedback files, and the smoke runner keeps both the variant selector and the generated CLI checks. Round 5 re-reviewed the CI prose fix in `workflow-command.cs`: the release-mode refusal no longer contains the literal `dev deploy`, which the NeverAutomated guard reads as an invocation. The path still exits 1 and does not deploy. M1 and M2 stay fixed. Every raised finding is fixed.

## Exception log (if accepted-exceptions)

None.

## Escalations

None.

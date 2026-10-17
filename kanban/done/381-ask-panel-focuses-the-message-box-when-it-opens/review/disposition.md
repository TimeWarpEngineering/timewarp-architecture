# Disposition — task 381

**Date:** 2026-10-17
**Outcome:** clean
**Rounds:** 2
**Final open count:** 0

## Summary

Round 1 (general) raised one bug: closing Ask reopened the command palette when Ask came from the
palette opened via the appbar search field, because Ask restored focus to the palette trigger
outside the palette's guard. Fixed on this task with a palette-owned guarded restore carried on the
focus-return claim, plus a Playwright test that failed before the fix. Round 2 re-verified it.
The implementer's goal restatement matches the task; no misread.

## Escalations

- None.

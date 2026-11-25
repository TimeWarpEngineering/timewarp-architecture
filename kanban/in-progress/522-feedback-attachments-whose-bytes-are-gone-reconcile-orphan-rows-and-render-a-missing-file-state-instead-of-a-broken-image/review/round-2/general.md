# Round 2 — general
**Date:** 2026-11-25
**Scope reviewed:** product delta since round 1, commit `5bb8d4b3f` (`documentation/developer/guides/feedback-attachments.md`). Re-checked M1 against `FeedbackModule`, `PostgresDbModule`, and `UnavailableFeedbackStorage`. Gates were not re-run. The fix-pass record under `## Results` (`./bin/dev build`, `ganda repo audit`, no test files because the diff is the guide only) covers this change.

## Summary

The guide's store table and the paragraph under it now match the code. `FeedbackModule` registers `UnavailableFeedbackStorage` for the row ports and, unless Azure is configured, for the byte port. `PostgresDbModule` replaces those ports only when a Postgres connection is set, and it leaves the byte port alone when Azure already took it. Reads on the stand-in are empty, removes complete without writing, and filing, updating, and uploading throw the message the guide quotes. M1 is fixed. The delta adds no new defect. The goal-restatement mismatch on the reconcile pass is already under `## Learnings` and is unchanged by this doc fix.

## Issues

<!-- None. M1 is carried on merged.md as fixed. -->

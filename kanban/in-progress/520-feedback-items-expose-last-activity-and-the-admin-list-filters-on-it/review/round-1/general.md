# Round 1 — general
**Date:** 2026-11-25
**Scope reviewed:** branch `task/520-feedback-items-expose-last-activity-and-the-admin` vs `master`, checked against the original task and `## Goal (implementer restatement)`, plus the recorded `## Results` and `### How to validate`.

## Summary

`FeedbackItem.LastActivityAt` is `FiledAt` at filing and the `At` of each appended thread entry. One private `Append` is the only writer, and the save-time invariant ties the field to the last entry by sequence. The migration adds the column and index, then backfills from the highest-sequence activity or `FiledAt`. The admin list item and admin detail return the field. `ActivityFrom` is an inclusive lower bound ANDed with the existing filters, and `SortBy` accepts `LastActivityAt`, still on `feedback:read`. Filer contracts are untouched. The restatement matches the task. The recorded build, targeted tests, and post-723 audit cover this diff. No issues.

## Issues

None.

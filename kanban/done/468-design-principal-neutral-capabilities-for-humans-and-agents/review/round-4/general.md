# Round 4 — general
**Date:** 2026-11-02
**Scope reviewed:** Delta since round-3 `base.sha` `00c2d430bd67b3da64e354f9c9c66b2d9fdafb59` (fix commit `65450aa43`). Task 4 in `design.md` and the task Results sentences. Prior M1–M9 re-checked against that delta. No product code in the diff.

## Summary

Task 4 now keeps the section 6.1 order. An unlinked draft stays owner-only before any permission is consulted, and only the linked cross-owner check becomes one disjunction of the credential (`feedback.read` or `admin.feedback.read`, no scheme branch). The proof gate expects 200 on another owner's linked attachment, 404 on another owner's unlinked draft, and 200 on the bearer's own unlinked draft. That matches the handler at `download-feedback-attachment-handler-application.cs` lines 69–88 and `FeedbackProblems` (401 unauthenticated, 404 not found, 404 attachment gone). Sections 2.4, 5, and 6.1 are unchanged in this delta. The implementer restatement matches the task. No misread.

## Issues

No new issues.

# Round 4 — merged findings
**Date:** 2026-11-02
**Sources:** general

Re-review of the delta since round-3 `base.sha` `00c2d430bd67b3da64e354f9c9c66b2d9fdafb59`. Prior IDs carried forward. Round 1, round 2, and round 3 files were not edited.

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 7 | 0 |
| suggestion | 0 | 1 | 0 |
| nit | 0 | 1 | 0 |

## Resolved prior

- M1 — bug — fixed. Per-id `TokenPermission` is unchanged in this delta. Round 3 re-verification stands.
- M2 — bug — fixed. Section 7.2 is unchanged in this delta. Round 3 re-verification stands.
- M3 — bug — fixed. The separate download row and the `admin.profiles.read` gate are unchanged in this delta. The download rule itself is M8.
- M4 — bug — fixed. Task 5 and task 7 dependencies are unchanged in this delta.
- M5 — bug — fixed. Q7 and the agent-link kind checks are unchanged in this delta.
- M6 — nit — fixed. "Both: 14" and the Results counts are unchanged in this delta.
- M7 — suggestion — fixed. Q3, Q6, and Q18 pointers are unchanged in this delta.
- M8 — bug — fixed. Sections 2.4, 5, and 6.1 are unchanged in this delta. Round 3 re-verification stands: no attribute policy, unlinked draft owner-only before any permission check, linked cross-owner check is `feedback.read` or `admin.feedback.read` on the credential, bytes-gone 404 titled "Attachment no longer available".
- M9 — bug — fixed. Re-verified round 4 against `design.md` line 491, section 6.1 lines 380–395, and `download-feedback-attachment-handler-application.cs` lines 69–88. Task 4 says the handler keeps the section 6.1 order: an unlinked draft stays owner-only before any permission is consulted, and only the linked cross-owner check in step 3 becomes one disjunction of the credential (`feedback.read` or `admin.feedback.read`, no scheme branch). The proof gate expects 200 on another owner's linked attachment, 404 on another owner's unlinked draft, and 200 on the bearer's own unlinked draft. The handler returns `FeedbackProblems.NotFound` (404) for another owner's unlinked draft before `HasCrossOwnerReadAsync`, and the owner of an unlinked draft falls through to the blob read, which is 200 when the bytes exist and `AttachmentGone` (404, title "Attachment no longer available") when they do not. `grep` of `design.md` finds no "collapses to the one disjunction".

## Issues

No new issues.

## Duplicates / conflicts

- One general reviewer. No new findings to merge. M9 stays `fixed`.

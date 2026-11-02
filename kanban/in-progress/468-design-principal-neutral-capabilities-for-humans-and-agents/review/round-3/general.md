# Round 3 — general
**Date:** 2026-11-02
**Scope reviewed:** Delta since round-2 `base.sha` `074ea5a9cffc851874b3e1c4b6176f26600b50fa` (commit `00c2d430b`). `design.md` download rule and task 4. Prior M1–M8 re-checked against that delta. No product code in the diff.

## Summary

Sections 2.4, 5, and 6.1 now match `DownloadFeedbackAttachment`'s handler: no attribute policy, unlinked drafts stay owner-only before any permission check, a linked attachment opens for the owner or for `feedback.read` or `admin.feedback.read` on the credential, and a missing blob is 404. `feedback:read` expands to `feedback.read` alone, and `feedback.file.self` is not an agent scope. Download is the only hosted `[EndpointAuthorize]` with no `Policy`. The task 4 cell undoes that rule: it tells the implementer to collapse the handler to one disjunction, and its proof gate expects 404 for the bearer's own unlinked draft, which the owner passes. The implementer restatement matches the task. No misread.

## Issues

### Issue 1 — Severity: bug
- File: kanban/in-progress/468-design-principal-neutral-capabilities-for-humans-and-agents/design.md:491
- Description: Task 4's scope says `DownloadFeedbackAttachment`'s handler "collapses to the one disjunction in section 6.1". Section 6.1 (lines 380–386) is four steps. Only step 3 is a disjunction, and only for a linked attachment. Step 2 is earlier and is not a permission check: when `FeedbackItemId` is null, the owner passes and every other caller is 404. The handler does that at `download-feedback-attachment-handler-application.cs` lines 69–76, before `HasCrossOwnerReadAsync`. Collapsing the handler to one disjunction (owner, or `feedback.read`, or `admin.feedback.read`) lets a `feedback.read` bearer open another owner's unlinked draft, which step 2 forbids. The same cell's proof gate says a bearer holding only `feedback.read` "gets 200 on another owner's linked attachment and 404 on their unlinked draft". "Their" is the bearer. The bearer is the owner of that draft, so step 2 lets them through and the response is 200 when the bytes exist (`FeedbackProblems.Unauthenticated` is 401, `NotFound` and `AttachmentGone` are 404, and `AttachmentGone`'s title is "Attachment no longer available"). A proof gate that expects 404 there fails against the handler section 6.1 says to keep. Sections 2.4, 5, and the 6.1 list already state the handler order and stay as they are.
- Suggestion: In the task 4 scope, say the handler keeps the section 6.1 order: an unlinked draft stays owner-only, and only the linked cross-owner check becomes one disjunction of the credential. In the proof gate, expect 200 on another owner's linked attachment, 404 on another owner's unlinked draft, and 200 on the bearer's own unlinked draft.
- Status: open

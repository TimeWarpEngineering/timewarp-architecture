# Round 3 — merged findings
**Date:** 2026-11-02
**Sources:** general

Re-review of the delta since round-2 `base.sha` `074ea5a9cffc851874b3e1c4b6176f26600b50fa`. Prior IDs carried forward. Round 1 and round 2 files were not edited.

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 7 | 0 |
| suggestion | 0 | 1 | 0 |
| nit | 0 | 1 | 0 |

## Resolved prior

- M1 — bug — fixed. Per-id `TokenPermission` is unchanged in this delta. Round 2 re-verification stands.
- M2 — bug — fixed. Section 7.2 is unchanged in this delta. Round 2 re-verification stands.
- M3 — bug — fixed. The separate download row and the `admin.profiles.read` gate remain. The download rule itself is M8.
- M4 — bug — fixed. Task 5 and task 7 dependencies are unchanged in this delta.
- M5 — bug — fixed. Q7 and the agent-link kind checks are unchanged in this delta.
- M6 — nit — fixed. "Both: 14" and the Results counts are unchanged in this delta.
- M7 — suggestion — fixed. Q3, Q6, and Q18 pointers are unchanged in this delta.
- M8 — bug — fixed. Re-verified round 3 against the handler, `FeedbackProblems`, `AgentScopePermissionSeed`, `AgentScopes`, `PermissionIds`, and every hosted `[EndpointAuthorize]`. Sections 2.4, 5, and 6.1 match: no attribute policy, unlinked draft owner-only before any permission check, linked cross-owner check is `feedback.read` or `admin.feedback.read` on the credential, bytes-gone 404 titled "Attachment no longer available". `feedback:read` expands to `feedback.read` alone. `feedback.file.self` is documented as not an agent-token scope and is absent from `AgentScopes.All`. Download is the only hosted contract whose `[EndpointAuthorize]` has no `Policy`. The task 4 wording that still says "one disjunction" is M9.

## Issues

### M9 — Severity: bug — Status: fixed
- File: kanban/in-progress/468-design-principal-neutral-capabilities-for-humans-and-agents/design.md:491
- Description: Task 4's scope says `DownloadFeedbackAttachment`'s handler "collapses to the one disjunction in section 6.1". Section 6.1 (lines 380–386) is four steps. Only step 3 is a disjunction, and only for a linked attachment. Step 2 is earlier and is not a permission check: when `FeedbackItemId` is null, the owner passes and every other caller is 404. The handler does that at `download-feedback-attachment-handler-application.cs` lines 69–76, before `HasCrossOwnerReadAsync`. Collapsing the handler to one disjunction (owner, or `feedback.read`, or `admin.feedback.read`) lets a `feedback.read` bearer open another owner's unlinked draft, which step 2 forbids. The same cell's proof gate says a bearer holding only `feedback.read` "gets 200 on another owner's linked attachment and 404 on their unlinked draft". "Their" is the bearer. The bearer is the owner of that draft, so step 2 lets them through and the response is 200 when the bytes exist. A proof gate that expects 404 there fails against the handler section 6.1 says to keep. Sections 2.4, 5, and the 6.1 list already state the handler order.
- Suggestion: In the task 4 scope, say the handler keeps the section 6.1 order: an unlinked draft stays owner-only, and only the linked cross-owner check becomes one disjunction of the credential. In the proof gate, expect 200 on another owner's linked attachment, 404 on another owner's unlinked draft, and 200 on the bearer's own unlinked draft.
- Source: general
- Disposition notes: Fixed 2026-11-02 (implement-fix-4). Task 4 scope now says the handler keeps the section 6.1 order: an unlinked draft stays owner-only before any permission is consulted, and only the linked cross-owner check in step 3 becomes one disjunction of the credential. The proof gate expects 200 on another owner's linked attachment, 404 on another owner's unlinked draft, and 200 on the bearer's own unlinked draft.

## Duplicates / conflicts

- One general reviewer. M9 is the task 4 remainder of M8. M8 stays `fixed` because sections 2.4, 5, and 6.1 match the handler. No other overlaps.

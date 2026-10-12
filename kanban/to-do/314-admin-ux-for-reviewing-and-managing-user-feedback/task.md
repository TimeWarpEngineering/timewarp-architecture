# Admin UX for reviewing and managing user feedback

## Description

Users can file feedback (task 273, plus attachments 295 / 303-001), but nobody can *see* it. There
is no admin view, no triage state, and no way to answer the filer. Add an admin-side experience
for reviewing feedback, and decide how feedback is managed over its life.

Reference conversation (Steve with Grok, 2026-10-12):
https://grok.com/share/bGVnYWN5LWNvcHk_d4f68045-abfd-476b-8fab-af33084d818e. It is captured
verbatim in [references/grok-feedback-admin-2026-10-12.md](references/grok-feedback-admin-2026-10-12.md).

## Current state (verified against master, 2026-10-12)

- `FeedbackItem` (`features/feedback/feedback-item-domain.cs`) is **immutable after `File`**. Its
  Design region says "no later mutations (no status, assignment, or edit)". Its fields are
  `OwnerPrincipalId`, `Kind`, `Title`, `Body` and `FiledAt`.
- `IFeedbackStore` only has `AddAsync`, `FindAsync`, `ListByOwnerAsync` and `RemoveAsync`. The EF
  config says "There is no cross-owner query". An admin cannot list everything today.
- Permissions: only `feedback.file.self` exists. Admin access patterns exist (`admin.access`,
  `admin.roles.*`, `admin.principals.*`; the Administrator role), and so do the admin slices under
  `features/admin/` (principals, roles) to mirror.
- Mail: `IProfileEmailLookup` and the filing receipt email (273) exist, so a reply to the filer can
  reuse that path.

## What comparable products do (from the reference)

Feedback boards (Canny, UserVoice, Productboard, Featurebase, Sleekplan) share one admin shape:
- a status per item (Open, Under Review, Planned, In Progress, Completed, Declined, plus custom
  ones);
- merge duplicates, tags or categories, and an assigned owner;
- replies, either **public** comments or **private** replies to the submitter (usually email);
- an automatic notification when the status changes, which closes the loop.

## Decision log (2026-10-12)

Steve delegated these six choices to the cockpit ("you choose and document the choice in the
kitchen; if later we change mind we will have history and can adjust"). Each entry records the
choice and the reason. To change one, add a dated entry below it; don't rewrite history.

1. **Status model: a closed enum, `FeedbackStatus`,** following the `FeedbackKind` pattern. Values:
   **New, UnderReview, Planned, InProgress, Completed, Declined.**
   - **Transitions:** filing sets `New`. An admin can then move an item to any other status
     *except* back to `New`. `New` means "nobody has looked yet", and returning to it would lie.
     Reopening (from `Completed` / `Declined` to `UnderReview`) is allowed.
   - **History:** every change is recorded as a status-change entry (from, to, who, when) in the
     item's activity, so the filer and admins can see what happened. The aggregate exposes it as a
     named mutation, `ChangeStatus(...)`, per `tw-aggregate-pattern`.
   - *Why:* this is the de-facto set across Canny, UserVoice and similar tools (see the reference).
     A closed enum gives typed switches and simple storage. Custom statuses are not needed for a
     template.
2. **Replies: private to the filer only, with no public comments.** Feedback is private to its
   filer today, and a template shouldn't silently make it public.
   - A reply is an **in-app thread entry**, visible on the filer's feedback permalink and in the
     admin detail view. The filer also gets an **email copy** when an address is known, through the
     existing receipt-email path (`IProfileEmailLookup` + the 273 sender).
   - The filer can **answer in the same thread** from the permalink, so the conversation stays in
     one place.
   - *Why:* the private reply is the core admin need. Public boards and voting are a different
     product (a roadmap board) and can come later.
3. **Filer notifications on status change.**
   - **In-app:** always. The filer's list and permalink show the current status and the activity
     history.
   - **Email:** only on changes to **Planned, Completed or Declined**, and on every admin reply.
     `UnderReview` / `InProgress` churn would be noise.
   - *Why:* this closes the loop as the reference describes, without spamming.
4. **Who administers: new permissions `admin.feedback.read` and `admin.feedback.manage`**, added to
   `PermissionIds`, with policies registered (TWA0024), and granted to **Administrator** in
   `RolePermissionSeed`. They are not granted to Operator, whose grants are reserved until the
   marketplace (118) lands, per `role-ids-contracts.cs`.
   - `read` covers the list, detail and attachment download. `manage` covers status changes,
     replies and internal notes.
   - *Why:* this follows the existing `admin.roles.*` / `admin.principals.*` read/manage split.
5. **Triage extras.**
   - **Internal notes: in this task.** A thread entry with admin-only visibility, never shown to the
     filer, so it costs almost nothing beside replies.
   - **Deferred, not in this task:** assignee, tags, merging duplicates, Kind re-classification.
     File them as follow-up tasks if the need appears.
   - *Why:* this keeps the first slice to "see it, triage it, answer it". Each deferred feature
     adds UI and policy surface that should be justified by real use.
6. **Retention and deletion: no admin delete in this task.** `Completed` and `Declined` are the
   end states, and the list defaults to hiding them, with a filter to show them. The filer's own
   abilities stay unchanged.
   - Admin hard-delete, together with privacy and erasure requests (deleting an item with its
     attachments and thread), is a deliberate separate task when needed.
   - *Why:* deletion of user-submitted content, and the attachments in blob storage, needs its own
     policy and an audit story. Hiding closed items covers day-to-day hygiene.

## Implementation shape (follows the decision log)

- Domain: `FeedbackItem` gains `Status` and an activity thread, with named mutations
  `ChangeStatus(...)`, `AddReply(...)` (admin or filer) and `AddInternalNote(...)`, plus their
  invariants, per `tw-aggregate-pattern`. Thread entries carry author, kind (status change / reply
  / internal note), visibility and timestamp. Update the Design region, which currently says
  "no later mutations", and add an EF migration.
- Store port: `ListAllAsync` (paged and filterable by status / kind / date) and `UpdateAsync`. EF
  index for the cross-owner query.
- Contracts and handlers: `ListFeedback` (admin, filter by status / kind / date, closed hidden by
  default), `GetFeedbackForAdmin`, `ChangeFeedbackStatus`, `ReplyToFeedback` (admin),
  `AddFeedbackNote` (admin), `ReplyAsFiler` (filer, own items), with `[EndpointAuthorize]` on the new admin permissions. Each gets co-located
  Jaribu tests covering the happy path, validation, and authorization denial.
- SPA: an admin feedback list (filters plus status column) and a detail page (body, attachments,
  status change, reply), under the admin area, mirroring `features/admin/principals`. Every
  interaction is a TimeWarp.State action (TWA0022 / TWA0026), and outcomes go through
  `NotificationState`.
- The filer's view (`list-my-feedback` / permalink) shows status and the non-internal activity
  thread, with a reply box.
- Email: reuse the receipt sender; send on admin replies and on Planned / Completed / Declined.
  Tests use the in-memory mail capture.

## Checklist

- [x] Decisions 1–6 recorded (decision log, 2026-10-12)
- [ ] Domain: `FeedbackStatus`, activity thread, named mutations + invariants; EF migration
- [ ] Store: cross-owner `ListAllAsync` (paged/filtered) + `UpdateAsync`; EF index
- [ ] Permissions `admin.feedback.read` / `.manage` (policies, seed → Administrator)
- [ ] Admin contracts/handlers + co-located Jaribu tests (happy, validation, authz denial)
- [ ] Filer view: status + thread + reply
- [ ] Email on admin reply and Planned/Completed/Declined
- [ ] SPA admin list + detail (State actions only; NotificationState outcomes)
- [ ] Gates: `dev build` 0/0, `dev test`, `dev template-smoke`, `ganda repo audit`

## Session

- Created: 2026-10-12 (cockpit, per Steve). Reference conversation captured under `references/`.
- 2026-10-12: Steve delegated decisions 1–6 to the cockpit; recorded in the decision log. Moved to to-do.

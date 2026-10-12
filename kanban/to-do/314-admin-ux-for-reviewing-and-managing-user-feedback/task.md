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
- [x] Domain: `FeedbackStatus`, activity thread, named mutations + invariants; EF migration
- [x] Store: cross-owner `ListAllAsync` (paged/filtered) + `UpdateAsync`; EF index
- [x] Permissions `admin.feedback.read` / `.manage` (policies, seed → Administrator)
- [x] Admin contracts/handlers + co-located Jaribu tests (happy, validation, authz denial)
- [x] Filer view: status + thread + reply
- [x] Email on admin reply and Planned/Completed/Declined
- [x] SPA admin list + detail (State actions only; NotificationState outcomes)
- [x] Gates: `dev build` 0/0, `dev test`, `dev template-smoke`, `ganda repo audit`

## Session

- Created: 2026-10-12 (cockpit, per Steve). Reference conversation captured under `references/`.
- 2026-10-12: Steve delegated decisions 1–6 to the cockpit; recorded in the decision log. Moved to to-do.
- 2026-10-12: Implemented by the headless implementer (ganda task work). Server, migration, and tests by
  the implementer; the SPA slice by a delegated subagent, then reviewed. Gates below.
- 2026-10-12: Review oracle (ganda task work, headless implementer-claude). Effort 3, roster general
  split into `general-server` / `general-spa` (Claude Sonnet subagents). Fixes on this id.

## Results

**Domain.** `FeedbackItem` has a `Status` (`FeedbackStatus`: New, UnderReview, Planned, InProgress,
Completed, Declined) and an owned, append-only thread of `FeedbackActivity` entries (kinds: StatusChange,
AdminReply, FilerReply, InternalNote; keyed by item id + `Sequence`). Named mutations: `ChangeStatus`
(never back to New, never to the current status; reopening is allowed), `AddAdminReply`,
`AddFilerReply` (owner only) and `AddInternalNote`. The `Invariants` validator checks that sequences are
dense, that `Status` equals the last change's `To`, the per-kind shape, and that a filer reply is
written by the owner. Migration `20261012115946_AddFeedbackTriage`: adds `Status` (existing rows
default to New), the `feedback_activity` table, a `(Status, FiledAt)` index, and the Administrator
grants.

**Deviation from the shape text:** `AddReply(...)` is split into `AddAdminReply` / `AddFilerReply`,
so the author role is part of the mutation's name and not a flag. Visibility is derived from the
kind (`IsVisibleToFiler`) and not stored, so the two can never disagree.

**Store.** `IFeedbackStore.ListAllAsync(FeedbackListFilter)` (paged; filters by status set, kind and
date range; newest first) and `UpdateAsync` (returns false on a Version or key conflict, which
handlers turn into 409). The EF store attaches the detached item, marks `Status` modified and inserts
only the entries above the stored maximum sequence. The in-memory store has no Version check; this
is documented in its Design region.

**Permissions.** `admin.feedback.read` and `admin.feedback.manage` are in `PermissionIds.All`, their
policies are auto-registered, and they are seeded to Administrator only (seed and migration). Because
they are `admin.*`, they join Administrator's protected core. The mock role payloads were updated.
Attachment download for admins now needs `admin.feedback.read` instead of `admin.access`, per
decision 4 ("read covers attachment download").

**Contracts and handlers** (in `web/features/feedback/`):
- `ListFeedback`: `GET api/admin/feedback`; closed items are hidden unless `IncludeClosed` or an
  explicit `Status`.
- `GetFeedbackForAdmin`: `GET api/admin/feedback/{id}`.
- `ChangeFeedbackStatus`: `PUT .../{id}/status`.
- `ReplyToFeedback`: `POST .../{id}/replies`.
- `AddFeedbackNote`: `POST .../{id}/notes`.
- `ReplyAsFiler`: `POST api/Feedback/{id}/replies`.

Admin mutations return the updated `FeedbackAdminView`. `GetFeedback` and `ListMyFeedback` now carry
`Status`. `GetFeedback` also carries the filer thread, built by `FeedbackContractMap.FilerThread`,
which drops internal notes and admin ids.

**Email.** `FeedbackFilerMail` reuses the 273 path (`IProfileEmailLookup` + `IEmailSender`) and is
best-effort after the commit. It sends on every admin reply and on changes to Planned, Completed or
Declined (`FeedbackStatusRules.NotifiesFiler`). It never sends for notes.

**SPA.**
- `web-spa/features/admin/feedback/`: `FeedbackAdminState` plus `/Admin/Feedback` (filters, status
  column, paging) and `/Admin/Feedback/{id}` (thread with internal notes marked). The manage
  controls sit behind `AuthorizeView` on `admin.feedback.manage`.
- The filer's list shows status. The permalink shows status, the thread and a reply box
  (`ReplyAsFilerActionSet`).
- All clicks dispatch State actions, and success toasts go through `OutcomeNotification`.

**Deferred (decision 5/6):** assignee, tags, merge duplicates, kind re-classification, admin delete,
and erasure. No follow-up tasks have been filed.

### Gates

- `dev build`: 0 warnings, 0 errors.
- `ganda repo audit`: all checks pass.
- `dev template-smoke`: SUCCEEDED.
- `dev check-version`: source 2.0.0-beta.20 is ahead of NuGet beta.19, so no bump is needed.
- `dev test`: every project is green except one failure in aspire-tests,
  `IngressSmoke_Given_.RolesThroughIngress_Should_Forbidden_Given_MockPrincipal_WithoutAdminRole`
  (expected 403, got 401). It fails the same way on the untouched base commit `d6a8e7103` (checked in
  a scratch checkout), so it predates this task. It is not fixed here.
- New tests: `features/feedback/feedback-triage-tests.cs` (28/28),
  `web-server-integration-tests/features/feedback/admin-feedback-authorization-tests.cs` (passing in
  the `Returns_` run, 139/139), and `web-spa-integration-tests/features/admin/feedback-admin-state-tests.cs`
  (4/4).

### How to validate

**Smoke:**
1. `dotnet run source/container-apps/web/features/feedback/feedback-triage-tests.cs`
2. `dotnet run --project tests/container-apps/web/web-server-integration-tests -c Release -- --filter-class Returns_`
3. Manual: `dev run`, then sign in with a passkey as user A (the first human becomes Administrator).
   - As user B (a Member), file feedback at `/Feedback`.
   - As A, open `/Admin/Feedback`, open the item, change the status to Planned, send a reply, and
     add an internal note.
   - As B, reopen the permalink and answer in the thread.

**Expect:**
1. 28/28 pass, and the run prints `TRIAGE-PROOF ... status=Declined mails=3`.
2. `failed: 0`. The admin feedback endpoints return:
   - anonymous: 401;
   - Member: 403;
   - Administrator: 200;
   - an Administrator with only `admin.feedback.read`: 403 on a status change or note.
3. In the manual run:
   - B's permalink shows status Planned and A's reply, and never shows the note.
   - B gets one email for Planned and one for the reply (when B's profile has an email).
   - A's admin detail shows all four entries, with the note marked internal.
   - The list hides the item once it is Completed or Declined, unless "show closed" is ticked.

### Review disposition

- **Rounds:** 1. **Roster:** general, split into `general-server` and `general-spa`. **Effort:** 3 (by-diff budget, 4588 lines).
- **Final counts:**
  - bug: 2 fixed (M1, and M9 which the new Postgres test surfaced).
  - suggestion: 4 fixed.
  - nit: 3 fixed.
  - 0 open and 0 wontfix.
- **Disposition:** `clean`.
- **Fixes:**
  - M1: the attachment download route no longer gates on `feedback.file.self`, so a read-only feedback admin can download.
  - M9: `EfFeedbackStore.AddAsync` clears the change tracker.
  - M3: `ListFeedback.MaxPage` bounds the row offset.
  - M5 / M6: admin SPA state no longer shows a stale item or stale rows. There is a new `ListLoadError`.
  - M7: pager wording and visibility.
  - M2 / M8: new Postgres store tests, SPA state tests, and an admin-download integration test.
  - M4: the store tie-break order is documented.
- **Gates after fixes:**
  - `./bin/dev build`: 0/0.
  - `ganda repo audit`: pass.
  - web-server-integration `Returns_`: 140/140.
  - web-infrastructure-tests: 70/70.
  - SPA feedback admin state: 7/7.
  - triage runfile: 28/28.
  - attachment runfile: 20/20.
- **Artifacts:**
  - `review/review-framework.md`
  - `review/round-1/general-server.md`, `review/round-1/general-spa.md`, `review/round-1/merged.md`
  - `review/disposition.md`

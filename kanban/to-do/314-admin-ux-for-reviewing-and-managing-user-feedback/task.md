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

## Decisions to make with Steve (BEFORE implementation — this is why the task is in backlog)

1. **Status model.** Pick the set (for example Open, Under Review, Planned, In Progress,
   Completed, Declined) and whether it is a closed enum, like `FeedbackKind`, or a Bogard
   `Enumeration`. Decide whether every transition is allowed or there are rules.
2. **Replies.** Private replies to the filer only, or public comments too? Feedback today is private
   to its filer, so public comments would change that. Decide whether a reply is an email, an
   in-app thread the filer sees on the feedback permalink, or both.
3. **Notify the filer on status change?** If so, by email, in-app, or both, and for which
   transitions.
4. **Who administers.** A new `admin.feedback.read` / `admin.feedback.manage` pair granted to
   Administrator (and maybe Operator), following the existing `admin.*` permission pattern.
5. **Triage extras in this task, or later.** Assignee, internal notes (never shown to the filer),
   tags, merging duplicates, and Kind re-classification.
6. **Retention and deletion.** Can admins delete feedback and its attachments, and is there an
   archive state?

## Likely shape once decided (not a commitment)

- Domain: `FeedbackItem` gains a status (and possibly an assignee) with named mutations,
  `SetStatus(...)` etc., per `tw-aggregate-pattern`. A separate child entity or aggregate holds
  replies and notes. Update the Design region, which currently says "no later mutations".
- Store port: `ListAllAsync` (paged and filterable by status / kind / date) and `UpdateAsync`. EF
  index for the cross-owner query.
- Contracts and handlers: `ListFeedback` (admin), `GetFeedbackForAdmin`, `SetFeedbackStatus`,
  `ReplyToFeedback`, with `[EndpointAuthorize]` on the new admin permissions. Each gets co-located
  Jaribu tests covering the happy path, validation, and authorization denial.
- SPA: an admin feedback list (filters plus status column) and a detail page (body, attachments,
  status change, reply), under the admin area, mirroring `features/admin/principals`. Every
  interaction is a TimeWarp.State action (TWA0022 / TWA0026), and outcomes go through
  `NotificationState`.
- The filer's view (`list-my-feedback` / permalink) shows status, and replies if decided.

## Checklist

- [ ] Decisions 1–6 recorded here (with Steve)
- [ ] Spec finalized, then move this task to to-do (Definition of Ready)

## Session

- Created: 2026-10-12 (cockpit, per Steve). Reference conversation captured under `references/`.

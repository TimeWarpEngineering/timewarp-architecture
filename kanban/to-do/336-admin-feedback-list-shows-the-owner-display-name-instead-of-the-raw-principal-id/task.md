# Admin feedback list shows the owner display name instead of the raw principal id

## Description

The admin feedback list (`/Admin/Feedback`, tasks 314 and 327) and the admin detail page
show the filer as a raw principal id (`OwnerPrincipalId`). Task 328 added the platform port
`IProfileDirectory` (`platform/profile/i-profile-directory-application.cs`, with batch lookup
`FindManyAsync`) and the `admin.profiles.read` permission. Use them to show the owner's
**display name**, linked to `/Admin/Principals/{id}` (328's detail page).

## Requirements

- **PII gating, the same as 328.**
  - With `admin.profiles.read`: show the display name, with the email in a tooltip, or just the
    name; record the choice. The name links to the principal detail page.
  - Without it: show the principal id as today, still linking to the detail page, which itself
    hides the profile.
  - No profile row: show the id and "no profile".
- **One batch lookup per page.** `ListFeedback` and `GetFeedbackForAdmin` resolve owners through
  `IProfileDirectory.FindManyAsync` for the page's distinct owner ids. No per-row calls.
  - Contract rows gain `OwnerDisplayName?`, populated only when the caller has
    `admin.profiles.read`, checked via `IPermissionEvaluator` as in `ListPrincipals`.
  - Feedback must reach profiles only through the platform port (TWA0009).
- **Sorting by owner** (optional). If it's added, follow 328's rule: sorting by a PII field
  without `admin.profiles.read` returns 403, so ordering can't leak data. Otherwise, keep Owner
  unsortable and say so.
- **SPA:** in the 327 `FluentDataGrid`, the Owner column shows the name (or id) as a link. The
  detail page shows the same. Every interaction is a State action.
- **Tests:**
  - handler: with and without the permission, a missing profile, one batch call per page;
  - HTTP: no-PII for an Operator-only principal with `admin.access` + `admin.feedback.read` but
    no `admin.profiles.read`, mirroring 328's test;
  - SPA state test;
  - a Playwright check that the Owner cell shows the name and links to the detail page.
- Gates: `dev build` 0/0, `dev test`, `dev template-smoke`, `ganda repo audit`.

## Checklist

- [ ] Batch owner lookup via `IProfileDirectory`; `OwnerDisplayName?` gated on `admin.profiles.read`
- [ ] List + detail UI show name/id linked to `/Admin/Principals/{id}`
- [ ] Owner sort decision recorded (403 rule if added)
- [ ] Tests incl. no-PII path
- [ ] Gates

## Session

- Created: 2026-10-13 (cockpit; follow-up suggested by the 328 worker, approved by Steve)

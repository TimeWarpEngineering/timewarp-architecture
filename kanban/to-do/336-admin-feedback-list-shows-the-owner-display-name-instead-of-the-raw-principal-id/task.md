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

- [x] Batch owner lookup via `IProfileDirectory`; `OwnerDisplayName?` gated on `admin.profiles.read`
- [x] List + detail UI show name/id linked to `/Admin/Principals/{id}`
- [x] Owner sort decision recorded (403 rule if added)
- [x] Tests incl. no-PII path
- [x] Gates

## Session

- Created: 2026-10-13 (cockpit; follow-up suggested by the 328 worker, approved by Steve)
- 2026-10-13: implemented under `ganda task work` (implement oracle).
- Implement oracle: implemented by implementer-claude (claude, model claude-opus-5-5), session not reported, max-turns 500, Done — 2026-10-13T03:21:15Z

## Results

### Decisions

- **Name only, no email tooltip.** The Owner cell and the detail page show the display name.
  The email stays on the principal detail page (328), which the name links to. Less PII on the
  feedback wire. The cell tooltip keeps the raw principal id.
- **Owner stays unsortable.** The feedback store sorts and pages in the database, and the name
  lives in the profile slice. A name sort would have to load every match. If it is ever added,
  328's rule applies: sorting by a PII field without `admin.profiles.read` returns 403. Recorded in
  the `FeedbackListSortColumn` and `FeedbackAdminListPage` Design regions.
- **One gate, every admin view.** `FeedbackOwnerNames` (new, scoped, feedback application) checks
  `admin.profiles.read` through `IPermissionEvaluator` (identity-session scheme, as
  `ListPrincipals`) and only then makes one `IProfileDirectory.FindManyAsync` call over the
  distinct owner ids. Without the permission the directory is never asked. `ListFeedback` calls it
  once per page. `FeedbackContractMap.AdminViewAsync` calls it for the detail. The view is also
  returned by ChangeStatus, Reply, and AddNote, so a mutation does not blank the name.
- **"No profile".** The SPA's `FeedbackOwnerLink` shows the id when there is no name. Inside
  `AuthorizeView Policy=admin.profiles.read` it adds "(no profile)", because a caller who may
  read profiles and got no name means the owner has no profile row.

### Changes

- Contracts: `ListFeedback.Item.OwnerDisplayName?`, `FeedbackAdminView.OwnerDisplayName?`
  (optional ctor args; mock factories carry "Ada Lovelace").
- Application: `feedback-owner-names-application.cs`, registered in
  `InMemoryFeedbackStoresModule`. List, GetForAdmin, ChangeStatus, ReplyToFeedback, and AddNote
  handlers take it.
- SPA: `features/admin/feedback/components/FeedbackOwnerLink.razor` is used by the list grid's
  Owner column and the detail page's Owner fact. It is a plain `Href` link to
  `/Admin/Principals/{id}`, so there is no page-local navigation method.
- Tests:
  - `feedback-triage-tests.cs` `FeedbackOwnerName_Given_`: with and without the permission, a
    missing profile, one batch per page with distinct ids, an empty page with no call, the
    detail and mutation views, and a JSON round trip. Uses the real `PermissionEvaluator`.
  - `admin-feedback-authorization-tests.cs`: an Administrator sees the name on the list and the
    detail. An Operator with `admin.access` + `admin.feedback.read` and no `admin.profiles.read`
    gets no name in either JSON.
  - `feedback-admin-state-tests.cs`: the name lands in state for list rows and the open item,
    and survives a note.
  - Playwright `AdminListOwnerCell_Should_ShowTheDisplayName_And_LinkToThePrincipal`.

### Gates

- `dev build`: succeeded, 0 warnings, 0 errors.
- Tests: all 23 test projects passed, each run with `dotnet test -c Release` (the same command
  `dev test` runs per project). This includes web-server-integration (330, 1 skipped, which was
  already skipped), web-spa-integration (215), web-spa-playwright (11), and web-jaribu (329).
  The co-located runfile `feedback-triage-tests.cs` passed on its own (5 new tests).
- `dev template-smoke`: SUCCEEDED.
- `ganda repo audit`: passes all checks.

### How to validate

**Smoke:**

```bash
dotnet run source/container-apps/web/features/feedback/feedback-triage-tests.cs
(cd tests/container-apps/web/web-server-integration-tests && dotnet test -c Release)
(cd tests/container-apps/web/web-spa-playwright-tests && dotnet test -c Release)
```

Manual check: `dev run`, sign in with a passkey (the first account is Administrator), file
feedback at `/Feedback`, then open `/Admin/Feedback`.

**Expect:**

- `FeedbackOwnerName_Given_` passes 5/5.
- The server suite includes `Ok_Owner_Display_Name_Given_Administrator` and
  `Ok_Feedback_Reader_Without_Profiles_Read_Sees_No_Owner_Name`, and both pass.
- The Playwright suite passes `AdminListOwnerCell_Should_ShowTheDisplayName_And_LinkToThePrincipal`.
- In the browser, the Owner column shows your display name ("Member" by default), not a GUID.
  Clicking it opens `/Admin/Principals/{id}`. The item detail page shows the same link.
- A caller without `admin.profiles.read` sees the raw id, still linked, and no "(no profile)".

### Review disposition

- Rounds: 1. Effort 2, roster: general (subagent ae31e2d3b3e3aafcb, sonnet).
- Final counts: bug 0, suggestion 0, nit 1 (fixed). 0 open.
- Disposition: **clean**. M1 re-wrapped the Design comment in `feedback-list-sort-column-contracts.cs`.
- Artifacts: `review/review-framework.md`, `review/round-1/general.md`, `review/round-1/merged.md`,
  `review/disposition.md`.

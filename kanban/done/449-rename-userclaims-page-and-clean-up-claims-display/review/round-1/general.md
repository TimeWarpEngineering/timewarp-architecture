# Round 1 — general
**Date:** 2026-10-30
**Scope reviewed:** commit 23515a3ae vs master (full diff), plus call sites: `BaseComponent`
auth partial, `EmptyState`, `TableHeader`, `twe-table` css, `ServicesPage` / `ServiceList`
pattern, `PermissionIds.ClaimType`, and a repo-wide sweep for leftover `UserClaims` references.

## Summary

The change renames the developer claims page and its list component, relabels the data as
`AuthenticationState` with the provider named in the page header, and replaces the old
`AuthMessage` / literal `_authMessage` / dead `ReturnClaims` rendering with a `twe-table`
Type / Value table and two `EmptyState` fallbacks. Risk is low: a developer-only diagnostic
page behind `DeveloperClaimsRead`, no contract or data changes, and the page registry test
updated. Everything in the restatement's acceptance criteria is present in the diff.

## Verification

- Restatement vs original task: no misread. The task title and checklist ask for a rename,
  route/Title review, honest source label, `AuthMessage` removal, the `_authMessage` fix, and the
  empty-filter fix; the restatement commits to concrete names (`ClaimsPage` / `ClaimList`,
  `/Developer/Claims`) that follow the existing `ServicesPage` / `ServiceList` split.
- `BaseComponent` exposes `[Inject] protected AuthenticationStateProvider` in
  `features/base/base-component.auth.cs:18`, so `ClaimsPage.IsMockAuthentication` resolves.
- `EmptyState` captures unmatched attributes onto its root div (`EmptyState.razor:39,54`), so
  `data-qa="ClaimListEmpty"` / `ClaimListSignedOut` land in the DOM. `twe-table` is a global
  class in `wwwroot/css/app.css:80-82`, used by `ServiceList`, `RolesListPage`, and others.
- `ShortClaimType`: `timewarp:principal_id` and `permission` (PermissionIds.ClaimType) have no
  slash and render unchanged; `.../claims/role` and `.../claims/nameidentifier` shorten to their
  last segment with the full type on the cell title. A trailing slash falls back to the full
  type. Correct.
- `git grep -i 'userclaims\|user claims' -- . ':!kanban'`: no hits in source, tests, skills,
  documentation, goldens, or timewarp-templates. The old route is gone with no redirect, which
  Results disclose; acceptable for a developer diagnostic page.
- Gates re-run in this worktree: `dev build` 0 warnings / 0 errors; `dev test -g fast` 14/14;
  web-spa-integration-tests 569/569 (includes `page-registry-tests` with `ClaimsPage`);
  `ganda repo audit` passes with the same `kanban-landed` advisory the implementer reported
  (other tasks' claims, outside this change).
- Purpose region: `ClaimsPage.razor.cs` keeps `#region Purpose`; `ClaimList.razor` carries a
  Purpose / Design comment. `.razor` files are outside TWA0004, so this is consistent with the
  repo.

## Issues

### Issue 1 — Severity: nit
- File: kanban/in-progress/449-rename-userclaims-page-and-clean-up-claims-display/task.md:20
- Description: The checklist item "Verify admin + developer views" is ticked, but Results state
  no live browser check of either view was done (headless worker, no AppHost). The tick
  overstates what was verified. The automated gates cover registration and compile, not the
  rendered page.
- Suggestion: Leave the tick but keep the "How to validate" steps in Results as the operator
  smoke before or after merge; workers must not run AppHost in a task worktree.
- Status: open

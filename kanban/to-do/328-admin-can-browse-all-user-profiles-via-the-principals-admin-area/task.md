# Admin can browse all user profiles via the Principals admin area

## Description

Steve, 2026-10-12: "we have a profile for each user but the Admin has no way to see all
profiles."

Today:
- `Profile` (`features/profile/profile-domain.cs`) is keyed 1:1 by the principal id
  (`Create(ProfileId, …)` is the principal-key path, task 148). Its fields are DisplayName,
  Email, Language, Region, Theme and Notifications.
- `IProfileStore` only has `FindAsync` / `AddAsync` / `UpdateAsync`, with no list.
- The admin area has `/Admin/Principals` (`features/admin/principals/list-principals`, task 147-004)
  with flat rows (PrincipalId, Kind, TrustTier, IsActive, IsQuarantined, RoleIds) and **no detail
  page**. Principals include agents, which have no human profile.

## Decision log (2026-10-12, cockpit defaults; change by appending a dated entry)

Following the 314 practice: these are recorded choices, open to revision with history.

1. **One "users" surface: extend Principals; no parallel Profiles list.** Two admin lists of the
   same people would drift. The Principals list gains **Display name** and **Email** columns from
   the profile. Agents and principals without a profile show "—". A new **principal detail page**,
   `/Admin/Principals/{id}`, shows the full profile (all fields) plus the existing roles and
   status.
2. **Permission: a new `admin.profiles.read`**, granted to Administrator only (seed + migration +
   policy registration; TWA0024). Profile data is PII (email), so it gets its own read permission
   separate from `admin.principals.read`. Without it, the list shows "—" for profile columns and
   the detail page hides the profile section. It doesn't fail the page.
3. **Read-only.** Admins can't edit another user's profile in this task. Admin edit, and privacy
   or erasure requests, would be separate tasks with an audit story.
4. **Search and sort:** filter by display name or email (contains, case-insensitive), and sort by
   display name / email / kind. Use the server-side `FluentDataGrid` pattern from **task 327**. If
   327 has not merged, follow its spec and coordinate.

## Requirements

- **Slice isolation (TWA0009).** Admin.Principals must not reference the Profile product slice
  directly. Expose what admin needs through a platform port, as `IProfileEmailLookup` already does
  under `platform/profile/`. For example, `IProfileDirectory`, with a batch lookup by principal ids
  and the search used by the list, implemented by the profile infrastructure. Record the placement
  in Design regions (`tw-slice-isolation`).
- **Store:** batch fetch by ids, plus search (contains on display name / email), in the EF and
  in-memory stores. Add an EF index if the search needs it, and record it.
- **Contracts and handlers:**
  - `ListPrincipals` rows gain `DisplayName?` / `Email?`, populated only when the caller has
    `admin.profiles.read`, plus the search and sort parameters;
  - new `GetPrincipal` (detail) returning roles, status and the profile (gated the same way);
  - `[EndpointAuthorize]` on the existing `admin.principals.read`, with profile fields gated
    inside on `admin.profiles.read`. Co-located Jaribu tests cover: happy path; validation; an
    admin without `admin.profiles.read` sees no PII; Member 403; anonymous 401.
- **SPA:**
  - Principals list: new columns, search box and sortable grid (327 pattern);
  - detail page with the profile card and existing role management (move the role editing there if
    it fits; record the choice).

  All interactions are State actions (TWA0022 / TWA0026), and outcomes go through
  `NotificationState`. Textareas, if any, use the shared component from task 326.
- **Follow-up hook (note only):** once profiles are reachable through the port, the admin feedback
  list's Owner column (task 327) can show a display name instead of the raw principal id. File it
  as a follow-up; don't do it here.

## Checklist

- [ ] Platform port for profile lookup/search (TWA0009-clean) + EF/in-memory implementations
- [ ] `admin.profiles.read` (PermissionIds, policy, seed, migration)
- [ ] `ListPrincipals` profile columns + search/sort; `GetPrincipal` detail
- [ ] SPA list columns/search/sort + principal detail page
- [ ] Tests incl. PII gating
- [ ] Gates: `dev build` 0/0, `dev test`, `dev template-smoke`, `ganda repo audit`

## Session

- Created: 2026-10-12 (cockpit, per Steve)

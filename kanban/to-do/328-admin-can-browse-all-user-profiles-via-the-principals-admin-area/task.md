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

- [x] Platform port for profile lookup/search (TWA0009-clean) + EF/in-memory implementations
- [x] `admin.profiles.read` (PermissionIds, policy, seed, migration)
- [x] `ListPrincipals` profile columns + search/sort; `GetPrincipal` detail
- [x] SPA list columns/search/sort + principal detail page
- [x] Tests incl. PII gating
- [x] Gates: `dev build` 0/0, `dev test`, `dev template-smoke`, `ganda repo audit`

## Session

- Created: 2026-10-12 (cockpit, per Steve)
- 2026-10-13: implemented under `ganda task work` (implement oracle). Gates re-run in this
  worktree after the implementation pass (see Results).
- Review oracle: claude (claude-opus-5-5), effort 3, roster general, 2 rounds, disposition clean — 2026-10-13
- Implement oracle: implemented by implementer-claude (claude, model claude-opus-5-5), session not reported, max-turns 500, Done — 2026-10-13T02:05:06Z
- Review oracle: review by implementer-claude (claude, model claude-opus-5-5), session not reported, max-turns 200 — 2026-10-13T02:18:20Z

## Results

### What changed

All paths are under `source/container-apps/web/` unless noted.

- **Platform port (TWA0009).** `platform/profile/i-profile-directory-application.cs` adds
  `IProfileDirectory`, with `FindManyAsync(principalIds)` (batch, missing ids absent) and
  `SearchAsync(term)` (principal ids whose display name or email contains the term,
  case-insensitive). Rows are `ProfileDirectoryEntry` (`profile-directory-entry-application.cs`).
  The profile slice implements it in `features/profile/profile-directory-application.cs`
  (scoped, never creates a profile). `InMemoryProfileStoresModule` registers it. Admin.Principals
  references only the port.
- **Store.** `IProfileStore` gains `FindManyAsync` and `SearchAsync`. `InMemoryProfileStore`
  uses `OrdinalIgnoreCase`. `EfProfileStore` uses `AsNoTracking`, `ids.Contains`, and
  lower-cased `Contains` (the translatable form; there is a local CA1862 pragma with a reason).
  **No EF index.** A leading-wildcard contains cannot use a btree index, and the admin list is
  small. The upgrade path is a pg_trgm GIN index. This is recorded in `EfProfileStore` and the
  Profile entity configuration Design regions.
- **Permission `admin.profiles.read`.** `PermissionIds.AdminProfilesRead` comes after
  `AdminPrincipalsManage` in `All`, so the server and SPA policies register automatically. It is
  in `RolePermissionSeed.AdminPermissions` (Administrator only, protected core). Migration
  `20261013120000_AddAdminProfilesReadPermission` inserts the Administrator grant, and Down
  deletes it. The model snapshot only moves its migration id. The role mock lists in get-role,
  get-roles and set-role-permissions include the new id.
- **ListPrincipals.** Rows gain `DisplayName?` / `Email?`. The query gains `Search`, `SortBy`
  (`PrincipalListSortColumn`: CreatedAt, DisplayName, Email, Kind; default CreatedAt) and
  `SortDescending` (default false), all in the query string. The validator checks `SortBy`
  IsInEnum and caps `Search` at 254 characters. The handler checks the caller's
  `admin.profiles.read` through `IPermissionEvaluator`. Without it, the profile fields are null.
  A search, or a sort by display name or email, returns **403** (`PrincipalProblems.ProfileAccessRequired`),
  so the ordering cannot leak PII. Sorting runs in memory over the whole list (the list has no
  paging). Display name and email sort with nulls last in both directions, and ties break by
  store order.
- **GetPrincipal (new).** `GET api/admin/principals/{PrincipalId}` uses the same
  `[EndpointAuthorize]` as the list (`admin.principals.read`, identity-session + mock). It
  returns `PrincipalDetailDto` (status, effective roles, `Profile?`). The profile is filled only
  with `admin.profiles.read` and is never created. A missing principal is 404.
- **SPA.**
  - `/Admin/Principals` is now a `FluentDataGrid` with the task 327 server-sort pattern
    (`RefreshItems` → `SetSort` → `FetchPrincipals`). Columns: Principal (link to detail),
    Display name, Email, Kind, Trust, Active. "—" shows when there is no value. The search box
    (`SetSearch` → `FetchPrincipals`) sits in an `AuthorizeView` on `admin.profiles.read`, and
    Display name / Email are sortable only with that permission.
  - The new `/Admin/Principals/{id}` page (`PrincipalDetailPage`, not navigable) shows status
    and roles. The profile card sits in an `AuthorizeView` on `admin.profiles.read`. The role
    editor sits in an `AuthorizeView` on `admin.principals.manage`.
  - **Choice: role editing moved to the detail page.** Per-row checkbox grids do not fit a data
    grid, and one edit surface is easier to keep correct. This is recorded in the
    `PrincipalsPage.razor.cs` Design region.
  - `OpenPrincipal` seeds the role drafts, so refetching the list cannot clobber an edit.
  - `FetchPrincipals` failure clears the rows and sets `ListLoadError`, like the feedback list.
  - All interactions are State actions. Problems surface through the `DefaultApiHandler`
    notification path. There are no textareas.

### Tests

- `features/admin/principals/list-principals/list-principals-tests.cs` (extended) covers:
  - the profile columns with the permission, and null without it (no PII);
  - agents and rows with no profile;
  - case-insensitive search on name and email;
  - 403 for a search or a profile sort without the permission;
  - each sort column in both directions;
  - the validator, the query string, the `[EndpointAuthorize]` policy, and the seed
    (Administrator has it, Member does not).
- `features/admin/principals/get-principal/get-principal-tests.cs` (new) covers:
  - the happy path with a profile, and a null profile when none exists;
  - no profile without the permission;
  - 404, the validator, the JSON round-trip, and the endpoint policy.
- `features/profile/get-profile/get-profile-tests.cs` covers the in-memory `FindManyAsync`,
  `SearchAsync` and `ProfileDirectory`.
- `tests/.../web-infrastructure-tests/profile-postgres-persistence-tests.cs` covers EF
  `FindManyAsync` and `SearchAsync` on live Postgres.
- `tests/.../web-server-integration-tests/.../principals-authorization-tests.cs` covers, over
  HTTP:
  - GetPrincipal: anonymous gets 401, Member gets 403, and an admin gets 200 with the profile;
  - an Operator-only principal granted `admin.access` + `admin.principals.read` (no
    `admin.profiles.read`) sees no display name or email in the list or the detail, and their
    search gets 403.
- `tests/.../web-spa-integration-tests/features/admin/principal-state-tests.cs` (new) checks
  that SetSort and SetSearch reach the fetch query, and that a 404 from OpenPrincipal sets
  LoadError.
- The permission-set tests (permission-ids, permission-evaluator, set-role-permissions-lockout)
  include the new id.

### Gates (2026-10-13, this worktree)

- `dev build`: 0 warnings, 0 errors
- `dev test`: all projects passed (exit 0)
- `dev template-smoke`: SUCCEEDED (web-jaribu aggregate 324/324)
- `ganda repo audit`: passes all checks
- `dev check-version`: source 2.0.0-beta.20 is ahead of NuGet beta.19, so no bump is needed

### Review disposition

- Effort 3 (by-diff budget, 2668 lines). Roster: general. Rounds: 2 (round 2 re-verified the fixes).
- Final counts: bug 0; suggestion 1 fixed; nit 2 fixed; 0 open; 0 wontfix.
- **Disposition: clean.**
  - M1: the no-PII HTTP test and the validation steps now use an Operator role with
    `admin.principals.read`, because Administrator cannot drop `admin.profiles.read`
    (protected core).
  - M2: the ASCII-only case-folding caveat is recorded in the store Design regions.
  - M3: a generic LoadError now shows on non-404 detail failures.
- After the fixes: `dev build` reports 0 warnings and 0 errors, and `dev test` passes.
- Artifacts: `review/review-framework.md`, `review/round-1/{general,merged}.md`,
  `review/round-2/{general,merged}.md`, `review/disposition.md`.

### Follow-up (not done here; for the cockpit to file)

- The admin feedback list's Owner column (task 327) can show the owner's display name instead of
  the raw principal id by using `IProfileDirectory.FindManyAsync`, gated on `admin.profiles.read`.

### How to validate

**Smoke:**

```bash
dotnet run source/container-apps/web/features/admin/principals/list-principals/list-principals-tests.cs
dotnet run source/container-apps/web/features/admin/principals/get-principal/get-principal-tests.cs
./bin/dev build && ./bin/dev test -q
```

Manually: sign in as Administrator and open `/Admin/Principals`. Type part of a display name or
email into the search box, then click the Display name header twice. Open a row.

**Expect:**

- Both runfiles report all tests passed. `dev build` shows 0 warnings and 0 errors, and
  `dev test` ends with "Tests completed successfully!".
- The grid shows Display name and Email columns, with "—" for agents and principals without a
  profile. Search narrows the rows case-insensitively, and the header click toggles ascending and
  descending order.
- The detail page shows status, roles, the full profile card (display name, email, language,
  region, theme, notifications) and the role editor with Save.
- `admin.profiles.read` is Administrator protected core, so it cannot be removed from
  Administrator. To see the no-PII view, grant Operator `admin.access` and
  `admin.principals.read` in `/Admin/Roles`, then sign in as a principal whose only admin role is
  Operator. The profile columns show "—", the search box and profile sorting disappear, and the
  profile card is hidden. The pages still load.

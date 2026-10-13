# Round 1 — general
**Date:** 2026-10-13
**Scope reviewed:** branch vs master (a506514d9, dfccd1e8a)

## Summary

I found no correctness or security defects. PII gating is evaluated against the calling principal, which comes from `ICurrentPrincipalAccessor` and goes through `IPermissionEvaluator`. That matches the precedent in download-feedback-attachment, and both endpoint schemes expand roles the same way. List rows, search, profile-field sorts and the detail endpoint all fail closed: they return null fields, or 403 before any profile read. The 403 detail text contains no PII.

Other areas checked:
- **Sort:** nulls go last in both directions, and ties keep a stable store order.
- **Store:** the EF and in-memory stores never create a profile.
- **Migration:** it uses the same Administrator GUID as earlier migrations, Up and Down are symmetric, and the Designer is byte-identical to the snapshot's model. The `(!postgres)` template exclusion already covers the migrations folder and the EF store.
- **Slice isolation (TWA0009):** Admin.Principals references only the `Abstractions` port.
- **Required test matrix:** it exists and asserts the right things. That covers happy path, validation, no PII without the permission, Member 403 and anonymous 401.

The findings below are a misleading validation step and test premise caused by the Administrator protected core, plus two polish items.

## Issues

### Issue 1 — Severity: suggestion
- File: kanban/to-do/328-admin-can-browse-all-user-profiles-via-the-principals-admin-area/task.md:198
- Description: `admin.profiles.read` was added to `RolePermissionSeed.AdminPermissions` (`platform/authorization/role-permission-seed-application.cs:47`), which is the Administrator protected core. `AdminLockoutGuards.ProtectedCoreConflict` returns 409 when any AdminPermissions id is stripped from Administrator. So the "How to validate" step "If `admin.profiles.read` is removed from Administrator in `/Admin/Roles` … profile columns show —" cannot be performed through the product: Save is rejected with 409. The HTTP test `Ok_Administrator_Without_Profiles_Read_Sees_No_Profile_Fields` (`tests/container-apps/web/web-server-integration-tests/.../principals-authorization-tests.cs:297`) proves the gating only by writing to `IRolePermissionStore` directly, which bypasses the guard. The same applies to the `ListWorld` and `GetWorld` fixtures. The gating logic is correct; the problem is that the documented and tested scenario is not reachable. The reachable no-PII case is a non-Administrator role that is granted `admin.principals.read` without `admin.profiles.read`.
- Suggestion: Rewrite the validation step to use a non-Administrator role (for example, grant Operator `admin.access` + `admin.principals.read` in `/Admin/Roles` and sign in as an Operator-only principal). Optionally re-base the integration no-PII test on that realistic setup, so it does not depend on a store write that the product forbids. Alternatively, if Administrator should be able to drop PII access, record a decision to leave `admin.profiles.read` out of the protected core. That conflicts with the current "all admin.* are protected core" rule in `permission-ids-contracts.cs`.
- Status: open

### Issue 2 — Severity: nit
- File: source/container-apps/web/features/profile/ef-profile-store-infrastructure.cs:66
- Description: The EF search lower-cases the term with `ToLowerInvariant()` in .NET and the columns with Postgres `lower()`, which depends on the database's LC_CTYPE. The in-memory store uses `OrdinalIgnoreCase`. The Design region in `in-memory-profile-store-application.cs` says the in-memory store "mirrors" the EF store. That holds for ASCII, but non-ASCII letters (for example Turkish dotted/dotless i, or characters whose invariant lowercase differs from the DB collation) can match in one store and not the other.
- Suggestion: Record the ASCII-equivalence caveat in the Design region. Alternatively, use `EF.Functions.ILike` with an escaped pattern, so the comparison happens entirely in Postgres. Low priority, since the admin list is small and the inputs are mostly ASCII emails and names.
- Status: open

### Issue 3 — Severity: nit
- File: source/container-apps/web/projects/web-spa/features/admin/principals/principal-state/principal-state.open-principal.cs:61
- Description: When OpenPrincipal is for a different id, `GetRequest` clears `Current` and `LoadError`. `HandleError` sets `LoadError` only for 404. A non-404 failure (403 after the caller loses `admin.principals.read`, or 500) leaves both null, so the detail page renders an empty Status section with no inline message. The notification toast from `base.HandleError` still fires, so this is cosmetic.
- Suggestion: Set a generic `LoadError` (for example "This principal could not be loaded.") for any non-success when `Current` is null, and keep the 404 message as it is.
- Status: open

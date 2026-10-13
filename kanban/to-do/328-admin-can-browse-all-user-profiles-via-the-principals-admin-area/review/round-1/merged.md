# Round 1 — merged findings
**Date:** 2026-10-13
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 0 | 0 |
| suggestion | 0 | 1 | 0 |
| nit | 0 | 2 | 0 |

## Issues

### M1 — Severity: suggestion — Status: fixed
- File: kanban/to-do/328-…/task.md:198; tests/container-apps/web/web-server-integration-tests/features/admin/principals/principals-authorization-tests.cs:297
- Description: `admin.profiles.read` is Administrator protected core (409 on removal), so the "remove it from Administrator" validation step was unreachable, and the no-PII HTTP test reached that state only by bypassing the guard through the store.
- Suggestion: Validate and test with a non-Administrator role that has `admin.principals.read` but not `admin.profiles.read`.
- Source: general
- Disposition notes: The test was renamed to `Ok_Principals_Reader_Without_Profiles_Read_Sees_No_Profile_Fields`. It now assigns [Member, Operator], grants Operator `admin.access` + `admin.principals.read`, and restores the permissions and roles in `finally`. The Design region, the "How to validate" steps and the Results test bullet in task.md were rewritten to match. `dev build` is 0/0 and `dev test` passes.

### M2 — Severity: nit — Status: fixed
- File: source/container-apps/web/features/profile/ef-profile-store-infrastructure.cs:66
- Description: EF lower()/ToLowerInvariant and in-memory OrdinalIgnoreCase agree only for ASCII; the Design region claimed they mirror each other.
- Suggestion: Record the caveat, or use ILike.
- Source: general
- Disposition notes: The ASCII-equivalence and LC_CTYPE caveat is now recorded in the Design regions of both stores. No behavior change: the admin list is small, and the pg_trgm/ILike upgrade path is already noted.

### M3 — Severity: nit — Status: fixed
- File: source/container-apps/web/projects/web-spa/features/admin/principals/principal-state/principal-state.open-principal.cs:61
- Description: A non-404 failure while opening a different principal left Current and LoadError both null, so the page rendered an empty Status section.
- Suggestion: Set a generic LoadError when Current is null.
- Source: general
- Disposition notes: `HandleError` now sets "This principal could not be loaded." for any non-404 failure when Current is null. The Design region was updated.

## Duplicates / conflicts

- None (single reviewer).

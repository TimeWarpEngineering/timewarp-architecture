# Round 1 — merged findings
**Date:** 2026-09-12
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 0 | 0 |
| suggestion | 0 | 1 | 0 |
| nit | 0 | 0 | 0 |

## Issues

### M1 — Severity: suggestion — Status: fixed
- File: source/container-apps/web/platform/postgres/migrations/PostgresDbContextModelSnapshot.cs:109
- Description: `RolePermissionGrant` now lives in `TimeWarp.Architecture.Authorization` (`role-permission-grant-infrastructure.cs`), and `PostgresDbContext` imports that namespace, but the living model snapshot still declares `modelBuilder.Entity("TimeWarp.Architecture.Features.RolePermissionGrant", …)`. Historical `*.Designer.cs` files keeping the old FQN are expected point-in-time artifacts; the snapshot is not. A future `dotnet ef migrations add` can treat this as remove+add (or a noisy rename) against `identity.role_permissions` even though the table mapping is unchanged.
- Suggestion: Retarget the snapshot entity name to `TimeWarp.Architecture.Authorization.RolePermissionGrant`. Do not rewrite older Designers. A new no-op migration is unnecessary if the snapshot is the only living model file that still names the old FQN.
- Source: general
- Disposition notes: Snapshot entity name retargeted to `TimeWarp.Architecture.Authorization.RolePermissionGrant`. Older `*.Designer.cs` files left unchanged.

## Duplicates / conflicts

- None (single reviewer).

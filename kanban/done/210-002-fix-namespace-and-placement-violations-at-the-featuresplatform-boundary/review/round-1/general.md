# Round 1 — general
**Date:** 2026-09-12
**Scope reviewed:** branch task/210-002-fix-namespace-and-placement-violations-at-the-feat vs origin/master (dcbefd87)

## Summary

Commit `dcbefd87` correctly closes parent 210 round-1 findings M3–M10: authorization runtime and payment port land under `web/platform/{authorization,payment}/` with non-Features namespaces, permission ids stay Features substrate, SPA pages rehome into feature `pages/` folders, and TWA0015/0016 features-only scope plus kebab exceptions are documented. Call sites (global-usings, api/web program registration, template.json postgres excludes, skill/AGENTS) match the moves; leftover `features/payment/` and `web-spa/pages/` are gone. Overall risk is low; the only follow-up is the EF model snapshot still recording the old `RolePermissionGrant` CLR name after the namespace move.

## Issues

### Issue 1 — Severity: suggestion
- File: source/container-apps/web/platform/postgres/migrations/PostgresDbContextModelSnapshot.cs:109
- Description: `RolePermissionGrant` now lives in `TimeWarp.Architecture.Authorization` (`role-permission-grant-infrastructure.cs`), and `PostgresDbContext` imports that namespace, but the living model snapshot still declares `modelBuilder.Entity("TimeWarp.Architecture.Features.RolePermissionGrant", …)`. Historical `*.Designer.cs` files keeping the old FQN are expected point-in-time artifacts; the snapshot is not. A future `dotnet ef migrations add` can treat this as remove+add (or a noisy rename) against `identity.role_permissions` even though the table mapping is unchanged.
- Suggestion: Retarget the snapshot entity name to `TimeWarp.Architecture.Authorization.RolePermissionGrant` (and optionally the latest Designer for consistency), or add an explicit no-op/rename migration so the next schema change does not invent a destructive diff. Do not rewrite older Designers.
- Status: open

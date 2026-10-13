# Round 1 — general
**Date:** 2026-10-13
**Scope reviewed:** branch vs origin/master, non-kanban files (51 files, +229/−236)

## Summary

Package pins are consistent: Mediator x3 at beta.5 and State x3 at beta.12, and the OpenApi hold is recorded. The State beta.11 migration is mechanical and correct except for one point. The ApiHandler that replaced `IStore.GetSemaphore` keeps its semaphore table in a static field on a generic class, so the per-state lock is no longer shared across actions. The ICloneable-returns-`this` DTOs are safe today, but three of them expose mutable `List<T>`.

## Issues

### Issue 1 — Severity: bug
- File: source/container-apps/web/projects/web-spa/features/base/api-handler.cs:28
- Description: `private static readonly ConditionalWeakTable<…> Semaphores` lives on `ApiHandler<TAction, TRequest, TResponse>`. Static fields of a generic type are per closed type, so each action handler (e.g. CredentialsState Fetch vs Revoke vs Rename) gets its own table and its own semaphore for the same state type. Two different API actions on one state no longer serialize. The Design region claims "one SemaphoreSlim per (store, state type)", and before beta.11 `IStore.GetSemaphore` gave exactly that. The nested-dispatch test still passes, but only because the lock is no longer shared at all.
- Suggestion: Move the table to a non-generic static holder and call it from AcquireSemaphoreAsync.
- Status: open

### Issue 2 — Severity: nit
- File: source/container-apps/web/projects/web-spa/features/base/api-handler.cs:157
- Description: SemaphoreGuard's `ObjectDisposedException` comment says RemoveState disposed the semaphore. The handler now owns the semaphores and never disposes them, so the comment is stale.
- Suggestion: Reword the comment as defensive.
- Status: open

### Issue 3 — Severity: nit
- File: source/container-apps/web/features/admin/roles/get-roles/get-roles-contracts.cs (RoleDto.PermissionIds), get-principal-contracts.cs (PrincipalDetailDto.RoleIds), list-principals-contracts.cs (PrincipalSummaryDto.RoleIds)
- Description: These DTOs implement `Clone() => this` with the doc "Immutable (get-only members)", but they expose a mutable `List<T>`. A clone shares the list. A grep found no mutation sites: drafts copy via `[.. RoleIds]` / new sets.
- Suggestion: Switch these to IReadOnlyList, or rely on the upstream generator fix (timewarp-state#646).
- Status: open

# Round 1 — merged findings
**Date:** 2026-10-13
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 1 | 0 |
| suggestion | 0 | 0 | 0 |
| nit | 0 | 1 | 1 |

## Issues

### M1 — Severity: bug — Status: fixed
- File: source/container-apps/web/projects/web-spa/features/base/api-handler.cs:28
- Description: The semaphore table was a static field on a generic class, giving one table per closed handler type, so different actions on the same state did not serialize.
- Suggestion: Use a non-generic holder.
- Source: general
- Disposition notes: Fixed. Added `features/base/api-handler-semaphores.cs` (`ApiHandlerSemaphores.Get(store, stateType)`), and ApiHandler now calls it. `dev build` 0/0, `dev test` all green.

### M2 — Severity: nit — Status: fixed
- File: api-handler.cs SemaphoreGuard.Dispose
- Description: Stale RemoveState-disposal comment.
- Source: general
- Disposition notes: Fixed. The comment now marks the catch as defensive.

### M3 — Severity: nit — Status: wontfix
- File: RoleDto / PrincipalDetailDto / PrincipalSummaryDto contracts
- Description: The `Clone() => this` DTOs expose a mutable List<T>.
- Source: general
- Disposition notes: Wontfix (orchestrator). No code mutates these lists; drafts copy them. Changing the contract collection types touches the serialization contracts, which is out of scope for a package-bump task. The real fix is upstream: timewarp-state#646 (the generated clone calls guarded constructors). The `ICloneable` shims go away when that fix ships.

## Duplicates / conflicts

- None (single reviewer).

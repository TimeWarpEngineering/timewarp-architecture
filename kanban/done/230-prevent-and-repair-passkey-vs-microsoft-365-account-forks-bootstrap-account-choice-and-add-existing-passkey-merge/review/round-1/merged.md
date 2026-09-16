# Round 1 — merged findings
**Date:** 2026-09-16
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 2 | 0 |
| suggestion | 0 | 2 | 0 |
| nit | 0 | 0 | 0 |

## Issues

### M1 — Severity: bug — Status: fixed
- File: source/container-apps/web/features/identity/entra-ticket-processor-application.cs:138
- Description: `ProcessLinkAsync` sends any non-null handle hit (including **revoked**) into `CompleteLinkWhenHandleOwnedAsync`, which merges the owner into the caller when that owner is active. `MergePrincipalAsync` only re-parents **active** credentials, so the revoked EntraAccount stays on the retired source; unique `(Type, Handle)` then permanently blocks attaching that Microsoft 365 identity to the survivor (and `Restore` is not on this path). Sync-hit/bootstrap correctly special-case revoked; link-merge must not treat a revoked foreign row as merge proof.
- Suggestion: Before merge, refuse revoked rows (e.g. `if (existing.IsRevoked) return IdentityProblems.EntraCredentialRevoked();`, and only call `CompleteLinkWhenHandleOwnedAsync` when `existing is { IsRevoked: false }`). Add a regression test: foreign revoked handle + link → 403, owner not merged.
- Source: general
- Disposition notes: ProcessLinkAsync returns EntraCredentialRevoked for revoked handles; merge only when `existing is { IsRevoked: false }`. Regression `Link_Foreign_Revoked_Handle_Should_Refuse_Without_Merge`.

### M2 — Severity: bug — Status: fixed
- File: source/container-apps/web/projects/web-spa/features/identity/pages/microsoft-365-choose-page/ChooseMicrosoft365Page.razor:76
- Description: **Create a new account** completes `CompleteEntraBootstrapCreate` (server sets the identity-session cookie) then `NavigateTo` without notifying `IdentitySessionAuthenticationStateProvider`. The already-have path goes through `PasskeyCeremonyClient.CompleteEntraChoiceExistingAsync`, which calls `NotifySessionChanged()`. Create does not, so CascadingAuthenticationState / AuthorizeView can remain anonymous after a successful mint until a full reload — broken post-choose UX on the primary create button.
- Suggestion: After a successful create, cast/inject `AuthenticationStateProvider` and call `NotifySessionChanged()` (same helper as `PasskeyCeremonyClient`), or `NavigateTo(..., forceLoad: true)`.
- Source: general
- Disposition notes: Create path notifies via inherited `AuthenticationStateProvider` as `IdentitySessionAuthenticationStateProvider` before NavigateTo.

### M3 — Severity: suggestion — Status: fixed
- File: source/libraries/timewarp-identity/persistence/in-memory-principal-store.cs:238
- Description: `ReparentTo` made `Credential.PrincipalId` mutable, but `UpdateCredentialAsync` (in-memory and EF) still only guards Type/Handle. A caller that `ReparentTo`s then `UpdateCredentialAsync`s can move a credential outside `MergePrincipalAsync` (no source retirement, no trust/display rules, no single transaction with principal merge).
- Suggestion: Reject PrincipalId changes in both `UpdateCredentialAsync` implementations (compare stored vs incoming PrincipalId; throw like Type/Handle immutability). Keep re-parent exclusively on the merge path.
- Source: general
- Disposition notes: In-memory and EF UpdateCredentialAsync reject PrincipalId changes. Contract test `Update_rejects_PrincipalId_reparent`. MergePrincipalAsync still uses its own snapshot/replace path.

### M4 — Severity: suggestion — Status: fixed
- File: source/container-apps/web/projects/web-server/program.cs:400
- Description: New `OnValidatePrincipal` correctly rejects inactive/merged principals (stale source session after merge), but only `RejectPrincipal()`s — it does not sign out / delete the identity-session cookie. The cookie remains and every subsequent request re-loads the principal from the store.
- Suggestion: After `RejectPrincipal()`, also `await context.HttpContext.SignOutAsync(IdentitySessionDefaults.Scheme)` (or equivalent cookie delete) so the stale session is cleared once.
- Source: general
- Disposition notes: Both reject paths SignOutAsync(IdentitySessionDefaults.Scheme).

## Duplicates / conflicts

- None (single general reviewer; four distinct findings).

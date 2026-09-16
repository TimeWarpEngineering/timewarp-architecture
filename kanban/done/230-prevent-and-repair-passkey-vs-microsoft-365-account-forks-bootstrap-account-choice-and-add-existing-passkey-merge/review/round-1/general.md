# Round 1 — general
**Date:** 2026-09-16
**Scope reviewed:** branch task/230-prevent-and-repair-passkey-vs-microsoft-365-accoun vs origin/master merge-base ee5d951f (commit 2229e3b8)

## Summary

Unknown-handle Entra bootstrap now parks validated claims (10 min, single-use, opaque id in Secure HttpOnly `.Tw.EntraChoice`) and redirects to `/Login/Microsoft365/Choose`; create mints like the old bootstrap, already-have asserts a passkey then attaches Entra. Repair adds Merge-scoped WebAuthn, `MergePrincipalAsync` (in-memory + EF), link-mode merge for foreign active handles, and Settings CTA. Overall the design matches the brief and tests cover the main paths; residual risk is concentrated in link-mode edge cases around revoked Entra rows and SPA session refresh after choose-create.

## Issues

### Issue 1 — Severity: bug
- File: source/container-apps/web/features/identity/entra-ticket-processor-application.cs:138
- Description: `ProcessLinkAsync` sends any non-null handle hit (including **revoked**) into `CompleteLinkWhenHandleOwnedAsync`, which merges the owner into the caller when that owner is active. `MergePrincipalAsync` only re-parents **active** credentials, so the revoked EntraAccount stays on the retired source; unique `(Type, Handle)` then permanently blocks attaching that Microsoft 365 identity to the survivor (and `Restore` is not on this path). Sync-hit/bootstrap correctly special-case revoked; link-merge must not treat a revoked foreign row as merge proof.
- Suggestion: Before merge, refuse revoked rows (e.g. `if (existing.IsRevoked) return IdentityProblems.EntraCredentialRevoked();`, and only call `CompleteLinkWhenHandleOwnedAsync` when `existing is { IsRevoked: false }`). Add a regression test: foreign revoked handle + link → 403, owner not merged.
- Status: open

### Issue 2 — Severity: bug
- File: source/container-apps/web/projects/web-spa/features/identity/pages/microsoft-365-choose-page/ChooseMicrosoft365Page.razor:76
- Description: **Create a new account** completes `CompleteEntraBootstrapCreate` (server sets the identity-session cookie) then `NavigateTo` without notifying `IdentitySessionAuthenticationStateProvider`. The already-have path goes through `PasskeyCeremonyClient.CompleteEntraChoiceExistingAsync`, which calls `NotifySessionChanged()`. Create does not, so CascadingAuthenticationState / AuthorizeView can remain anonymous after a successful mint until a full reload — broken post-choose UX on the primary create button.
- Suggestion: After a successful create, cast/inject `AuthenticationStateProvider` and call `NotifySessionChanged()` (same helper as `PasskeyCeremonyClient`), or `NavigateTo(..., forceLoad: true)`.
- Status: open

### Issue 3 — Severity: suggestion
- File: source/libraries/timewarp-identity/persistence/in-memory-principal-store.cs:238
- Description: `ReparentTo` made `Credential.PrincipalId` mutable, but `UpdateCredentialAsync` (in-memory and EF) still only guards Type/Handle. A caller that `ReparentTo`s then `UpdateCredentialAsync`s can move a credential outside `MergePrincipalAsync` (no source retirement, no trust/display rules, no single transaction with principal merge).
- Suggestion: Reject PrincipalId changes in both `UpdateCredentialAsync` implementations (compare stored vs incoming PrincipalId; throw like Type/Handle immutability). Keep re-parent exclusively on the merge path.
- Status: open

### Issue 4 — Severity: suggestion
- File: source/container-apps/web/projects/web-server/program.cs:400
- Description: New `OnValidatePrincipal` correctly rejects inactive/merged principals (stale source session after merge), but only `RejectPrincipal()`s — it does not sign out / delete the identity-session cookie. The cookie remains and every subsequent request re-loads the principal from the store.
- Suggestion: After `RejectPrincipal()`, also `await context.HttpContext.SignOutAsync(IdentitySessionDefaults.Scheme)` (or equivalent cookie delete) so the stale session is cleared once.
- Status: open

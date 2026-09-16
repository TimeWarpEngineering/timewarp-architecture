# Round 2 — general
**Date:** 2026-09-16
**Scope reviewed:** post-fix uncommitted delta + M1–M4 re-verification (task/230 vs origin/master ee5d951f)

## Summary

All four prior findings M1–M4 are confirmed fixed in the working tree: link refuses revoked Entra handles without merge, choose-create notifies the identity-session auth state provider, both stores reject PrincipalId re-parent on Update, and OnValidatePrincipal signs out after RejectPrincipal on both paths. One new issue: the claimed `Update_rejects_PrincipalId_reparent` contract method was added to the shared suite but was not wired into the InMemory or EF Fixie runners, so it never executes.

## Prior findings

### M1 — Severity: bug — Status: fixed
- File: source/container-apps/web/features/identity/entra-ticket-processor-application.cs:139
- Verification: `ProcessLinkAsync` now returns `IdentityProblems.EntraCredentialRevoked()` when `existing is { IsRevoked: true }` and only calls `CompleteLinkWhenHandleOwnedAsync` when `existing is { IsRevoked: false }`. Regression `Link_Foreign_Revoked_Handle_Should_Refuse_Without_Merge` asserts 403 title "Entra credential revoked" and that the foreign owner remains active/unmerged; registered via `RegisterTests<Bootstrap_Given_>`.
- Status: fixed

### M2 — Severity: bug — Status: fixed
- File: source/container-apps/web/projects/web-spa/features/identity/pages/microsoft-365-choose-page/ChooseMicrosoft365Page.razor:76
- Verification: After successful `CompleteEntraBootstrapCreate`, create path casts inherited `AuthenticationStateProvider` (`BaseComponent` inject in `base-component.auth.cs`) to `IdentitySessionAuthenticationStateProvider` and calls `NotifySessionChanged()` before `NavigateTo`, matching `PasskeyCeremonyClient`.
- Status: fixed

### M3 — Severity: suggestion — Status: fixed
- File: source/libraries/timewarp-identity/persistence/in-memory-principal-store.cs:237; source/container-apps/web/features/identity/ef-principal-store-infrastructure.cs:266
- Verification: Both `UpdateCredentialAsync` implementations throw when `existing.PrincipalId != credential.PrincipalId`. `MergePrincipalAsync` still re-parents via `ReparentTo` then direct snapshot/replace (in-memory dictionary write; EF credentialReplacements), not via Update. Shared suite method `Update_rejects_PrincipalId_reparent` exists (see Issue 1 for harness gap).
- Status: fixed

### M4 — Severity: suggestion — Status: fixed
- File: source/container-apps/web/projects/web-server/program.cs:392
- Verification: Both `OnValidatePrincipal` reject paths (`!Guid.TryParse` / empty guid, and `principal?.IsActive != true`) call `context.RejectPrincipal()` then `await context.HttpContext.SignOutAsync(IdentitySessionDefaults.Scheme)`.
- Status: fixed

## Issues

### Issue 1 — Severity: bug
- File: tests/libraries/timewarp-identity-tests/in-memory-principal-store-contract-tests.cs:82; tests/container-apps/web/web-infrastructure-tests/ef-principal-store-contract-tests.cs:146
- Description: Shared contract method `PrincipalStoreContract_.Credentials.Update_rejects_PrincipalId_reparent` was added, but Fixie discovery only runs public static wrappers on the concrete runners. Neither the InMemory `Credentials` class nor the EF `Credentials` class exposes a static wrapper for that method (siblings like `Update_missing_credential_fails` / `Lists_in_ascending_CreatedAt_order` are wrapped), so the new regression never executes for either store.
- Suggestion: Add the same static forwarding wrappers used by sibling Credentials tests in both runners (and keep them next to `Update_missing_credential_fails`).
- Status: open

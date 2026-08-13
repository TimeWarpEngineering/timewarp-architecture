# Round 1 — general
**Date:** 2026-08-12
**Scope reviewed:** task 180 first-admin Create-only

## Summary

Create-only first-admin matches the product rule. `CompletePasskeyRegistration` claims only after `AddCredentialAsync` succeeds; `CompletePasskeyAuthentication` and agent registration do not take `IPrincipalRoleStore`. The resolver’s empty-store `{Member}` default never writes, and bootstrap stays an effective-role union. In-memory `TryClaim` is lock-serialized and tested; the durable EF path is Serializable as designed but had no claim tests until M1 fix.

## Issues

### Issue 1 — Severity: suggestion
- File: tests/container-apps/web/web-infrastructure-tests/ef-principal-role-store-tests.cs
- Description: `EfPrincipalRoleStore.TryClaimFirstAdministratorAsync` (Serializable EXISTS + write) was untested; host-free tests only cover `InMemoryPrincipalRoleStore`.
- Suggestion: Add first-wins / second-is-unassigned on ephemeral Postgres (two contexts, one database).
- Status: open

### Issue 2 — Severity: suggestion
- File: source/container-apps/web/features/admin/principals/effective-roles-resolver-tests.cs:126
- Description: No handler-level test that failed registration or sign-in leaves roles unchanged. Structural proof: claim is only after successful AddCredential; sign-in handler has no role store.
- Suggestion: Optional in-proc fail-then-inspect / authenticate-stays-Member cases.
- Status: open

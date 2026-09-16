# Round 1 — general
**Date:** 2026-09-16
**Scope reviewed:** branch task/227-remove-trusted-tenants-list-trust-the-configured-t vs origin/master (a818d927 feat + 1c9b22c2 Results)

## Summary

The change removes the TrustedTenants allowlist end-to-end and pins trust to GUID equality of token `tid` against `Authentication:Entra:TenantId` in `SiteSettingsEntraSignInPolicy`, including the new Link mode check. Admin Authentication drops the textarea/add-tenant/tenant-drift UI for a read-only registration-tenant line and “Microsoft 365 sign-in policy” heading; Settings Microsoft 365 is gated on server `GetEntraSignInOffered` with SPA Entra keys removed. Residual risk is low: migration/snapshot/validator/DI wiring and bootstrap+link foreign-tid coverage match the brief. Keeping `MockAuthenticationDefaults` Entra helpers is a justified exception (Web.Server scheme registration still uses them); SettingsPage’s silent catch matches Login’s fail-closed offered gate.

## Issues

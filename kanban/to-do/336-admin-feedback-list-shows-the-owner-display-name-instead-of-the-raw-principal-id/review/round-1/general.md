# Round 1 — general
**Date:** 2026-10-13
**Scope reviewed:** branch task/336-admin-feedback-list-shows-the-owner-display-name-i vs master

## Summary
The change is correct and matches the spec. FeedbackOwnerNames checks admin.profiles.read through IPermissionEvaluator under the IdentitySession scheme, the same as ListPrincipals. Without the permission it never calls IProfileDirectory, and the list handler makes one FindManyAsync batch per page over distinct owner ids. Feedback uses only the platform port IProfileDirectory (TWA0009 holds). All admin view endpoints (get, reply, note, status) go through AdminViewAsync, so the name is filled consistently. The SPA policy admin.profiles.read is registered (AddPermissionClaimPolicies, or the server PermissionRequirement under prerender) and is already used by PrincipalDetailPage and PrincipalsPage. Tests cover batching, gating, the mutation paths and JSON round-trip. I found one nit.

## Issues
### Issue 1 — Severity: nit
- File: source/container-apps/web/features/feedback/list-feedback/feedback-list-sort-column-contracts.cs:8
- Description: The edited Design comment line now runs on ("... task 328). PascalCase names on the wire, like FeedbackKind; the query string") and is far past the wrapping used elsewhere in the region.
- Suggestion: Re-wrap the paragraph.
- Status: open

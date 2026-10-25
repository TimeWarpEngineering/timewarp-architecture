# Round 3 — general
**Date:** 2026-10-25
**Scope reviewed:** fix delta `3db714967..7cdc03fe7` (commit `7cdc03fe7`, "fix: fill the domain on
a GUID-only accepted tenant"): `site-settings-state.resolve-entra-tenant.cs`,
`authentication-tenant-lookup-tests.cs`, `web-spa/wwwroot/auth.md`, `timewarp-identity/overview.md`,
and `task.md`.

## Summary

The fix narrows the early return in `ResolveEntraTenantActionSet.HandleSuccess` to a draft row with
the same tenant id **and** a non-null domain. A matching GUID-only row now falls through to the
append + `EntraTenantIds.TryNormalize` path. `TryNormalize` (`entra-tenant-ids.cs:85`) replaces a
null-domain entry with the later non-null domain for the same id, so the row becomes
`{id, domain}` in place and no "more than one domain" error is raised. A second, different domain for
a tenant that already has one still returns early with "already listed under {domain}" and leaves the
list as it was. The Design region, `auth.md`, `overview.md`, and the task restatement now describe
this behavior. The renamed test `Fill_The_Domain_Of_A_Guid_Only_Row` expects the filled row and a
null message.

Verified: `ResolveEntraTenant_Action_Should_` in `web-spa-integration-tests` passed 4/4 (re-run by
this reviewer). The goal restatement now matches task Scope 4 (a lookup adds `{tenant ID, domain}`)
and round-1 M3 (a second, *different* domain is refused). The round-2 misread is corrected.

M6 is fixed. No new issues.

## Issues

<!-- None. -->

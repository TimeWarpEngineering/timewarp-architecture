# Round 2 — general
**Date:** 2026-10-25
**Scope reviewed:** fix delta `e1d6e20b9..81ad53928` (commit `81ad53928`, 8 files). The prior
round's M1–M5 were re-checked against that delta.

## Summary

The fix pass closes all five round-1 findings. `Register` now strips `ResilienceHandler` from the
discovery client. A new test registers `ConfigureHttpClientDefaults(AddStandardResilienceHandler)`
before `Register`, which is the same order as `web-server/program.cs:151` and `:215`, and proves a
503 is `UnexpectedStatus` after one call. Every exception except caller cancellation is now typed.
The `#pragma warning disable CA1031` is narrow and explains itself. It does the same thing as the
`container-apps/Directory.Build.props` NoWarn ("cause is logged and remapped"), and the catch is
the behavior this change exists to add, so it does not hide unfinished work. The legacy converter
is gone with no remaining references. Cache entries set `Size`, and a test uses a `SizeLimit`
cache. `task.md` now states the default rate limit.

The M3 fix added one regression. Before it, looking up the domain of a tenant already listed by
GUID only (a hand-entered GUID, or any row the migration converted) filled in that row's domain
through `TryNormalize`. Now that lookup changes nothing, and a new test pins that.

Gates re-run by the reviewer: `dev build` passed (0 warnings, 0 errors). TimeWarp.Identity tests
passed 316/316: round 1 had 314, this pass removed 1 test and added 3. The reviewer did not run
the web-spa integration suite. The implementer reports `ResolveEntraTenant_Action_Should_` 4/4.

## Prior findings

| ID | Status | Evidence |
|----|--------|----------|
| M1 | fixed | `entra-tenant-resolver.cs:112` `ConfigureAdditionalHttpMessageHandlers` → `RemoveResilienceHandlers`; catch-all at :174 maps non-cancellation exceptions to `TimedOut` (OCE) or `NetworkFailure`; Design region updated; tests `Standard_Resilience_Should_Report_503_After_One_Call`, `Unexpected_Exception_Should_Be_Network_Failure` |
| M2 | fixed | `ConvertLegacyGuidArrayJson` and its test deleted; Design region at `entra-tenant-ids.cs:16` points to the migration and its EF test |
| M3 | fixed | `site-settings-state.resolve-entra-tenant.cs:67` reports "already listed under {domain}"; test `Leave_The_List_When_The_Tenant_Is_Already_Listed` (but see Issue 1) |
| M4 | fixed | `Size = 1` at `entra-tenant-resolver.cs:137`; test `Cache_Should_Accept_A_Size_Limit` |
| M5 | fixed | `task.md` How to validate names the 20/60 s default and the `PermitLimit` 2 override |

## Issues

### Issue 1 — Severity: suggestion
- File: source/container-apps/web/projects/web-spa/features/settings/site-settings-state/site-settings-state.resolve-entra-tenant.cs:67
- Description: The new early return runs for any draft entry with the same tenant id, including
  one with `Domain == null`. Before this pass, `TryNormalize` (`entra-tenant-ids.cs:85`) replaced
  a null-domain row with the looked-up domain. The migration turns every stored tenant into a
  null-domain row. Before, an admin could label those rows by looking up their domain. Now the
  page says "Tenant {guid} is already listed." and keeps the bare GUID. The only way to add the
  domain is to remove the row and look it up again, and nothing on the page says so. The new test
  `Leave_A_Guid_Only_Row_When_Lookup_Finds_That_Tenant` pins this behavior. Round-1 M3 was only
  about a second, different domain for a tenant that already has one. This change goes further
  than that.
- Suggestion: return early only when the existing entry has a non-null domain. When the existing
  entry has no domain, let the normal `TryNormalize` path replace it with `{id, looked-up domain}`.
  Change the GUID-only test to expect `[new EntraAcceptedTenant(TenantId, "contoso.com")]`, and
  update the handler's Design region to match.
- Status: open

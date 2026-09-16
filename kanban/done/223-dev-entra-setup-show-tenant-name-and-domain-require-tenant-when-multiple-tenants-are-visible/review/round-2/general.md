# Round 2 — general
**Date:** 2026-09-16
**Scope reviewed:** post-fix delta for M1/M2 (commit e752204b) plus surrounding call sites

## Summary

Re-verified commit `e752204b` against the current Ambiguous setup branch, `FormatTenantLine`, `SelectTenant` candidates, status’s matching message, and the new Jaribu tests. M1 and M2 both remain fixed: setup now splits omitted vs provided `--tenant` on Ambiguous, and domain-only resolved tenants print `{domain} — {guid}` for copy-paste. The fix delta introduces no new defects.

## Prior findings

### M1 — Severity: bug — Status: fixed
- File: tools/dev-cli/endpoints/entra-setup-command.cs:162–172
- Description: Ambiguous setup now branches on `string.IsNullOrWhiteSpace(Command.Tenant)`. Omitted keeps `--tenant is required when more than one tenant is visible`; provided prints `--tenant '{value}' matched more than one tenant` (aligned with `entra-status-command.cs`). `PrintCandidateTable` still receives `selection.Candidates`, which `SelectTenant` narrows to the matched rows when an option was supplied. `ProvidedDisplayNameMatchesTwo_Should_BeAmbiguous` covers two `Default Directory` orgs among a larger list.
- Status: fixed

### M2 — Severity: suggestion — Status: fixed
- File: tools/dev-cli/services/entra-tenants.cs:39–59
- Description: `FormatTenantLine` now treats `NameResolved` with domain and no display name as `{domain} — {guid}` instead of `{guid} (name unavailable)`. Name+domain, name-only, and unresolved paths are unchanged. `PrintCandidateTable`’s “Full values for --tenant” path uses this helper, so copy-paste keeps the known domain. `DomainOnlyResolved_Should_IncludeDomainAndId` locks the new form.
- Status: fixed

## Issues

<!-- new findings only; none -->

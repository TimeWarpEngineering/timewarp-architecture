# Round 1 — merged findings
**Date:** 2026-09-16
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 1 | 0 |
| suggestion | 0 | 1 | 0 |
| nit | 0 | 0 | 0 |

## Issues

### M1 — Severity: bug — Status: fixed
- File: tools/dev-cli/endpoints/entra-setup-command.cs:160
- Description: `TenantSelectionStatus.Ambiguous` covers both omitted `--tenant` with multiple visible tenants and a provided `--tenant` that matches more than one candidate (for example two orgs named `Default Directory`). Setup always prints `--tenant is required when more than one tenant is visible`, which is the wrong remediation when `--tenant` was already passed. Status already distinguishes `matched more than one tenant`.
- Suggestion: Branch on whether `Command.Tenant` was provided (same pattern as status). Omitted → keep the current required message. Provided → print that the value matched more than one tenant and show the narrowed candidate table.
- Source: general
- Disposition notes: Setup Ambiguous branch now splits omitted vs provided `--tenant`. Tests cover SelectTenant matching two display names.

### M2 — Severity: suggestion — Status: fixed
- File: tools/dev-cli/services/entra-tenants.cs:40
- Description: `FormatTenantLine` treats empty `DisplayName` as `{guid} (name unavailable)` even when `NameResolved` is true and `DefaultDomain` is set. Discovery/status can mark a tenant resolved from domain alone, so the printed line and “Full values for --tenant” copy-paste drop the known domain while `MatchesTenant` still accepts it.
- Suggestion: When `DisplayName` is missing but `DefaultDomain` is present, print a domain-bearing line (for example `{domain} — {guid}`) instead of the Graph-failure “name unavailable” form.
- Source: general
- Disposition notes: Domain-only resolved tenants print `{domain} — {guid}`. Added `DomainOnlyResolved_Should_IncludeDomainAndId`.

## Duplicates / conflicts

- None. Single reviewer; two distinct findings.

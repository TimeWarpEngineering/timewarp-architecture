# Round 1 — general
**Date:** 2026-09-16
**Scope reviewed:** branch `task/223-dev-entra-setup-show-tenant-name-and-domain-requir` vs `origin/master` (commits `8900db03` feat + `be85ca5e` Results). Product: Graph organization name/domain on tenant lines, union of `az account list` + `az account tenant list` (subscription-less), `--tenant` on setup/status with refuse-when-ambiguous, `TenantDisplayName`/`TenantDomain` user secrets, domain-suffixed default app name with legacy reuse, sign-in audience summary + public-origin warning, Jaribu helper tests, auth.md.

## Summary

The change makes `dev entra setup` / `status` name the organisation, enumerate subscription-less tenants, refuse ambiguous selection, and carry domain into the default app display name and user secrets. Risk is low: Amuru-only process use, tokens/secrets stay masked, Graph/token failures do not abort setup, and `--dry-run` still runs read-only discovery. Verified claims held (`CaptureAzTokenAsync` never prints token stdout; Bearer dry-run lines mask to `********`; `az ad app list --display-name` uses `startswith` so bare-name list + `TryFindExactAppIds` finds suffixed registrations; wrong-tenant `--tenant` refuses with the login hint; tests stay on pure helpers). One user-facing message bug remains on the ambiguous-match path.

## Issues

### Issue 1 — Severity: bug
- File: tools/dev-cli/endpoints/entra-setup-command.cs:160
- Description: `TenantSelectionStatus.Ambiguous` covers both “`--tenant` omitted and more than one tenant is visible” and “`--tenant` was provided but matched more than one candidate” (exact id/domain/name). Setup always prints `--tenant is required when more than one tenant is visible`, which is wrong remediation when the operator already passed `--tenant`. That second case is realistic: personal tenants are often both named `Default Directory`, so `--tenant "Default Directory"` can match two rows. `entra-status-command.cs` already distinguishes and prints `matched more than one tenant`.
- Suggestion: Branch on whether `Command.Tenant` was provided (same pattern as status): omitted → keep the current “required when more than one is visible” message; provided → print that the value matched more than one tenant and show the narrowed candidate table.
- Status: open

### Issue 2 — Severity: suggestion
- File: tools/dev-cli/services/entra-tenants.cs:40
- Description: `FormatTenantLine` treats empty `DisplayName` as `{guid} (name unavailable)` even when `NameResolved` is true and `DefaultDomain` is set. Discovery/status set `NameResolved` from domain alone (`ResolveTenantAsync` / secrets path), so a domain-only tenant drops the known domain from the printed line and from “Full values for --tenant” copy-paste, while `MatchesTenant` would still accept that domain.
- Suggestion: When `DisplayName` is missing but `DefaultDomain` is present, print a domain-bearing line (for example `({domain}) — {guid}` or `{domain} — {guid}`) instead of the Graph-failure “name unavailable” form; reserve that form for truly unresolved tenants.
- Status: open

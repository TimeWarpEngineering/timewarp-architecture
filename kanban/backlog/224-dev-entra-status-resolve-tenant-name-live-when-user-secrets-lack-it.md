# Dev entra status: resolve tenant name live when user secrets lack it

## Description

Follow-up to 223. `dev entra status` without `--tenant` reports the tenant from user secrets
only (`Authentication:Entra:TenantDisplayName` / `TenantDomain`). Those keys are written only by
`dev entra setup`, so on any machine where setup ran before 223 (or where secrets were set by
hand or by env) status prints `Tenant: <guid> (name unavailable)` even though `az` is signed in
and Graph resolves the organisation fine. Observed 2026-09-16 on the tester's machine right after
223 merged: status said "name unavailable" for the CrunchIt, LLC tenant; rerunning setup fixed it.

Status must be useful without a prior setup rerun.

## Requirements

- In `tools/dev-cli/endpoints/entra-status-command.cs`, when `--tenant` is omitted and the
  secrets tenant has no display name or domain, and `az` is on PATH with a signed-in account,
  resolve the secrets `TenantId` through `EntraTenantDiscovery` (signed-in tenant → Graph
  `/organization`; other tenant → `get-access-token --tenant` path) and print the resolved name.
  Keep the existing behaviour when `az` is unavailable or resolution fails (print the GUID with
  "name unavailable" plus a one-line hint: `run dev entra setup --tenant <domain> to persist`).
- Do not write user secrets from `status` (read-only command).
- Print where the name came from when it was resolved live rather than read from secrets, e.g.
  `Tenant: CrunchIt, LLC (crunchitfs.com) — <guid>  (resolved via Graph; not yet in user secrets)`.
- Unit test the decision helper (pure function: secrets name present → use it; absent + az →
  resolve; absent + no az → unavailable with hint) in `tests/tools/dev-cli-tests/`.
- Update the Design region comment in the status command (it currently documents the
  secrets-only behaviour) and the one-liner in `auth.md` if it mentions status.

## Checklist

- [ ] Live resolution fallback in status; read-only; hint when unresolved
- [ ] Source annotation when resolved live
- [ ] Jaribu test for the decision helper; `dev build` 0/0; `ganda repo audit` clean
- [ ] Design region + auth.md updated
- [ ] Results and How to validate (transcript with secrets name keys unset)

## Session

- Created: cockpit (2026-09-16)
- Claude Code cockpit session: https://claude.ai/code/session_01KPZXyAmA6Vk99W1yUQUn1N

## Notes

- Files: `tools/dev-cli/endpoints/entra-status-command.cs` (~L163-181 secrets-only branch),
  `tools/dev-cli/services/entra-tenant-discovery.cs` (`ResolveTenantAsync`, `EnumerateAsync`),
  `tools/dev-cli/services/entra-tenants.cs` (`FormatTenantLine`).
- Verified 2026-09-16: `az rest --url https://graph.microsoft.com/v1.0/organization?$select=...`
  via Amuru `Shell.Builder("az")` returns `CrunchIt, LLC` / `crunchitfs.com`; the parser handles
  that payload; the gap is purely that status never calls it without `--tenant`.

## Results

_Pending._

### How to validate

_Pending._

## Disposition

- 2026-09-16: shelved to backlog by decision (Steve). `dev entra setup` now persists the tenant name, so the secrets-only status path only misses on hand-set or env-set secrets. Not worth an extra Graph call per status run. Revisit only if that case shows up.

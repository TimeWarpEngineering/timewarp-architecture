# Round 2 — merged findings
**Date:** 2026-10-21
**Sources:** general (review oracle)

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 0 | 0 |
| suggestion | 0 | 5 | 0 |
| nit | 0 | 3 | 0 |

## Resolved prior (round 1)

### M1 — Severity: suggestion — Status: fixed
- Disposition notes: The operator guide and `aca-neon-compute.cs` now say publish emits no HTTP probe. The guide warns that `/api/health` opens Postgres and wakes Neon. `program.cs` and the How to validate steps agree.

### M2 — Severity: suggestion — Status: fixed
- Disposition notes: The culture swap was removed. Decompiling Azure.Provisioning 1.6.0 `BicepSyntax.Value(double)` shows `value.ToString(CultureInfo.InvariantCulture)` inside `json()`, so the new Design text is true. The regex is now `0\.25`.

### M3 — Severity: suggestion — Status: fixed
- Disposition notes: `PostgresRetryPolicy.Configure(builder, count, maxRetryDelay)` and `Resolve` were added. The module and the default overload both call `Configure`. `Resolve_Given_` covers defaults, overrides, one knob, and unparsable or negative input (4 of 4 passed in this round). The comments call the delay the maximum back-off.

### M4 — Severity: suggestion — Status: fixed
- Disposition notes: `SecretParameters(target, scope)` returns only the direct secret for Migrate. `ProbesAzureAccount` is false for aca-neon Migrate and true for Deploy and Open. The preflight takes a dedicated detail branch, and `MigrateNeonAsync` still reads the direct string from `preflight.Secrets`. Tested by `AcaNeonMigrate_Should_CheckOnlyTheDirectSecretAndSkipAzure`.

### M5 — Severity: suggestion — Status: fixed
- Disposition notes: `workflow.yml` uploads `artifacts/aspire-output/aca-neon/**/*.bicep` and `efmigrations/*.sql`. Both comments now name aca-neon.

### M6 — Severity: nit — Status: fixed
- Disposition notes: `Disabled` is refused and `Server` is a host alias. A `postgresql://` URI is refused without echoing the secret or host. Two new tests cover this.

### M7 — Severity: nit — Status: fixed
- Disposition notes: The tests pass real secret values. They assert that argv and the plan omit them, and that only `Parameters__neon-*` environment entries are written. A new overload throws when a secret name appears on the forwarded parameter list. Production call sites use it. `Publish_Should_KeepAcaScaleAndPostgres` now asserts that Flexible Server and Key Vault are present.

### M8 — Severity: nit — Status: fixed
- Disposition notes: SKILL.md, the operator guide, and `program.cs` scope the `unused` placeholder to every publish target and name the AADSTS `invalid_client` consequence.

## Issues

No new findings on the fix delta.

## Duplicates / conflicts

- None.

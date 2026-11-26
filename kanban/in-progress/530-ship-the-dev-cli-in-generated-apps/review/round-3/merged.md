# Round 3 — merged findings
**Date:** 2026-11-26
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 1 | 0 |
| suggestion | 0 | 1 | 0 |
| nit | 0 | 0 | 0 |

## Issues

### M1 — Severity: bug — Status: fixed
- File: tools/dev-cli/services/app-features.cs:44
- Description: Round 1: `AppFeatures.Postgres` used the migrations directory as its marker, and `--web false` removes that tree, so `dev deploy migrate` named `--postgres false`.
- Suggestion: Name the flag that is actually off, and fail the marker test when another flag's exclude covers a marker the feature does not require.
- Source: general
- Disposition notes: fixed, re-verified on round 3. Product is unchanged since `f88c6cd88`. `Postgres` requires `Web`. `FirstMissing` walks required features first. `EachMarker_Should_BeRemovedOnlyByItsOwnOrARequiredFlag` allows an exclude only when every negated flag is the feature's own or a required one. `MissingRequiredFeature_Should_BeNamedFirst` pins the order. readme names web for `deploy migrate`.

### M2 — Severity: suggestion — Status: fixed
- File: tools/dev-cli/endpoints/deploy-command.cs:50
- Description: Round 1: `dev deploy` returned when yarp was absent and still ran when web was absent. Smoke always requests web-server paths.
- Suggestion: Refuse `dev deploy` when web is off, before preflight.
- Source: general
- Disposition notes: fixed, re-verified on round 3. `Handle` checks yarp, then web, and returns before `DeployRunner.RunAsync`.

## Duplicates / conflicts

- None. One reviewer. No new findings. M1 and M2 carried from round 1.

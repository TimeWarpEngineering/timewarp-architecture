# Round 2 — merged findings
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
- Disposition notes: fixed, re-verified on `f88c6cd88`. `Postgres` requires `Web`. `FirstMissing` walks required features first. `deploy-migrate-command.cs` stays compiled because `dev deploy` calls its handler. `EachMarker_Should_BeRemovedOnlyByItsOwnOrARequiredFlag` and `MissingRequiredFeature_Should_BeNamedFirst` pin the rule. readme names web for `deploy migrate`. A replay of that rule against `template.json` shows the postgres marker covered only by `(!web)` and `(!postgres)`.

### M2 — Severity: suggestion — Status: fixed
- File: tools/dev-cli/endpoints/deploy-command.cs:50
- Description: Round 1: `dev deploy` returned when yarp was absent and still ran when web was absent. Smoke always requests `/web-server/alive` and `/api/identity/sign-in-methods`.
- Suggestion: Refuse `dev deploy` when web is off, before preflight.
- Source: general
- Disposition notes: fixed, re-verified on `f88c6cd88`. `Handle` checks yarp, then web, and returns before `DeployRunner.RunAsync`. Smoke paths are unchanged and still require web-server.

## Duplicates / conflicts

- None. One reviewer. No new findings. M1 and M2 carried from round 1.

# Round 1 — merged findings
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
- File: tools/dev-cli/services/app-features.cs:37
- Description: `AppFeatures.Postgres` marks the feature with `source/container-apps/web/platform/postgres/migrations`. `template.json` removes that tree for `(!postgres)` and also removes all of `source/container-apps/web/**` for `(!web)`. Postgres stays an independent parameter and defaults to true. On `--web false` the migrations directory is gone, `dev db` is excluded by `(!postgres || !web)`, and `dev deploy migrate` still ships. `MissingMessage` then says the app was generated with `--postgres false`. `EachMarker_Should_BeRemovedByItsFlagOff` only looks for a modifier whose condition is exactly `(!postgres)`, so the web overlap stays green. `readme.md` repeats the same story (`db` with postgres, `deploy migrate` without postgres) and does not mention web. The recorded manual pass refused migrate for no-postgres, not for no-web.
- Suggestion: Make the refusal name the flag that is actually off. Exclude `deploy-migrate-command.cs` under the same `(!postgres || !web)` modifier as `dev db`, or check web when the web tree is gone and postgres only when that tree is present and the migrations directory is not. Extend the marker test so a marker covered by another flag's exclude fails. Align `readme.md` (the "with postgres" / "without postgres" sentence) with that rule.
- Source: general
- Disposition notes: fixed. `AppFeature.Requires`; postgres requires web and `Require` checks it first, so `dev deploy migrate` without web names web. `deploy-migrate-command.cs` stays compiled because `dev deploy` calls its handler. New test `EachMarker_Should_BeRemovedOnlyByItsOwnOrARequiredFlag` fails when any other flag's exclude covers a marker; `MissingRequiredFeature_Should_BeNamedFirst`. readme aligned.

### M2 — Severity: suggestion — Status: fixed
- File: tools/dev-cli/endpoints/deploy-command.cs:48
- Description: `dev deploy` now returns immediately when yarp is absent, and still runs when web is absent. Its smoke step always requests `/web-server/alive` and `/api/identity/sign-in-methods` (`tools/dev-cli/services/deploy-pipeline.cs`). `--web false` removes `source/container-apps/web/**`, so those paths have no web-server, while the yarp project remains. The command is not excluded and does not refuse up front. The matrix builds this variant and runs generated `dev build`; it does not run `dev deploy`.
- Suggestion: Refuse `dev deploy` when web is off, with the same kind of feature message as entra, or skip smoke paths for container apps the template did not generate.
- Source: general
- Disposition notes: fixed. `dev deploy` refuses without web (after the yarp check), before any preflight.

## Duplicates / conflicts

- None. One reviewer.

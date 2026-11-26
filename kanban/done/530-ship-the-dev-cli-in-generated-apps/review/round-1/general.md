# Round 1 — general
**Date:** 2026-11-26
**Scope reviewed:** branch `task/530-ship-the-dev-cli-in-generated-apps` vs `master`

## Summary

The pack allow-list now ships `tools/dev-cli/`, `tools/Directory.Build.props`, `.timewarp/dev.jsonc`, and `.envrc`. Template-only endpoints are `template.json` excludes or `templateRepository` regions, and the recorded template-smoke run checks that those files and commands are absent and that `dev build` passes on every matrix variant. The flag checks are right for web and yarp. The postgres check is not: its marker sits inside the web tree, so `--web false` (postgres left on) makes `dev deploy migrate` say the app was generated with `--postgres false`.

No misread between the Goal restatement and the original task. The restatement's "excluded without postgres" is narrower than Results and the code, which also drop `dev db` when web is off. That gap is issue 1, not a misread of the task.

The recorded gates were not re-run. They cover shipping, the command list, and generated `dev build`. They do not cover the migrate refusal on `--web false`.

## Issues

### Issue 1 — Severity: bug
- File: tools/dev-cli/services/app-features.cs:37
- Description: `AppFeatures.Postgres` marks the feature with `source/container-apps/web/platform/postgres/migrations`. `template.json` removes that tree for `(!postgres)` and also removes all of `source/container-apps/web/**` for `(!web)`. Postgres stays an independent parameter and defaults to true. On `--web false` the migrations directory is gone, `dev db` is excluded by `(!postgres || !web)`, and `dev deploy migrate` still ships. `MissingMessage` then says the app was generated with `--postgres false`. `EachMarker_Should_BeRemovedByItsFlagOff` only looks for a modifier whose condition is exactly `(!postgres)`, so the web overlap stays green. `readme.md` repeats the same story (`db` with postgres, `deploy migrate` without postgres) and does not mention web. The recorded manual pass refused migrate for no-postgres, not for no-web. No targeted command was run; the two excludes are in `.template.config/template.json` (web at the `(!web)` modifier, db files at `(!postgres || !web)`).
- Suggestion: Make the refusal name the flag that is actually off. Exclude `deploy-migrate-command.cs` under the same `(!postgres || !web)` modifier as `dev db`, or check web when the web tree is gone and postgres only when that tree is present and the migrations directory is not. Extend the marker test so a marker covered by another flag's exclude fails. Align `readme.md` (the "with postgres" / "without postgres" sentence) with that rule.
- Status: open

### Issue 2 — Severity: suggestion
- File: tools/dev-cli/endpoints/deploy-command.cs:48
- Description: `dev deploy` now returns immediately when yarp is absent, and still runs when web is absent. Its smoke step always requests `/web-server/alive` and `/api/identity/sign-in-methods` (`tools/dev-cli/services/deploy-pipeline.cs`). `--web false` removes `source/container-apps/web/**`, so those paths have no web-server, while the yarp project remains. The command is not excluded and does not refuse up front. The matrix builds this variant and runs generated `dev build`; it does not run `dev deploy`.
- Suggestion: Refuse `dev deploy` when web is off, with the same kind of feature message as entra, or skip smoke paths for container apps the template did not generate.
- Status: open

# Round 1 — general
**Date:** 2026-10-15
**Scope reviewed:** branch task/366 vs master: workflow.yml, workflow-command.cs, test-command.cs, test-groups.cs, test-groups-tests.cs, dev-cli-tests.csproj, releasing.md

## Summary

The change splits PR/merge CI into a four-way matrix (`ci (fast|playwright|integration|publish)`). Each leg runs one `dev workflow --phase/--group` pass. A `ci` roll-up keeps the old check name. Release and dispatch now run in one `release` job. The groups are defined once by path convention in `TestGroups`. `dev test --group` refuses to run on a misfiled project, and a monorepo guard test backs that up. Risk is low.

Checks made:
- `workflow_dispatch` maps to `CiMode.Release` in `DetermineMode`, so moving dispatch into the `release` job keeps its earlier behaviour: pack-only, no tests, no publishes. Dropping the Aspire CLI and dev-cert steps from that path removes nothing it used.
- The OIDC login, the probe and the `Packages-*` upload are unchanged and stay single-job. `id-token: write` is now only on `release`.
- In the roll-up, `skipped` (no ci paths changed) passes and `failed`/`cancelled` fails. This matches the old single-job skip semantics.
- The guard test uses the same tests/ glob and bin/obj filter as `dev test`. I re-ran it: dev-cli-tests passed 235 of 235, including the GroupsOf guard tests.
- `ganda repo audit` passes.

Observation, not a defect: an empty group passes with 0 projects. In the monorepo the guard test `EveryGroup_Should_HaveAProjectInTheMonorepo` prevents that. In a generated app with flags off, an empty group is a legitimate outcome.

## Issues

<!-- none -->

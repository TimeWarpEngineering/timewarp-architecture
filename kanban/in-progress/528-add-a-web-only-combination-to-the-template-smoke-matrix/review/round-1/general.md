# Round 1 — general
**Date:** 2026-11-26
**Scope reviewed:** branch `task/528-add-a-web-only-combination-to-the-template-smoke` vs `master` (template-smoke matrix, runner, tier-1 artifact table, command option, co-located Jaribu reference). Compared with `## Goal (implementer restatement)` and the original task.

## Summary

`SmokeWebOnly` is the matrix entry the task asks for: `--api false --grpc false --yarp false`, web and postgres left at their template defaults (on), and `ExcludedFamilies` api, grpc, and yarp. `FamilyTestArtifacts` plus `AssertFamilyTestArtifacts` make an orphaned api, grpc, or yarp test project or listed file fail tier 1. Each table row matches a `.template.config/template.json` exclude: api integration tests and `api-test-server-application.cs` under `!api`; the superhero test folder under `!web` and `!grpc`; the yarp integration project under `!web` and `!yarp`; the grpc route test under all three; `yarp-test-server-application.cs` under `!yarp`. `SmokeNoGrpc` and `SmokeNoYarp` now list `grpc` and `yarp`, so the presence half of that check holds when only that flag is off. `--variant` selects one matrix name (ordinal ignore case) and an unknown name exits 1 before packing; with no `--variant`, `--matrix` still runs the whole list and the default path still runs `SmokeDefault`. The nightly job in `.github/workflows/workflow.yml` calls `template-smoke --matrix --app-tests` with no name filter. `dev template-publish-smoke` has its own matrix (default and postgres off) and does not publish `aca` or `aca-neon`; those suites are `aca-publish-tests.cs`, `aca-publish-secrets-tests.cs`, and `aca-neon-publish-tests.cs` inside generated `aspire-tests`, which `--app-tests` runs. Recorded results cover `./bin/dev build`, `ganda repo audit`, `./bin/dev lint-skills`, `./bin/dev template-smoke --variant SmokeWebOnly --app-tests` (including aspire-tests 70/70), the artifact table against `dotnet new` for the default and each single flag plus the smoke output, and `--variant Nope`. The goal restatement matches the original task. No test file was edited. Gates were not re-run.

## Issues

<!-- None. -->

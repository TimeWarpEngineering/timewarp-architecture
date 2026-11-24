# Round 1 — general
**Date:** 2026-11-24
**Scope reviewed:** `tools/dev-cli/endpoints/build-command.cs`, `tools/dev-cli/services/build-output.cs`, `tests/tools/dev-cli-tests/build-output-tests.cs`, `tests/tools/dev-cli-tests/dev-cli-tests.csproj`, and the `dev workflow` call sites in `tools/dev-cli/endpoints/workflow-command.cs`. Compared with `## Goal (implementer restatement)` and the original requirements. Gates taken from `## Results` / `### How to validate` / `### Gates`. No build, test, audit, or hook was run.

## Summary

`dev build` now captures MSBuild output unless `--verbose` is set. A green capture prints the existing short preamble (repository root, client-name scan, the build step line) and one summary line with elapsed seconds and the repository root. A failed capture writes `CommandOutput.Combined` (stdout and stderr in production order) and `Build failed!` to stderr and sets exit code 1. `--quiet` stays on the command and selects the same capture mode. `--verbose` streams through the previous `RunAsync` path and wins when both flags are passed. `RunPrAsync` and `RunReleaseAsync` still call `new BuildCommand()` with defaults, and `RunStepAsync` stops the pipeline when `Environment.ExitCode` is non-zero, so a red CI build still prints every captured diagnostic.

The pure rules live in `BuildOutput` and are the five tests in `BuildOutput_`. The recorded gates cover this diff: self-install, `./bin/dev build` (default, `-v`, `-q`) with line counts and exit codes, `./bin/dev test --project dev-cli-tests --filter-class BuildOutput_` (5 passed), and `ganda repo audit` (exit 0). `TreatWarningsAsErrors` is true, so a green build has nothing to hide. `CommandOutput.Success` is exit code 0. The class filter matches the `BuildOutput_` namespace, which is where those five tests live.

## Issues

None.

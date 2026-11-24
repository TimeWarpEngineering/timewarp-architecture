# Round 1 — general
**Date:** 2026-11-24
**Scope reviewed:** branch `task/516-add-project-and-class-method-filter-options-to-dev` vs merge-base `b9110665f`. Product files for `dev test --project`, `--filter-class`, and `--filter-method`, plus the recorded `## Results` and `### How to validate`.

## Summary

`dev test` now takes repeatable, comma-separated `--project` / `-p`, and optional `--filter-class` and `--filter-method`. `TestProjectSelection` matches an exact project stem first, otherwise one unique substring of the stem or the repo-relative path, and fails with the available names or the candidates. `DotNetTestArguments.BuildPlan` keeps the old solution-then-out-of-solution build when no project name is given, and for a named subset builds those projects one at a time in Release instead of the solution. Every `dotnet test` stays `--no-build --no-restore` with the filters appended after the coverage flags. The unit tests pin exact, substring, path, multi-name, unknown, and ambiguous selection, plus filter forwarding and the build plan. `AGENTS.md` has the one-line example. `skills/tw-feature-placement/references/co-located-jaribu-runfiles.md` does not describe a targeted test run, so leaving it unchanged matches the task. The implementer restatement matches the original requirements; there is no misread. Recorded gates (self-install, `./bin/dev build`, `./bin/dev test --project dev-cli-tests` at 385 passed, and `ganda repo audit` after the advisory fix) cover this change. This review did not re-run those gates.

## Issues

<!-- None. -->

# Round 1 — general
**Date:** 2026-11-17
**Scope reviewed:** branch `task/493-adopt-the-shared-pull-request-build-classifier-and` vs merge-base `71e78941`. Product diff in `.github/workflows/workflow.yml`, `.timewarp/dev.jsonc`, `Directory.Packages.props`, and `tools/dev-cli/endpoints/workflow-command.cs`, checked against the task and against `## Goal (implementer restatement)`.

## Summary

The change pins `TimeWarp.Nuru` and `TimeWarp.Nuru.DevCli` at 4.0.0-beta.15 and calls `PullRequestBuildClassifier.DecideAsync` on the default and `classify` phases. On a skip it prints the classifier message and returns without building, which leaves the process exit code at 0. `--phase classify` writes `ci=true|false` to `GITHUB_OUTPUT`. Release mode returns before that call. Push CI still runs `--phase test` and `--phase publish`, which do not classify, and `detect-paths` still uses the existing regex for push. `pull_request` no longer has a paths filter. The push filter is unchanged and the comment states why a kanban-only merge must not rebuild.

The config matches the beta.15 rule. `pullRequestBuild.buildPaths` covers `timewarp-templates/**`, `.template.config/**`, and `goldens/**`, so markdown under those roots builds (the default rule would treat that markdown as non-build). `nonBuildPaths` covers `skills/**`, `evals/**`, and `.vally.yaml`, which the default rule would otherwise build and which the old `ci` regex did not treat as build paths. Protected roots, workflows, `Directory.Build.props`, `Directory.Packages.props`, and `global.json` still build. Nothing in `tools/dev-cli/` copies the rule. The job output expression uses the classifier on a pull request and fails open to `ci=true` when the classify step writes nothing; the `ci` aggregator now fails when `detect-paths` did not succeed, so a failed classify cannot pass as a skipped matrix. `fetch-depth` uses the string `'0'` on pull requests, which is the form that stays truthy in a GitHub Actions `&&` / `||` expression.

The implementer restatement matches the task. `--phase classify` is the adaptation of the Nuru classify stage to this repo's existing `--phase` matrix and `ci` output, not a different goal. Recorded gates cover the change: `./bin/dev build` passed, `./bin/dev test` passed 23 of 23, and `ganda repo audit --fix` passed with no edits. Those gates were not re-run here. The synthetic classify smoke in `### How to validate` matches the classifier messages and the config (non-build paths `ci=false`, template and golden markdown and `global.json` `ci=true`).

The checkout section header still says every job is shallow and that nothing in CI reads history. The `detect-paths` checkout step states the pull-request full history. That header is stale and was not raised: the step a later edit would touch already says why `fetch-depth` is 0.

## Issues

<!-- None. -->

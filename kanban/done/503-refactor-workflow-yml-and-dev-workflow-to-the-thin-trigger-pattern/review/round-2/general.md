# Round 2 — general
**Date:** 2026-11-24
**Scope reviewed:** delta since round 1, commit `9ecb89ee7`. `ClassifyAsync` and `WorkflowPathClasses.Classify`, the four `Classify_Given_` tests, and the fix-pass record in `## Results` / `### How to validate`. Checked against the original task and the updated `## Goal (implementer restatement)`. Gates in that record were read and not re-run.

## Summary

The fix stops taking `ci` from `PullRequestBuildDecision.RequiresBuild`. For a diff that was read, `WorkflowPathClasses.Classify` asks `PullRequestBuildClassifier.RequiresBuild` with `pullRequestBuild` from `.timewarp/dev.jsonc`. A null diff still turns `ci`, `skill`, and `template` on, and an unreadable config forces `ci` on. `IsSkillPath` is unchanged. That is the round 1 M1 contract.

`RequiresBuild` (TimeWarp.Nuru.DevCli 4.0.0-beta.17) skips `skills/**`, `evals/**`, and `.vally.yaml` once those entries are loaded, and it builds `source/`, `tests/`, `tools/`, `msbuild/`, `.github/workflows/`, `.github/actions/**`, and `timewarp-architecture.slnx`. A pull request still uses the classifier's path list, including the empty-list fail-open when the diff was not read. The fix-pass record covers this delta: build 0/0, dev-cli-tests 357/357, a skills-only push smoke (`b867613f6`, `ci=false`), a branch-range smoke (`ci=true`), and `ganda repo audit` exit 0.

No new issues.

## Issues

<!-- None. -->

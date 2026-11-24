# Round 2 — merged findings
**Date:** 2026-11-24
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 1 | 0 |
| suggestion | 0 | 0 | 0 |
| nit | 0 | 0 | 0 |

## Issues

### M1 — Severity: bug — Status: fixed
- File: tools/dev-cli/endpoints/workflow-command.cs:161
- Description: Round 1: `ClassifyAsync` wrote `ci` from `decision.RequiresBuild`, which is true for every push, so a skills-only or evals-only push ran ci-group. Re-checked on `9ecb89ee7`. `ci` now comes from `WorkflowPathClasses.Classify` over the read diff and `PullRequestBuildClassifier.RequiresBuild` with `.timewarp/dev.jsonc` `pullRequestBuild`. A null diff still turns every job on. `IsSkillPath` is unchanged.
- Suggestion: (carried, done) Use the pull request build rule on the push diff.
- Source: general
- Disposition notes: Fixed in `9ecb89ee7`. Round 2 re-read the wiring, the classifier rule in TimeWarp.Nuru.DevCli 4.0.0-beta.17, and the fix-pass record. Still fixed. No new findings.

## Duplicates / conflicts

- One reviewer. Nothing to collapse.

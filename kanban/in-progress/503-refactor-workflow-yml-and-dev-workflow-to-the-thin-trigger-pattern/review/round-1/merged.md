# Round 1 — merged findings
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
- File: tools/dev-cli/endpoints/workflow-command.cs:158
- Description: `ClassifyAsync` writes `ci` from `decision.RequiresBuild`. For a push, `PullRequestBuildClassifier.DecideAsync` returns immediately from `NotPullRequest` (`timewarp.nuru.devcli` 4.0.0-beta.17) with `RequiresBuild` true and an empty path list. The push diff `ReadChangedPathsAsync` already fetched is used only for `skill` and `template`. `on.push.paths` in `.github/workflows/workflow.yml` (lines 31–35) still includes `**/SKILL.md`, `**/eval.yaml`, `.vally.yaml`, `evals/**`, and `skills/**`. Those paths used to set `ci=false` in the deleted detect-paths PowerShell, so `ci-group` was skipped. A skills-only or evals-only push to `master` or `feature/**` now runs `ci (fast)`, `ci (integration)`, and `ci (publish)`. `timewarp-architecture.slnx` and `.github/actions/**` should still build.
- Suggestion: For a push whose diff was read, set `ci` from that diff with the same rule pull requests use (`PullRequestBuildClassifier.RequiresBuild` plus `pullRequestBuild` in `.timewarp/dev.jsonc`), not from the event-level `RequiresBuild` flag. An unreadable diff stays fail-open (`ci`, `skill`, and `template` true). Lock it with a test on the pure decision (known skill-only list → `ci` false; `source/**` or `.github/actions/**` or `timewarp-architecture.slnx` → `ci` true; null diff → `ci` true). Dropping those five globs from `on.push.paths` is an acceptable alternative (a skills-only push then never starts). Do not make the skill-path tests pass by narrowing `IsSkillPath`.
- Source: general
- Disposition notes: Fixed. `ClassifyAsync` takes `ci` from `WorkflowPathClasses.Classify(changed, paths => PullRequestBuildClassifier.RequiresBuild(paths, pullRequestBuild))`, so a push uses its own diff with the pull request rule; null diff and unreadable `dev.jsonc` build. `IsSkillPath` unchanged. Tests: `Classify_Given_` in `tests/tools/dev-cli-tests/workflow-path-classes-tests.cs`. `on.push.paths` kept, so a skills-only push still runs skill-lint.

## Duplicates / conflicts

- One reviewer. Nothing to collapse.

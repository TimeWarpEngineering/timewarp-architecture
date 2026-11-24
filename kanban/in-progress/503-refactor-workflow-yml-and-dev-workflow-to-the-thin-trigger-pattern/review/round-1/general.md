# Round 1 — general
**Date:** 2026-11-24
**Scope reviewed:** branch `task/503-refactor-workflow-yml-and-dev-workflow-to-the-thin` vs merge-base `3cb120c94` (product commit `000a4063f`), checked against the original task and `## Goal (implementer restatement)`. Recorded gates in `## Results` / `### How to validate` were read and not re-run.

## Summary

The workflow is a thin trigger: checkout stays per job, `.github/actions/setup-ci` owns the .NET SDK, NuGet cache, dev-cert trust, and the pinned Aspire CLI, and `dev workflow` owns classify, the fast-group ingress golden, and the release pack path. Concurrency, the removed publish uploads, the `--file` invocations, and the Aspire/skill guards match the task. One regression remains: a push whose diff is only skills or evals now runs the full `ci` matrix, because `ci` is taken from `PullRequestBuildClassifier`, which forces a build for every non-pull-request event, while `on.push.paths` still lists those non-build paths.

Recorded gates cover the change that has tests: `./bin/dev build` 0/0, dev-cli-tests 353/353, `./bin/dev lint-skills`, actionlint 1.7.12, and `ganda repo audit` exit 0 after the shallow-clone repair. `dev template-smoke` and a full `dev workflow --phase test --group fast` are explicitly left to CI. That record is enough for those gates. The push `ci` wiring is not in the new path-class tests.

One targeted check, not filed: `pwsh -NoProfile -File /tmp/pwsh-exit-check.ps1` (suspected that a bare `dotnet run` step would stay green on failure, because local pwsh exits 0 when `$PSNativeCommandUseErrorActionPreference` is false). The built-in GitHub `pwsh` shell appends `exit $LASTEXITCODE`, and each pipeline step's last command is the `dotnet run`, so that suspicion did not hold.

## Issues

### Issue 1 — Severity: bug
- File: tools/dev-cli/endpoints/workflow-command.cs:158
- Description: `ClassifyAsync` writes `ci` from `decision.RequiresBuild`. For a push, `PullRequestBuildClassifier.DecideAsync` returns immediately from `NotPullRequest` (`timewarp.nuru.devcli` 4.0.0-beta.17, `pull-request-build-classifier.cs`) with `RequiresBuild` true and an empty path list. The push diff that `ReadChangedPathsAsync` already fetched is used only for `skill` and `template`. `on.push.paths` in `.github/workflows/workflow.yml` (lines 31–35) still includes `**/SKILL.md`, `**/eval.yaml`, `.vally.yaml`, `evals/**`, and `skills/**`. Those paths used to set `ci=false` in the deleted detect-paths PowerShell (`$ciPattern` did not match them), so `ci-group` was skipped. A skills-only or evals-only push to `master` or `feature/**` now runs `ci (fast)`, `ci (integration)`, and `ci (publish)`. `timewarp-architecture.slnx` and `.github/actions/**` should still build; the classifier's path rule already treats a solution file as a build and `.timewarp/dev.jsonc` lists `.github/actions/**` under `buildPaths`.
- Suggestion: For a push whose diff was read, set `ci` from that diff with the same rule pull requests use (`PullRequestBuildClassifier.RequiresBuild` plus `pullRequestBuild` in `.timewarp/dev.jsonc`), not from the event-level `RequiresBuild` flag. An unreadable diff stays fail-open (`ci`, `skill`, and `template` true). Lock it with a test on the pure decision (known skill-only list → `ci` false; `source/**` or `.github/actions/**` or `timewarp-architecture.slnx` → `ci` true; null diff → `ci` true). Dropping those five globs from `on.push.paths` is an acceptable alternative (a skills-only push then never starts, which is what `timewarp-nuru` does with `paths-ignore: skills/**`). Do not make the skill-path tests pass by narrowing `IsSkillPath`.
- Status: open

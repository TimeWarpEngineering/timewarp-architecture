# Round 1 — general
**Date:** 2026-11-26
**Scope reviewed:** branch `task/531-ship-a-ci-workflow-in-generated-apps` vs `master` (merge-base `ec8488cef`). Product commit `da69eb292` and the task kitchen. Compared with `## Goal (implementer restatement)` and the original task.

## Summary

A generated app now gets `.github/workflows/workflow.yml` and `.github/actions/setup-ci/action.yml`, and nothing else under `.github/`. The workflow source is `.github/app-workflows/workflow.yml`, so it does not run in this repository. The unconditional `template.json` modifier renames it into `.github/workflows/` and excludes this repository's own workflows. The pack lists that source and the existing `setup-ci` tree. `setup-ci` is still the one file both workflows use.

The app workflow is a thin trigger: classify, then `ci (fast)`, `ci (integration)`, and `ci (publish)`, then the required `ci` check, plus a green-key marker job for the ci gate. Publish is `dev workflow --phase publish`, which runs `dev publish all` (production-safety suites and helm lint) and deploys nothing. There is no release, skill-lint, template-smoke, NuGet login, or template-content path. `pull_request` has no paths filter. Runners are `ubuntu-latest` unless `CI_RUNNER_LABELS` is set; the wslc labels are not in the file. The workflow has no flag condition, so every flag-off variant gets the same file. Test groups and publish follow the projects that generation left in the tree.

Green key is carried, and Results say so. `green-key.cs` and `green-key-ledger.cs` now ship. The app's classify writes `ci` from `PullRequestBuildClassifier` and the two green-key outputs. Skill and template lookups stay in a `templateRepository` region. On a push, `DecideAsync` reports a build for every non-pull-request event, which is the right fail-open here: the app's push `paths` list is build paths (skills, docs, and kanban do not start a push run). A pull request still uses the classifier, so a skills-only pull request skips ci-group and the skipped group still passes `ci`. A manual dispatch is not a green-key candidate, so it stays a full run.

`AppCiWorkflowShape` parses the block-YAML subset and checks the app's jobs, phases, the unfiltered `pull_request` trigger, `setup-ci` as the only local action, and the absence of template-only jobs and text. template-smoke, on every variant, asserts `.github/` is exactly those two files, both byte-for-byte with their sources, and that the workflow passes `Check`. readme.md tells a template user that a generated app includes this CI and how to set the required check and the runner variable.

The implementer restatement matches the task. The extra "nothing else under `.github/`" line is the same constraint the smoke enforces. Green key, the readme, and running the phases in a generated app are the checklist, not a different goal. No misread.

The recorded gates cover the change: `./bin/dev build` (0/0), the new shape tests (9/9), green-key and workflow-path-classes tests, the aspire-deploy scan, `template-smoke` and `template-smoke --matrix` (7 variants), `ganda repo audit`, and manual classify / fast / publish in generated apps (SmokeDefault, SmokeWebOnly, SmokeNoWeb). Integration was not run locally; Results say why, and the phase is the existing project-convention group, not a new project list. This review did not re-run those gates.

No issues.

## Issues

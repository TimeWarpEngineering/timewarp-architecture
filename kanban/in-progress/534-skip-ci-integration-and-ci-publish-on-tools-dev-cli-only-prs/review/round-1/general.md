# Round 1 — general
**Date:** 2026-11-27
**Scope reviewed:** branch `task/534-skip-ci-integration-and-ci-publish-on-tools-dev` vs merge-base `af245adde`. Product files listed in `review/review-framework.md`.

## Summary

A pull request with a known diff now gets a `ci_legs` list. `ci (fast)` stays on whenever `ci` is on. `ci (integration)` and `ci (publish)` stay off when every changed path is either fast-only (`tools/dev-cli/`, `tests/tools/dev-cli-tests/`) or a non-build path, unless the diff names a host file (both legs), a test-runner file (integration), or a publish file (publish). Any other build path runs every leg. Push, dispatch, and a null diff still run every leg, and `Packages-*` staging still keys off the event and ref, not the leg list.

`skill-lint` no longer follows every `tools/dev-cli/` edit. It follows skills, evals, vally pins, `SKILL.md` / `eval.yaml`, CI roots, the lint-skills verb and the client-name scan it runs first, and the CLI host files. `template-smoke` still follows all of `tools/dev-cli/`. The `ci` green-key marker is written only when `ci` is on and both extra legs run, so a fast-only pull request cannot make the master push skip ci-group. The required `ci` summary job still treats a successful narrowed matrix as success.

The generated-app workflow is unchanged. It still classifies `ci` only and uses a static three-leg matrix. `.template.config/template.json` already excludes `workflow-path-classes.cs` and `workflow-template-repository.cs`. `ci (fast)` still runs clean, build, the fast test group (which includes `dev-cli-tests`), and the generated-files check.

The recorded gates cover the logic: build 0/0, `dev-cli-tests` 479 passed with 23 on `WorkflowPathClasses_`, self-install, and `ganda repo audit`. The classify smoke in `### How to validate` matches the code (`ci_legs=["fast"]`, `ci_marker=false` for a release-packages-only pull request; every leg and `ci_marker=true` when the diff includes `.github/workflows/`). Local `template-smoke` is recorded as not run; the path rule still turns that job on for this diff, and the changed classify files are not packed into generated apps.

The behavior in `## Goal (implementer restatement)` matches the task. No findings.

## Issues

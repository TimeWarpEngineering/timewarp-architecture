# Round 1 — general
**Date:** 2026-11-24
**Scope reviewed:** branch `task/504-move-ci-onto-wslc-self-hosted-runners-and-adopt` vs merge-base `789928877` (workflow, setup-ci, green-key ledger, dev workflow classify, dev-cli tests) against task 504 and `## Goal (implementer restatement)`.

## Summary

Same-repo push and pull request runs of classify, ci-group, skill-lint, and template-smoke use the nuru 539 `runs-on` expression (`["self-hosted", "twe-001", "wslc"]` unless `CI_HOSTED_ONLY` is true). Fork pull requests, release, schedule, workflow_dispatch, the `ci` aggregator, and the new `green-key` job stay on `ubuntu-latest`. `ci (integration)` sets `matrix.hosted: true` because the wslc image has no Docker socket. The composite setup action saves and restores the NuGet cache only when `runner.environment == 'github-hosted'`.

Classify hashes the committed tree minus kanban task content, the SDK, and the classify runner, then turns off a gate whose marker already exists. The `green-key` job uploads `green-key-v1-<gate>-<key>` only after that gate succeeds, and only for a same-repo candidate. `Green-Key: full-run` and dispatch mode `full` do not skip. A skipped ci-group still lets the required `ci` check pass. The pure rules have 14 tests. `## Results` records `./bin/dev build` (0 warnings), dev-cli-tests (371 passed), and `ganda repo audit` exit 0, including that `self-hosted-runner` skips on a public repository. That record covers this change. Gates were not re-run.

The hosted integration image is outside the key, and `ci (publish)` on wslc is unproven until this pull request's CI. Both are stated in the design region and in `### Follow-ups / risks`. `aspire publish` is documented as writing files only. No defect beyond those recorded residuals.

## Issues

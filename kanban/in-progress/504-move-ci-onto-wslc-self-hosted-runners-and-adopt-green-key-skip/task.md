# Move CI onto wslc self-hosted runners and adopt green-key skip

## Depends on

- 503

## Description

Move `timewarp-architecture` same-repo CI jobs onto the org `wslc` self-hosted container runners on TWE-001 (`["self-hosted", "twe-001", "wslc"]`, matching `timewarp-nuru`) and adopt `green-key` skip (matching `timewarp-ganda` tasks 469, 470, and 711) so `master` merges and identical trees do not re-run the entire CI pipeline after a green PR.

## Requirements

- **Route same-repo CI jobs to `wslc` runners:** Match `timewarp-nuru/.github/workflows/workflow.yml` (task 539):
  - Same-repo `push` and same-repo `pull_request` jobs (`classify`, `ci-group`, `skill-lint`, `template-smoke`) run on `["self-hosted", "twe-001", "wslc"]` when `vars.CI_HOSTED_ONLY != 'true'`.
  - Fork pull requests, `release`, `workflow_dispatch`, and the lightweight `ci` status aggregator stay on `ubuntu-latest`.
- **NuGet caching on `wslc` vs hosted:** Only use `actions/cache` when `runner.environment == 'github-hosted'` (matching `timewarp-nuru`'s `setup-dotnet` composite action so shared `wslc` volumes do not pollute this repo's Actions cache).
- **Verify container/tooling compatibility on `wslc`:** Confirm `aspire-tests` / Testcontainers / Docker, `dotnet dev-certs`, `Aspire.Cli`, `helm`, and `node`/`vally-cli` work on `wslc`. If any leg (e.g. Docker-dependent `integration` or `template-smoke`) cannot run inside `wslc`, document why and keep only that leg on `ubuntu-latest`.
- **Adopt `green-key` markers and skip:** Follow `timewarp-ganda` tasks 469, 470, and 711:
  - Emit a `green-key` marker artifact after a full same-repo CI pass.
  - Skip redundant rebuild/retest on `master` merge pushes (or unchanged re-runs) whose green key already passed, preserving the `Green-Key: full-run` commit trailer and manual `workflow_dispatch` escape hatches.
- **Audit compliance:** Verify `ganda repo audit` (including `self-hosted-runner`) passes cleanly.

## Checklist

- [ ] Switch same-repo CI jobs in `.github/workflows/workflow.yml` to `["self-hosted", "twe-001", "wslc"]` with `CI_HOSTED_ONLY` fallback and fork-PR safety
- [ ] Gate `actions/cache` to `runner.environment == 'github-hosted'` in the composite setup action
- [ ] Verify Docker/Aspire/Helm/Node compatibility on `wslc` runners
- [ ] Implement `green-key` marker upload and skip logic in `dev workflow` + `workflow.yml`
- [ ] Run `./bin/dev build`, `dev-cli-tests`, and `ganda repo audit`

## Session

- Created: 2026-11-23 (from Task 500 walk performance review)

## Notes

- Reference implementations: `timewarp-nuru/.github/workflows/workflow.yml` (task 539, `wslc` runners) and `timewarp-ganda/.github/workflows/workflow.yml` (tasks 469, 470, 711, `green-key` skip + pack-only master).
- Parent performance review: **500** (`kanban/to-do/500-walk-performance-review-for-tasks-499-483-494-and-498/report/analysis.md`).

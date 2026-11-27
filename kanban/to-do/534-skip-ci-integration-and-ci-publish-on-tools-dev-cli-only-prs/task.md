# Skip ci (integration) and ci (publish) on tools/dev-cli-only PRs

## Description

From the `/tw-walk-retro` on task 533 (PR #714): a PR that changed only `tools/dev-cli/`, `.timewarp/dev.jsonc`, `.github/workflows/workflow.yml`, and `documentation/` ran the full `ci-group` matrix (`ci (fast)`, `ci (integration)` on Docker-hosted `ubuntu-latest`, and `ci (publish)`) plus `skill-lint` and `template-smoke`, even though zero files under `source/`, `skills/`, or `tests/` outside `dev-cli-tests` changed.

Today `WorkflowPathClasses` (`tools/dev-cli/services/workflow-path-classes.cs`) puts `tools/dev-cli/` in `CiRoots` so any change there turns on every job (`ci`, `skill`, `template`), and `.github/workflows/workflow.yml` gates the whole `ci-group` matrix behind a single `needs.classify.outputs.ci == 'true'` boolean.

Split the `ci` classification (or add targeted outputs for `ci (integration)` and `ci (publish)` and narrow `skill-lint` triggering for `tools/dev-cli/`) so a PR touching only `tools/dev-cli/` (plus non-build paths like `documentation/` and `kanban/`) runs only the gates that can actually be affected by `tools/dev-cli/` changes:
- `ci (fast)` (which builds the solution and runs `dev-cli-tests`, plus the generated-files check)
- `template-smoke` (because generated apps ship `tools/dev-cli/`), or only when template-shipped `tools/dev-cli/` files change
- `skill-lint` only when `skills/`, `evals/`, `.vally.yaml`, `SKILL.md`/`eval.yaml`, `tools/dev-cli/vally-cli.version`, `tools/dev-cli/endpoints/lint-skills-command.cs`, or `.github/workflows/` / `.github/actions/` change (not on unrelated `tools/dev-cli/` release/pack edits)
- Skip `ci (integration)` and `ci (publish)` on `tools/dev-cli/`-only PRs unless files that affect runtime container builds/publish or integration tests changed (for example `source/`, `tests/` other than `dev-cli-tests`, `tools/dev-cli/endpoints/publish-*`, `tools/dev-cli/services/publish-*`, `Directory.Build.props`, `Directory.Packages.props`, `.github/workflows/`, `.github/actions/`).

Preserve safety:
- An unknown diff (`changed is null`), a `push` to `master`, or `workflow_dispatch` with `mode=full` must still turn on every job (including `packages` / `pack` behavior on `master` untouched).
- Green-key markers (`tools/dev-cli/services/green-key.cs` and `green-key-ledger.cs`) and the required `ci` summary job in `.github/workflows/workflow.yml` must handle skipped `ci (integration)` / `ci (publish)` legs cleanly without breaking branch protection or green-key hits on a later `master` push.
- Keep `.github/app-workflows/workflow.yml` (generated-app workflow) consistent if it shares any `classify` outputs or `WorkflowCommand` types.

## Requirements

- [ ] Update `WorkflowPathClasses` (`tools/dev-cli/services/workflow-path-classes.cs`), `WorkflowTemplateRepository` (`tools/dev-cli/services/workflow-template-repository.cs`), and `.github/workflows/workflow.yml` so a PR changing only `tools/dev-cli/` (excluding publish/test-runner commands) skips `ci (integration)` and `ci (publish)`, and skips `skill-lint` unless skill-lint files (`lint-skills*`, `vally-cli.version`, `skills/`, `evals/`, `.vally.yaml`) or workflow/action files changed.
- [ ] Keep `ci (fast)` enabled for all `tools/dev-cli/` changes, and keep `push` to `master`, `workflow_dispatch`, and unknown-diff (`changed is null`) runs running the full matrix and `Packages-*` staging unchanged.
- [ ] Verify green-key accounting (`tools/dev-cli/services/green-key.cs`): a PR that skipped `ci (integration)` or `ci (publish)` must not write a `ci` green-key marker that would cause the subsequent `master` push to skip `ci (integration)` or `ci (publish)`.
- [ ] Add unit tests in `dev-cli-tests` covering `tools/dev-cli/`-only diffs (release/workflow/service edits vs publish-command edits vs `lint-skills`/`vally-cli.version` edits), `source/` diffs, `skills/`-only diffs, unknown diffs, and green-key marker gating.
- [ ] Run `./bin/dev build`, `./bin/dev test --project dev-cli-tests`, and `./bin/dev template-smoke` (since `tools/dev-cli/` ships in generated apps).

## Checklist

- [ ] Classify outputs split or narrowed in `WorkflowPathClasses`, `WorkflowTemplateRepository`, and `.github/workflows/workflow.yml`
- [ ] Green-key writer guarded so partial `ci-group` runs never poison full-CI green keys
- [ ] Generated-app workflow (`.github/app-workflows/workflow.yml`) checked for compatibility with `WorkflowCommand`
- [ ] `dev-cli-tests` added and passing
- [ ] `./bin/dev build` and `./bin/dev template-smoke` passing with 0 warnings

## Notes

- Origin: `/tw-walk-retro` on task 533 (PR #714), Finding 1. On PR #714 (`tools/dev-cli/`, `.timewarp/dev.jsonc`, `.github/workflows/workflow.yml`, `documentation/`), `ci (integration)` took 8m26s, `ci (publish)` took 4m26s, and `skill-lint` took 2m15s with no `source/` or `skills/` edits.
- Check `.github/workflows/workflow.yml` lines 131–285 (`classify`, `ci-group`, `ci`, `green-key`) and `tools/dev-cli/services/workflow-path-classes.cs`.

## Session

- Filed from `/tw-walk-retro` on task 533 (2026-11-27).

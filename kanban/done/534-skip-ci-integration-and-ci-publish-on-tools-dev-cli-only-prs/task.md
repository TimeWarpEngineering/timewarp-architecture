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

## Goal (implementer restatement)

A pull request that edits only the dev CLI (plus docs or kanban) should not pay for `ci (integration)`
(8 min, hosted Docker) and `ci (publish)` (4 min), and should not run `skill-lint` unless the edit
touches what `dev lint-skills` runs.

Acceptance:
- classify writes a `ci_legs` matrix list. A pull request whose build paths are all under
  `tools/dev-cli/` or `tests/tools/dev-cli-tests/` gets `["fast"]`; a dev CLI test-runner file adds
  integration, a publish file adds publish, a host file (`dev.cs`, props, global usings,
  `workflow-command.cs`) adds both. Any other build path (source/, tests/, root props,
  `.github/workflows/`, `.github/actions/`) runs every leg.
- push (master and feature), dispatch mode full, and an unknown diff always run every leg; Packages-*
  staging on master is unchanged.
- `skill-lint` turns on for skills/evals/vally paths, CI roots, and the dev CLI files lint-skills runs,
  not for other `tools/dev-cli/` edits. `template-smoke` still runs for every `tools/dev-cli/` edit.
- A run that skipped a leg writes no `ci` green-key marker (`ci_marker=false`), so the master push of
  that tree runs every leg.
- The generated-app workflow is unchanged and still compatible (its classify writes `ci` only).
- dev-cli-tests cover these cases; build is 0 warnings.

## Requirements

- [x] Update `WorkflowPathClasses` (`tools/dev-cli/services/workflow-path-classes.cs`), `WorkflowTemplateRepository` (`tools/dev-cli/services/workflow-template-repository.cs`), and `.github/workflows/workflow.yml` so a PR changing only `tools/dev-cli/` (excluding publish/test-runner commands) skips `ci (integration)` and `ci (publish)`, and skips `skill-lint` unless skill-lint files (`lint-skills*`, `vally-cli.version`, `skills/`, `evals/`, `.vally.yaml`) or workflow/action files changed.
- [x] Keep `ci (fast)` enabled for all `tools/dev-cli/` changes, and keep `push` to `master`, `workflow_dispatch`, and unknown-diff (`changed is null`) runs running the full matrix and `Packages-*` staging unchanged.
- [x] Verify green-key accounting (`tools/dev-cli/services/green-key.cs`): a PR that skipped `ci (integration)` or `ci (publish)` must not write a `ci` green-key marker that would cause the subsequent `master` push to skip `ci (integration)` or `ci (publish)`.
- [x] Add unit tests in `dev-cli-tests` covering `tools/dev-cli/`-only diffs (release/workflow/service edits vs publish-command edits vs `lint-skills`/`vally-cli.version` edits), `source/` diffs, `skills/`-only diffs, unknown diffs, and green-key marker gating.
- [ ] Run `./bin/dev build`, `./bin/dev test --project dev-cli-tests`, and `./bin/dev template-smoke` (since `tools/dev-cli/` ships in generated apps).

## Checklist

- [x] Classify outputs split or narrowed in `WorkflowPathClasses`, `WorkflowTemplateRepository`, and `.github/workflows/workflow.yml`
- [x] Green-key writer guarded so partial `ci-group` runs never poison full-CI green keys
- [x] Generated-app workflow (`.github/app-workflows/workflow.yml`) checked for compatibility with `WorkflowCommand`
- [x] `dev-cli-tests` added and passing
- [ ] `./bin/dev build` and `./bin/dev template-smoke` passing with 0 warnings

## Review

- Round 1: Done

## Notes

- Origin: `/tw-walk-retro` on task 533 (PR #714), Finding 1. On PR #714 (`tools/dev-cli/`, `.timewarp/dev.jsonc`, `.github/workflows/workflow.yml`, `documentation/`), `ci (integration)` took 8m26s, `ci (publish)` took 4m26s, and `skill-lint` took 2m15s with no `source/` or `skills/` edits.
- Check `.github/workflows/workflow.yml` lines 131–285 (`classify`, `ci-group`, `ci`, `green-key`) and `tools/dev-cli/services/workflow-path-classes.cs`.

## Session

- Filed from `/tw-walk-retro` on task 533 (2026-11-27).
- Implement (2026-11-27): `ci_legs` / `ci_marker` classify outputs, dynamic ci-group matrix,
  narrowed skill-lint rule, ci marker gate. `WorkflowTemplateRepository` lives at
  `tools/dev-cli/endpoints/workflow-template-repository.cs` (the brief named `services/`).
- Implement oracle: implemented by implementer-claude (claude, vendor anthropic, model claude-opus-5-5, response model claude-opus-5-5), session 5787cfc4-7044-4671-9466-ec6668122dd1, max-turns 500, Done — 2026-11-27T10:43:58Z
- Review oracle: Grok 4.7, tw-implementation-review effort 2, roster general, 1 round, disposition clean, session not reported, turn cap 120 (--max-turns; cursor uncapped). The behavior restatement matches the task. The acceptance list omits the task's local `template-smoke` run; see Learnings. — 2026-11-27
- Review oracle: review by reviewer-cursor (cursor, vendor xai, model grok-4.7), session not reported, max-turns uncapped (no --max-turns flag; bound 120), artifacts general.md, merged.md, disposition.md — 2026-11-27
- Review oracle: review by reviewer-cursor (cursor, vendor xai, model grok-4.7-high), session not reported, max-turns uncapped (no --max-turns flag; bound 120), artifacts general.md, merged.md, disposition.md — 2026-11-27T10:53:33Z

## Results

What changed:
- `tools/dev-cli/services/workflow-path-classes.cs`: `ClassifyCiLegs` and the `CiLegs` record.
  A pull request with a known diff runs `ci (integration)` / `ci (publish)` only when a build path
  outside `tools/dev-cli/` and `tests/tools/dev-cli-tests/` changed, or a dev CLI file that leg
  runs (test verb and services for integration; `publish-*` and `aspire-publish*` for publish;
  `dev.cs`, `Directory.Build.props`, `global-usings.cs`, `workflow-command.cs` for both). Push,
  dispatch, and unknown diffs run every leg. `IsSkillPath` no longer turns on for all of
  `tools/dev-cli/`: only the lint-skills verb, `vally-cli.version`, the spec lint, the client-name
  scan, and the host files. `tools/dev-cli/` still turns on template-smoke.
- `tools/dev-cli/endpoints/workflow-template-repository.cs`: classify writes `ci_legs` and
  `ci_marker`; the green-key writer counts ci only when every leg runs.
- `.github/workflows/workflow.yml`: ci-group matrix is `fromJSON(needs.classify.outputs.ci_legs)`
  (fallback: every leg); args and the hosted integration runner come from `matrix.name`; the ci
  green-key marker uploads only when `ci_marker == 'true'`. The `ci` required check is unchanged
  (a narrowed matrix that passes is `success`).
- Generated-app workflow: unchanged. Its classify writes `ci` only with a static matrix, and
  `.template.config/template.json` excludes both changed service files from generated apps.
- Not done locally: `./bin/dev template-smoke` (the PR's template-smoke job runs it, since
  `tools/dev-cli/` is a template path). Known limit: the dev CLI leg lists name entry points, not
  every transitive helper; the master push runs every leg, so a miss fails there.

### Gates

- `./bin/dev build`: passed, 0 warnings, 0 errors.
- `tests/tools/dev-cli-tests/workflow-path-classes-tests.cs`: not a standalone runfile (no
  `#if !JARIBU_MULTI`); ran `./bin/dev test --project dev-cli-tests --no-build`: passed (479 tests,
  0 failed); `--filter-class WorkflowPathClasses_`: 23 passed, 0 failed.
- `dotnet run --file tools/dev-cli/dev.cs -- self-install` (AOT publish of the changed dev CLI): passed.
- `ganda repo audit`: passed (5 passed, 0 failed, 0 warnings).

### How to validate

Smoke: in this worktree, build a throwaway commit that changes only
`tools/dev-cli/services/release-packages.cs` on the merge base (`git commit-tree`), write a
pull_request event JSON with that base and head, and run
`GITHUB_EVENT_NAME=pull_request GITHUB_EVENT_PATH=<event> GITHUB_OUTPUT=<out> ./bin/dev workflow --phase classify --attestation off`.

Expect: `ci=true`, `skill=false`, `template=true`, `ci_legs=["fast"]`, `ci_marker=false`, and the
line `ci-group legs: fast (no ci green-key marker).` Run against this branch's own head (it edits
`.github/workflows/`), expect `ci_legs=["fast","integration","publish"]` and `ci_marker=true`.
Both were run on 2026-11-27 with those outputs.

### Review

- Rounds: 1. Effort 2, roster: general. Reviewer: Grok 4.7. Session not reported. Turn cap 120 (--max-turns; cursor uncapped).
- Final counts: 0 bug, 0 suggestion, 0 nit. 0 open, 0 fixed, 0 wontfix.
- Disposition: **clean**. No wontfix and no escalations.
- Paths: `review/review-framework.md`, `review/round-1/general.md`, `review/round-1/merged.md`, `review/disposition.md`.

## Learnings

- The behavior in `## Goal (implementer restatement)` matches the task: fast-only legs for a dev-cli pull request, skill-lint only for the lint-skills surface, no `ci` green-key marker unless every leg runs, and a full matrix on push, dispatch `mode=full`, and an unknown diff.
- Misread (validation list only): the task requires a local `./bin/dev template-smoke` because `tools/dev-cli/` ships. The restatement's acceptance names `dev-cli-tests` and a 0-warning build, and does not name template-smoke. Results record that the local run was not done and that the changed classify files are excluded from generated apps. The PR path rule still turns the template-smoke job on. Not a product defect.

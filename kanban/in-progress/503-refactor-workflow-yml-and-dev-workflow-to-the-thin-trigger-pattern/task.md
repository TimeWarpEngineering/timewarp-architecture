# Refactor workflow.yml and dev workflow to the thin-trigger pattern

## Depends on

- 501

## Description

Refactor `.github/workflows/workflow.yml` (currently 594 lines — 200 lines longer than any other TimeWarp repo) and `tools/dev-cli/endpoints/workflow-command.cs` to match the org thin-trigger standard (~180–220 lines, modeled on `timewarp-nuru` and `timewarp-ganda`).

All pipeline logic should live in C# (`dev workflow`), shared runner setup should live in a single local composite action, and superseded runs should be cancelled via `concurrency:`.

## Requirements

- **Extract shared setup into a local composite action** (e.g. `.github/actions/setup-ci/action.yml`, modeled on `timewarp-nuru/.github/actions/setup-dotnet`) covering checkout, `setup-dotnet` (`global.json`), NuGet cache, optional `dotnet dev-certs https --trust`, and optional pinned Aspire CLI install (reading `<AspireVersion>` from `Directory.Packages.props`). Remove the 5x copy-pasted checkout/.NET/cache blocks and 3x copy-pasted Aspire CLI install blocks from `workflow.yml`.
- **Update `dev-cli-tests` workflow guards:** Update `AspireVersionSources.Validate` and `tests/tools/dev-cli-tests/aspire-run-tests.cs` to validate the single Aspire CLI install source (composite action or C# helper) instead of grepping duplicated inline `sed` lines in `workflow.yml`. Keep `SkillSpecLint.WorkflowDriftError` (`tests/tools/dev-cli-tests/skill-spec-lint-tests.cs`) passing.
- **Unify path classification in C# (`dev workflow --phase classify`):** Emit `ci`, `skill`, and `template` outputs directly from `WorkflowCommand` (using `PullRequestBuildClassifier` + repo rules) and delete the 54-line inline PowerShell `git diff`/regex block in `detect-paths`.
- **Move ingress-golden verification into C#:** Move `regenerate-generated` + `git diff --exit-code -- goldens/ingress` + clean-status check from `workflow.yml` (`ci (fast)`) into `dev workflow --phase test --group fast`.
- **Add `concurrency:` control:** Cancel superseded pull request runs and superseded `master` pushes while keeping `release` and `workflow_dispatch` uncancelled (matching `timewarp-nuru` / `timewarp-ganda`).
- **Drop PR-level Aspire publish artifact uploads:** Remove the 4 `Upload ... publish output` steps (`compose`, `helm`, `aca`, `aca-neon`, 56 lines) from `ci-group` — `dev publish all` still runs all targets, safety tests, and `helm lint` in `ci (publish)`.
- **Align actions, CLI invocation, and comments:** Use `dotnet run --file tools/dev-cli/dev.cs -- ...`, update GitHub Actions versions to current fleet standard, add `confirm=release` break-glass guard if appropriate for `workflow_dispatch`, and trim archaeological `# Task ...` comments (move durable rationale to `Design` regions).

## Goal (implementer restatement)

Shrink `.github/workflows/workflow.yml` to a thin trigger: YAML only says when to run and on which
runner; every decision and check lives in `dev workflow` (C#), and runner setup lives in one local
composite action.

Acceptance:

- One composite action (`.github/actions/setup-ci/action.yml`) sets up .NET from `global.json`,
  the NuGet cache (save or restore-only, per-job key), optional dev-cert trust (plus `SSL_CERT_DIR`),
  and the optional pinned Aspire CLI read from `<AspireVersion>`. No job repeats those blocks.
  Checkout stays one step per job (a local action cannot load before the repo is checked out).
- `dev workflow --phase classify` writes `ci`, `skill`, and `template` to `GITHUB_OUTPUT` for push
  and pull request; the inline PowerShell diff/regex step is gone. `.github/actions/**` counts as a
  build, skill, and template path.
- `dev workflow --phase test --group fast` (and any run whose tests include the fast group) runs
  `regenerate-generated` and fails on a `goldens/ingress` diff or untracked file; the YAML step is gone.
- `concurrency:` cancels superseded pull request runs and superseded pushes to the same ref;
  release, dispatch, and schedule runs never cancel.
- The four PR-level Aspire publish uploads are gone (`dev publish all` still runs in `ci (publish)`).
- Actions at the fleet versions, `dotnet run --file tools/dev-cli/dev.cs -- ...`, no archaeological
  `# Task NNN` comments (durable rationale moves to Design regions). No `confirm=release` guard:
  this repo's dispatch has no publishing mode.
- `AspireVersionSources.Validate` checks the composite action's install and that workflow.yml
  installs no Aspire CLI itself; `SkillSpecLint.WorkflowDriftError` keeps passing with the skill
  path rule now in C#.
- Gates: `./bin/dev build`, dev-cli-tests, `ganda repo audit`.

## Checklist

- [x] Create local composite action for shared CI setup (checkout, .NET, NuGet cache, optional dev-cert + Aspire CLI)
- [x] Emit `ci`, `skill`, and `template` from `dev workflow --phase classify` and remove inline PowerShell regexes
- [x] Move `regenerate-generated` + `goldens/ingress` diff check into `dev workflow --phase test --group fast`
- [x] Add `concurrency:` block and remove the 4 PR-level Aspire publish artifact upload steps
- [x] Update `AspireVersionSources.Validate` / `aspire-run-tests.cs` and verify `skill-spec-lint-tests.cs`
- [x] Run `./bin/dev build`, `dev-cli-tests`, and `ganda repo audit` (`dev template-smoke`: CI runs it on this PR, see Results)

## Session

- Created: 2026-11-23 (from Task 500 walk performance review)
- 2026-11-24 implement: a local smoke of `dev workflow --phase classify` under a fake push event ran
  `git fetch --depth=1` against the shared clone and made it shallow (the first `ganda repo audit`
  then failed `kanban-landed`: no merge base). Repaired with `git fetch --unshallow origin`, and the
  classifier now fetches only missing commits and uses `--depth=1` only on an already-shallow checkout.

## Notes

- Reference patterns: `timewarp-nuru/.github/workflows/workflow.yml`, `timewarp-ganda/.github/workflows/workflow.yml`, and `timewarp-ganda/source/timewarp-ganda/templates/repo-baseline/github/workflows/workflow.yml.template`.
- Parent performance review: **500** (`kanban/to-do/500-walk-performance-review-for-tasks-499-483-494-and-498/report/analysis.md`).

## Results

`workflow.yml` is 581 → 278 lines; the shared setup is `.github/actions/setup-ci/action.yml` (77 lines).

- **Composite action** `.github/actions/setup-ci`: setup-dotnet (global.json), NuGet cache
  (`save` / `restore` / `none`, per-job key segment), optional dev-cert trust that also exports
  `SSL_CERT_DIR` through `GITHUB_ENV`, optional Aspire CLI at `<AspireVersion>`. Checkout stays one
  `actions/checkout` step per job: a local action cannot load before the checkout exists.
- **Classify in C#**: job `detect-paths` is now `classify`. `dev workflow --phase classify` writes
  `ci` (PullRequestBuildClassifier), `skill`, and `template` (new `WorkflowPathClasses`,
  `tools/dev-cli/services/workflow-path-classes.cs`). Pull request uses the classifier's
  base...head diff; push reads before/after from `GITHUB_EVENT_PATH`, fetches them only if missing,
  and diffs. Unknown diff → every job on. The 54-line PowerShell regex step is gone.
  `.github/actions/**` was added to the push `paths`, to `.timewarp/dev.jsonc` `buildPaths` (else the
  classifier would treat it as non-build `.github/**`), and to the skill/template rules.
- **Ingress golden** moved into `RunPrAsync`: whenever the test step includes the fast group
  (`--group fast`, or no group), `regenerate-generated` runs, then `git diff --exit-code` and
  `git status --porcelain` on `goldens/ingress`.
- **concurrency:** pull request runs group by PR number, pushes by ref, both cancel in progress;
  release / dispatch / schedule get `run-<run_id>` and never cancel.
- **Removed** the four `aspire-*` publish artifact uploads (and comments claiming CI uploads them in
  `publish-target.cs`, `aspire-publish.cs`, `skills/tw-deploy/SKILL.md`).
- **Versions / CLI:** checkout@v7, setup-dotnet@v6, cache@v6, upload-artifact@v7, setup-node@v7
  (latest releases; nuru uses the same majors). Every call is `dotnet run --file tools/dev-cli/dev.cs`.
  The release job passes the OIDC key as `NUGET_API_KEY` env (already read by `PushAsync`) instead of
  `--api-key` on the command line; a dispatch has no key and stays pack-only.
  **No `confirm=release` guard:** this repo's dispatch has no publishing mode (merge = pack-only,
  probe, nightly), so there is nothing to break-glass. Recorded in the WorkflowCommand Design region.
- **Comments:** `# Task NNN` archaeology removed from the YAML; durable rationale (phases, storage
  cap, shallow checkouts, cache policy, golden check, classify rules, release gating) is in the
  `WorkflowCommand` / `WorkflowPathClasses` / `AspireVersionSources` Design regions and the action.
- **Guards:** `AspireVersionSources.Validate` now takes the setup action (`SetupActionRelativePath`);
  new `WorkflowInstallError` fails when workflow.yml installs Aspire.Cli itself or never asks the
  action for it. `SkillSpecLint.WorkflowDriftError` now requires the skill-lint job to be gated on
  `workflow --phase classify` `outputs.skill` (the `^tools/dev-cli/` regex is gone from YAML; the
  rule is tested on `WorkflowPathClasses`), and `LintSkillsCommand` is the `--file` form.
- AGENTS.md and the RegenerateGenerated Design region now name `dev workflow` as the golden check.

### Gates

- `./bin/dev build` — Build succeeded, 0 Warning(s), 0 Error(s) (rerun after the last product edit).
- `dotnet run --file tools/dev-cli/dev.cs -- self-install` — dev CLI (runfile, not in the solution) compiles clean under its analyzers.
- `tests/tools/dev-cli-tests` (`workflow-path-classes-tests.cs` new; `aspire-run-tests.cs`, `skill-spec-lint-tests.cs` edited) — not standalone runfiles (csproj test project, no `#if !JARIBU_MULTI`); ran the built project once: `tests/tools/dev-cli-tests/bin/Release/net11.0/dev-cli-tests` — 353 total, 353 succeeded, 0 failed. CI runs it too.
- `./bin/dev lint-skills` (skills/tw-deploy/SKILL.md changed) — 9 skills + evals passed.
- actionlint 1.7.12 (`docker run rhysd/actionlint`) on workflow.yml — clean.
- `ganda repo audit` — first run exit 1 (`kanban-landed`: no merge base, caused by the shallow fetch noted in Session); after `git fetch --unshallow` and the classifier fix, rerun exit 0 with one advisory (`vscode-window-icon`: `.vscode/settings.json` is git-ignored local state, nothing to commit).
- Not run locally: `dev template-smoke` (only comment/doc lines changed in template content; this PR touches template paths so CI's template-smoke job runs it) and the full `dev workflow --phase test --group fast` (CI's `ci (fast)` runs the new golden step).

### How to validate

- Smoke: `GITHUB_EVENT_NAME=push GITHUB_EVENT_PATH=<file with {"before":"<origin/master~2>","after":"<origin/master>"}> GITHUB_OUTPUT=/tmp/out ./bin/dev workflow --phase classify; cat /tmp/out`
  Expect: changed paths listed, `ci=true`, and `skill`/`template` true only when a listed path matches; `git rev-parse --is-shallow-repository` still prints `false`.
- Smoke: `./bin/dev workflow --phase classify` with no GitHub env.
  Expect: "Changed paths unknown. Every path-gated job runs." and `ci=true skill=true template=true`.
- Smoke: on this PR's checks.
  Expect: `classify` writes all three outputs; `ci (fast)` log shows `test (fast) -> ingress golden` and the regenerate step; no `aspire-*` artifacts on the run; `Lint skill specs` and `template-smoke` run (this PR changes `.github/` and `tools/dev-cli/`); `ci` green.


# Ship a CI workflow in generated apps

## Description

A generated app has no `.github/` at all. The pack allow-list does not include it, and this repo's
`.github/workflows/workflow.yml` is the template's own pipeline (NuGet release, template-smoke,
skill lint). An instance has to hand-write CI before a `ganda task work` walk can merge on green,
and hand-written workflows drift from each other.

Ship a purpose-built workflow for generated apps, plus the shared setup action. It is not a copy of
this repo's workflow: it has the jobs an app needs and nothing about being a template.

## Depends on

- 530

## Propagation

template-only

## Requirements

- A generated app has `.github/workflows/workflow.yml` and `.github/actions/setup-ci/action.yml`.
- The instance workflow is a thin trigger, the same shape as this repo's: every decision and check
  is a `dev workflow` phase. The YAML says when to run and on which runner.
- Jobs: classify, the ci-group phases (fast, integration, publish) and the single required `ci`
  check over them. The publish phase runs `dev publish` and the production-safety suites.
- Not in the instance workflow: NuGet pack and release, template-smoke, skill-lint, anything that
  reads `.template.config/` or `timewarp-templates/`.
- `on:` includes `pull_request` with no paths filter. `ganda pr merge` treats a base branch as
  CI-configured when a workflow triggers `pull_request`, and refuses a pull request that registers
  no check.
- Runners default to `ubuntu-latest`. The self-hosted wslc labels are this repo's; an instance opts
  in with a repository variable, not an edit.
- Green key: decide whether the instance workflow carries the classify key and the `green-key`
  marker job (task 504). Task 504 (`kanban/done/504-*`) already enabled the green-key skip on this repo. Say which in Results.
- `setup-ci` ships as is and is the same file in both places: one source, used by this repo's
  workflow and packed for the instance.
- The instance workflow does not run in this repo. Keep it outside `.github/workflows/` here and map
  it into place at generation (a `template.json` rename or source mapping).
- Flag-off output is valid: with api, grpc, yarp or postgres off the workflow has no job or phase
  that needs the missing project.
- `dev template-smoke` asserts both files are present in the generated app and that the workflow
  parses (note: neither `actionlint` nor a YAML parser is currently installed or used in this repo; use a YAML parse check or structural assertions matching `tests/tools/dev-cli-tests/`).
- The deploy job is not part of this task. Task 529 adds it to this file.

## Goal (implementer restatement)

A generated app gets working CI on day one, so a `ganda task work` walk can merge it on green
without anyone hand-writing a workflow.

Acceptance:

- The app contains `.github/workflows/workflow.yml` and `.github/actions/setup-ci/action.yml`, and
  nothing else under `.github/`.
- The app workflow is a thin trigger over `dev workflow`: classify, then ci (fast), ci
  (integration), ci (publish), then the required `ci` check over them. No NuGet release,
  template-smoke, skill lint, or template-content paths.
- `pull_request` has no paths filter, so every pull request reports `ci`.
- Runners are `ubuntu-latest` unless the repository sets a variable; no wslc labels in the file.
- Green key: decided and recorded in Results.
- `setup-ci` has one source in this repo, used by this repo's workflow and packed as is.
- The app workflow's source sits outside `.github/workflows/` here and is mapped into place at
  generation.
- The workflow has nothing flag-specific, so every flag-off variant gets a valid file, and the
  `dev workflow` phases run in a generated app.
- `dev template-smoke` (default and `--matrix`) checks both files ship and the workflow parses and
  has the app's shape.
- readme says a generated app includes CI and what to configure (required check `ci`, variable).

## Checklist

- [x] Instance workflow source file and its mapping to `.github/workflows/workflow.yml`.
- [x] Pack `setup-ci` from its one source.
- [x] `dev workflow` phases run in a generated app (classify, test groups, publish), with no
      template-only path rule in classify.
- [x] Runner selection variable; hosted default.
- [x] Green key decision.
- [x] `dev template-smoke` assertions, default variant and `--matrix`.
- [x] `readme.md`: a generated app includes CI; what to configure in the repo (required check `ci`,
      variables).

## Notes

- Steven's decision 2026-11-25: ship a `.github/` for generated apps, purpose-built.
- `ganda` CI rules: `source/timewarp-ganda/services/pr-ci-policy.cs` and
  `services/ci-configured/ci-configured-probe.cs` in timewarp-ganda.
- The first instance repo (see task 491) has one hand-written workflow today. It takes this one
  when it is regenerated.

## Session

- Created: f7eccb81-87c9-4fb4-a0c4-0fb7417d4a2f (2026-11-25)
- 2026-11-26 implement (ganda task work): app workflow, setup-ci pack, green key for the ci gate,
  template-smoke check, readme. `dev workflow` phases run by hand in generated apps (see Results).
- Implement oracle: implemented by implementer-claude (claude, vendor anthropic, model claude-opus-5-5, response model claude-opus-5-5), session 582162b7-1afd-4c49-9dfa-857b3355486d, max-turns 500, Done — 2026-11-26T09:54:55Z

## Results

A generated app now ships `.github/workflows/workflow.yml` and `.github/actions/setup-ci/action.yml`,
and nothing else under `.github/`.

**Where the workflow lives.** Its source is `.github/app-workflows/workflow.yml`, outside
`.github/workflows/`, so it never runs in this repository. The pack ships it at that path, and the
`(true)` modifier in `.template.config/template.json` renames it to `.github/workflows/workflow.yml`
at generation. The same modifier excludes `.github/workflows/**`, so this repository's own workflow
cannot reach an app, even from a repo-root install. `setup-ci` packs from its one file at
`.github/actions/setup-ci/`, unchanged. This repository's workflow uses the same file.

**The app workflow.** It is a thin trigger, the same shape as this repository's. Jobs: classify
(`dev workflow --phase classify`), ci-group (ci (fast), ci (integration), ci (publish); publish is
`dev publish all` plus the production-safety suites and helm lint), `ci` (the required check:
classify must succeed, ci-group must succeed or skip), and green-key. Triggers: `pull_request` with
no paths filter; push to master, main and `feature/**` with a paths filter; `workflow_dispatch`
(a full run). No release, NuGet, template-smoke, skill-lint or template-content paths. Nothing in it
depends on a flag, so every variant gets the same file.

**Runners.** Every job defaults to `ubuntu-latest`. The repository variable `CI_RUNNER_LABELS` (a
JSON array of labels) moves classify, ci (fast) and ci (publish) to self-hosted runners for a push
or a same-repo pull request. ci (integration) stays hosted because it needs Docker, and fork pull
requests always run hosted. The wslc labels are not in the file.

**Green key: carried, for the ci gate.** Without it, every merge of a green pull request runs the
whole CI again on the push to master. In a private app repository that costs Actions minutes for
nothing. So `green-key.cs` and `green-key-ledger.cs` now ship (they were template-only in task
530). The app's classify computes the key, turns ci off on a marker hit, and writes `green_key`
and `green_key_writer`. The app's green-key job uploads the ci marker after ci-group passes. The
skill and template gates in the ledger are a `templateRepository` region. `WorkflowJobs` and
`GreenKeyObservation` moved into `green-key.cs`. The shared `GreenKey.OutputLines` writes the two
green-key outputs for both repositories, replacing the duplicated writer rule. Classify in an app
has no template-only path rule: `workflow-path-classes.cs` stays excluded, and the app's ci output
comes from `PullRequestBuildClassifier` and its own `.timewarp/dev.jsonc`.

**Checks.** `services/template-smoke/app-ci-workflow-shape.cs` (pure) reads the block-YAML subset
that workflows use. It reports bad indentation, tabs, unclosed one-line values, duplicate keys and
lines that are neither a key nor an item, and returns the key paths. `Check` then requires
`on.pull_request` with no `paths` or `paths-ignore`, the jobs classify, ci-group, ci (with no name
override) and green-key, each `dev workflow` phase, `setup-ci` as the only local action, and no
Aspire.Cli install outside `setup-ci`. It refuses the release, skill-lint and template-smoke jobs
and template-only text (NuGet login, `.template.config`, `timewarp-templates`, vally, wslc
labels, `#if`). template-smoke (`TemplateSmokeCiWorkflow`, every variant, right after generation)
asserts the app's `.github/` is exactly the two files, that both equal their sources byte for byte,
and that the workflow passes `Check`. `.github/app-workflows/` is a template path for this
repository's classify and a build path in `.timewarp/dev.jsonc` (a templateRepository region), so a
change to it runs template-smoke and dev-cli-tests.

**Docs.** readme.md (what a generated app contains: CI, the required check `ci`, and
`CI_RUNNER_LABELS`), AGENTS.md, and a header note in this repository's workflow.yml.

**Follow-up (not filed by this walk).** Every generated app keeps the monorepo AppHost's
`UserSecretsId` (`a3d4de92-…`). `template.json` has no `guids` entry, so two apps on one machine
share user secrets, including generated postgres passwords. It predates this task.

### Gates

- `dotnet run --file tools/dev-cli/dev.cs -- self-install`: installed (dev-cli changed; rerun after the CA1307 fix).
- `./bin/dev build`: passed, 0 warnings and 0 errors (the first run failed on CA1307 in `app-ci-workflow-shape.cs`, fixed).
- `./bin/dev test tests/tools/dev-cli-tests/app-ci-workflow-shape-tests.cs`: 9/9 passed (new file; Source 4, Check 3, ParseKeyPaths 2).
- `./bin/dev test tests/tools/dev-cli-tests/green-key-tests.cs`: passed, 4 classes (new `Writer_Should_NeedACandidateAKeyAndAGateLeftOn`).
- `./bin/dev test tests/tools/dev-cli-tests/workflow-path-classes-tests.cs`: passed, 4 classes.
- `./bin/dev test tests/tools/dev-cli-tests/aspire-deploy-never-automated-given-tests.cs`: passed (it scans `.github/` recursively, so the app workflow is covered).
- dev-cli-tests files are classes in the dev-cli-tests project, not standalone runfiles (no `#if !JARIBU_MULTI` guard).
- `./bin/dev template-smoke`: SUCCEEDED (SmokeDefault: "Generated app ships its CI workflow and setup-ci").
- `./bin/dev template-smoke --matrix`: SUCCEEDED (all 7 variants passed the CI workflow check: SmokeDefault, SmokeNoPostgres, SmokeNoApi, SmokeNoWeb, SmokeNoGrpc, SmokeNoYarp, SmokeWebOnly).
- `./bin/dev check-version`: source is one prerelease ahead of the published 2.0.0-beta.31, so no bump is needed.
- `ganda repo audit`: passed with one advisory, vscode-window-icon (the git-ignored local `.vscode/settings.json` was missing in this worktree). `ganda repo audit --fix` wrote it and every check passed. No tracked file changed.
- Manual, `dev workflow` in generated apps (copied to a short /tmp path, see below):
  - SmokeDefault, `--phase classify` (GITHUB_EVENT_NAME=push): wrote `ci=true`, `green_key=<64 hex>`, `green_key_writer=true`, and no skill or template output.
  - SmokeDefault, `--phase test --group fast`: 4/4 projects passed.
  - SmokeDefault, `--phase publish`: compose, kubernetes, aca and aca-neon published, safety suites passed, helm lint passed.
  - SmokeWebOnly (api, grpc, yarp off): fast 3/3 passed, and publish passed for all four targets.
  - SmokeNoWeb: fast 1/1 passed, and publish passed for all four targets.
  - `--phase test --group integration` was not run locally. It boots the AppHost, whose user secrets this machine shares with the monorepo (follow-up above). The nightly `template-smoke --matrix --app-tests` runs those suites in every generated app, and CI runs the phase.
  - Local-only finding: from the nested `artifacts/template-smoke/work/<App>` path, every `dotnet test` with coverage aborted (PAL_SEHException, exit 134). The coverage profiler path there is 279 characters. The same binaries pass from a short path, so this does not affect CI, where checkout paths are short.
  - `~/.microsoft/usersecrets/a3d4de92-…/secrets.json` had the same SHA-256 before and after the publish runs.

### How to validate

Smoke:

1. `./bin/dev test tests/tools/dev-cli-tests/app-ci-workflow-shape-tests.cs`
2. `./bin/dev template-smoke`, then
   `find artifacts/template-smoke/work/SmokeDefault/.github -type f`
3. Optional: `./bin/dev template-smoke --variant SmokeWebOnly`

Expect:

1. 9/9 pass.
2. "Generated app ships its CI workflow and setup-ci." and "Template smoke SUCCEEDED". The find
   lists exactly `.github/workflows/workflow.yml` and `.github/actions/setup-ci/action.yml`. The
   workflow is identical to `.github/app-workflows/workflow.yml`.
3. The same CI-workflow line, and the variant succeeds.

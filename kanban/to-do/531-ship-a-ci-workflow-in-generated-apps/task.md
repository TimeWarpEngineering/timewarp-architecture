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

## Checklist

- [ ] Instance workflow source file and its mapping to `.github/workflows/workflow.yml`.
- [ ] Pack `setup-ci` from its one source.
- [ ] `dev workflow` phases run in a generated app (classify, test groups, publish), with no
      template-only path rule in classify.
- [ ] Runner selection variable; hosted default.
- [ ] Green key decision.
- [ ] `dev template-smoke` assertions, default variant and `--matrix`.
- [ ] `readme.md`: a generated app includes CI; what to configure in the repo (required check `ci`,
      variables).

## Notes

- Steven's decision 2026-11-25: ship a `.github/` for generated apps, purpose-built.
- `ganda` CI rules: `source/timewarp-ganda/services/pr-ci-policy.cs` and
  `services/ci-configured/ci-configured-probe.cs` in timewarp-ganda.
- The first instance repo (see task 491) has one hand-written workflow today. It takes this one
  when it is regenerated.

## Session

- Created: f7eccb81-87c9-4fb4-a0c4-0fb7417d4a2f (2026-11-25)

## Results (required before done)

### How to validate (required before done)

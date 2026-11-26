# Add release-triggered Azure deploy for generated apps

## Description

A generated app can only be deployed by an operator running `dev deploy --target <t>` from a machine
that is logged in to Azure. The `tw-deploy` skill says deploying is "never automated in CI" and that
no workflow step may call `dev deploy`.

Steven's decision (2026-11-25): deploy on release only is the pattern we want. A merge still never
deploys. Publishing a GitHub release does.

Give a generated app that path: a workflow job that runs on a published release (and on manual
dispatch), logs in to Azure with GitHub OIDC, and runs the same `dev deploy --target <t>` an operator
runs. Change the skill to say so.

## Depends on

- 531

## Propagation

likely

## Requirements

- A deploy job for generated apps, triggered by `release: published` and `workflow_dispatch` only.
  No push or pull-request trigger reaches it.
- The job calls `dev deploy --target <t>` (publish, `aspire deploy`, migrate, smoke). It does not
  re-implement those steps in YAML. Pass `--yes --no-browser` (`deploy-runner.cs` refuses redirected stdin without `--yes`, and `--no-browser` prints the URL).
- Azure login is GitHub OIDC (`azure/login`, `id-token: write`), with client id, tenant id and
  subscription id read from repository variables. No client secret.
- The target is configuration, not an edit to the YAML: a repository variable, default `aca-neon`.
- `aca-neon` gets `neon-pooled-connection-string` and `neon-direct-connection-string` from GitHub
  Actions secrets. Nothing secret is written to a file or echoed.
- The job runs in a GitHub `Production` environment, so an instance can add required reviewers.
- The job is added to the instance workflow that task 531 ships (`.github/workflows/workflow.yml` in
  a generated app), and uses the shared `setup-ci` action. It is not a second workflow file, and it
  does not go in this repo's own `workflow.yml`.
- The federated credential subject is documented. A `release` event runs on a tag ref, and a
  credential that trusts only `refs/heads/master` refuses it. The first instance worked around this
  with a master-subject dispatch; prefer an `environment:Production` subject, which covers both
  triggers.
- `skills/tw-deploy/SKILL.md` is amended: the rule becomes "a merge never deploys; a published
  release or a manual dispatch may". The lines "never automated in CI", "(never CI)" and "No CI job,
  workflow step or `dev workflow` mode calls it, and none may" are rewritten to match. `dev publish`
  on every CI run stays as is.
- The one-time Azure and GitHub setup (app registration, service principal, federated credential,
  role assignments, repository variables and secrets) is documented as a prerequisite, in the skill
  or its `references/`.

## Goal (implementer restatement)

A generated app can deploy itself to Azure from GitHub Actions, but only when someone publishes a
GitHub release or runs the workflow by hand and asks for a deploy. A push or a pull request never
deploys.

Acceptance criteria:

- The app workflow source (`.github/app-workflows/workflow.yml`, shipped as the app's
  `.github/workflows/workflow.yml`) has a `deploy` job. It runs on `release: published`, and on
  `workflow_dispatch` when the dispatch's `deploy` input is on. On those runs the CI jobs do not
  run. On push and pull request the deploy job is skipped.
- The job runs in the `Production` environment, logs in with `azure/login` over GitHub OIDC
  (`id-token: write`, client, tenant and subscription ids from repository variables, no client
  secret), installs the pinned Aspire CLI through `setup-ci` (`aspire-cli: 'true'`), and runs
  `dev deploy --target <t> --yes --no-browser`. `<t>` is the `DEPLOY_TARGET` repository variable,
  default `aca-neon`. The Neon connection strings come from Actions secrets into `Parameters__`
  environment variables; nothing secret is written to a file or echoed.
- `AppCiWorkflowShape.Check` (run by dev-cli-tests over the source and by `dev template-smoke`
  over every generated variant) requires that deploy job and its gating, and refuses a deploy
  invocation anywhere else and `id-token` outside the deploy job.
- The test that said no workflow may ever deploy now says: only the app workflow's release-gated
  deploy job may run `dev deploy`; nothing else deploys, deprovisions or opens.
- `tw-deploy` says "a merge never deploys; a published release or a manual dispatch may", and a new
  reference documents the one-time Azure and GitHub setup (app registration, service principal,
  federated credential with subject `repo:<owner>/<repo>:environment:Production`, role assignments,
  variables, secrets). The dev-cli and AppHost comments that said "never CI" match the new rule.

## Checklist

- [x] Deploy job in the instance workflow from task 531.
- [x] Pinned Aspire CLI install in the job through `setup-ci` (`aspire-cli: true`).
- [x] `dev deploy` runs non-interactively under OIDC (no `az login` prompt, no browser).
- [x] `tw-deploy` skill text and `references/azure-container-apps-neon.md` updated.
- [x] Prerequisite setup documented, with the federated credential subject.
- [x] `dev template-smoke` asserts the deploy job is present in the generated workflow.
- [x] `dev lint-skills` passes.

## Review

- Round 1: Done

## Notes

- Not in scope: a `dev bootstrap-oidc` command that creates the OIDC login. Archived task 323 left
  that as an open question ("Steven is not convinced this belongs in Architecture"). This task only
  documents the setup. If the command is wanted here, reopen 323.
- The first instance repo (see task 491) has a working version of this job and of the bootstrap
  command to read from: its `.github/workflows/workflow.yml` deploy job and
  `tools/dev-cli/endpoints/` `deploy` and `bootstrap-oidc` commands. Its own task 057 moves that job
  from push-to-master to release-only.
- Task 531 ships the instance CI workflow this job lives in. Task 530 ships the dev CLI the job
  calls. 531 depends on 530, so this task waits on both.
- Task 527 makes a web-only (yarp off) app reachable on the Azure targets. A web-only instance needs
  it before this job is useful, but this task does not depend on it.
- Grooming note: align the "never CI" / "never a CI step" comments in `tools/dev-cli/endpoints/deploy-command.cs`, `tools/dev-cli/services/deploy-runner.cs`, `tools/dev-cli/services/deploy/aspire-deploy.cs`, and `source/container-apps/aspire/projects/aspire-app-host/program.cs` with the amended `tw-deploy` rule.

## Session

- Created: f7eccb81-87c9-4fb4-a0c4-0fb7417d4a2f (2026-11-25)
- Implement oracle: implemented by implementer-claude (claude, vendor anthropic, model claude-opus-5-5, response model claude-opus-5-5), session 4ab94756-4b94-4b6f-a09d-3c3e40c74b22, max-turns 500, Done — 2026-11-26T10:37:50Z
- Review oracle: grok session 01a2176e-f942-73f3-8fbe-c8fa2931659b (2026-11-26), effort 3, roster general, round 1
- Review oracle: review by reviewer-grok (grok, vendor xai, model grok-4.7, response model grok-4.7-build), session 01a2176e-f942-73f3-8fbe-c8fa2931659b, max-turns 200, artifacts general.md, merged.md, disposition.md — 2026-11-26T10:48:09Z

## Results

What changed:

- `.github/app-workflows/workflow.yml` (ships as the app's `.github/workflows/workflow.yml`):
  `on.release: types: [published]`, a `workflow_dispatch` boolean input `deploy` (default false),
  and a `deploy` job. Its `if` is `github.event_name == 'release' || (github.event_name ==
  'workflow_dispatch' && inputs.deploy)`. `classify` and `ci` carry `github.event_name !=
  'release' && !inputs.deploy`, so a deploy run runs no CI and reports no `ci` check (ci-group and
  green-key are skipped with classify). The job runs on `ubuntu-latest` in environment
  `Production`, concurrency group `deploy-production` (no cancel), `id-token: write`, setup-ci
  with `aspire-cli: 'true'`, `azure/login@v3` with client/tenant/subscription ids from variables,
  then one pwsh step that refuses a target other than aca/aca-neon and missing Azure variables,
  maps the variables and secrets to `Azure__*` / `Parameters__*` only when set, and runs
  `dotnet run --file tools/dev-cli/dev.cs -- deploy --target $env:DEPLOY_TARGET --yes --no-browser`.
- `AppCiWorkflowShape` (dev-cli-tests and `dev template-smoke`): `deploy` is a required job;
  `DeployJobErrors` requires the release trigger, the dispatch input, the deploy `if` (release and
  dispatch, no push/pull_request), `environment: Production`, `id-token: write`, `azure/login@`,
  no `client-secret`/`creds:`, exactly the one deploy line, and the CI skip on classify and ci. It
  refuses `id-token` and any deploy/deprovision/open/aspire deploy outside the deploy job.
  `id-token` left the template-only text list (the template repository's own workflow still fails
  the check, now as "Missing job deploy." and "id-token appears outside the deploy job.").
- `aspire-deploy-never-automated-given-tests.cs` became `aspire-deploy-release-only-given-tests.cs`
  (`ReleaseOnly_Given_`): every `.github` workflow and `workflow-command.cs` is scanned, minus the
  app workflow's deploy job and full-line comments; the regex moved to
  `AppCiWorkflowShape.DeployInvocation`. 8 new failure cases in `app-ci-workflow-shape-tests.cs`.
- `skills/tw-deploy`: SKILL.md rule is now "a merge never deploys; a published release or a manual
  dispatch may" (description, the bullet, and the Deploying paragraph); new
  `references/release-deploy.md` (events table, configuration table, az/gh one-time setup, the
  `repo:<owner>/<repo>:environment:Production` subject and why a `ref:refs/heads/master` one
  refuses, private-repo plan note, GITHUB_TOKEN-created releases do not trigger, re-run);
  `references/azure-container-apps-neon.md` links it. SKILL.md is 499 lines (vally limit 500).
- Comments aligned: `deploy-command.cs` (and its route description), `deploy-group.cs` description,
  `deploy-runner.cs`, `aspire-deploy.cs`, `workflow-command.cs` (and the app release-mode refusal
  text), `template-smoke-ci-workflow.cs`, AppHost `program.cs`.

Gates:

- `./bin/dev build` (after `dotnet run --file tools/dev-cli/dev.cs -- self-install`): passed, 0 warnings, 0 errors.
- `./bin/dev test tests/tools/dev-cli-tests/app-ci-workflow-shape-tests.cs`: passed, 3 classes (Source 4, Check 11, ParseKeyPaths 2). dev-cli-tests is a project, not a standalone runfile (no `#if !JARIBU_MULTI`); `dev test <file>` filters its classes.
- `./bin/dev test tests/tools/dev-cli-tests/aspire-deploy-release-only-given-tests.cs`: passed, 3/3.
- `./bin/dev lint-skills`: passed (first run failed on SKILL.md at 501 lines; a duplicate link was dropped).
- `./bin/dev template-smoke` (default variant): SUCCEEDED, "Generated app ships its CI workflow and setup-ci."
- `./bin/dev check-version`: source 2.0.0-beta.32 is ahead of NuGet 2.0.0-beta.31; no bump needed.
- `ganda repo audit`: Repository passes, 1 advisory warning (`vscode-window-icon`: `.vscode/settings.json` is missing in this worktree; that file is git-ignored, so there is nothing to commit).

Not verified here: a real release run against Azure (needs an instance repository with the OIDC
setup). The deploy step's behavior is the existing `dev deploy` path.

### How to validate

Smoke:

```bash
./bin/dev test tests/tools/dev-cli-tests/app-ci-workflow-shape-tests.cs tests/tools/dev-cli-tests/aspire-deploy-release-only-given-tests.cs
./bin/dev template-smoke
```

Expect: both files pass; template-smoke prints "Generated app ships its CI workflow and setup-ci."
and "Template smoke SUCCEEDED". In an instance with the setup in
`skills/tw-deploy/references/release-deploy.md`, publishing a release starts a run where only
`deploy` runs (in `Production`), and a push or pull request run shows `deploy` skipped.

### Review disposition

Round 1, roster general, effort 3. Counts: bug 0, suggestion 0, nit 0 (all open/fixed/wontfix zero). Disposition: clean. No wontfix and no escalation. The implementer restatement matches the original task; no misread. Paths: `review/review-framework.md`, `review/round-1/general.md`, `review/round-1/merged.md`, `review/disposition.md`. Review did not re-run build, test, or audit.

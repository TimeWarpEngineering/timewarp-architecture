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
  re-implement those steps in YAML. `--no-browser` so it prints the URL.
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

## Checklist

- [ ] Deploy job in the instance workflow from task 531.
- [ ] Pinned Aspire CLI install in the job through `setup-ci` (`aspire-cli: true`).
- [ ] `dev deploy` runs non-interactively under OIDC (no `az login` prompt, no browser).
- [ ] `tw-deploy` skill text and `references/azure-container-apps-neon.md` updated.
- [ ] Prerequisite setup documented, with the federated credential subject.
- [ ] `dev template-smoke` asserts the deploy job is present in the generated workflow.
- [ ] `dev lint-skills` passes.

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

## Session

- Created: f7eccb81-87c9-4fb4-a0c4-0fb7417d4a2f (2026-11-25)

## Results (required before done)

### How to validate (required before done)

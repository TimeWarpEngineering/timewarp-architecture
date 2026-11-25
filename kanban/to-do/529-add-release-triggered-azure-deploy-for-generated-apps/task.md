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
- The job reaches a generated app. `.github/` is not in the template pack today
  (`timewarp-templates/source/timewarp-architecture-template/*.csproj`), and this repo's
  `workflow.yml` is the template's own CI and NuGet release. Ship the instance workflow as template
  content under a path that lands at `.github/workflows/` in the generated app, without it running in
  this repo.
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

- [ ] Instance deploy workflow file, packed to `.github/workflows/` in a generated app.
- [ ] Confirm `tools/dev-cli` and `bin/dev` are available to that job in a generated app, and build
      the dev CLI in the job if not.
- [ ] Pinned Aspire CLI install in the job, matching `<AspireVersion>` (same as the CI jobs).
- [ ] `dev deploy` runs non-interactively under OIDC (no `az login` prompt, no browser).
- [ ] `tw-deploy` skill text and `references/azure-container-apps-neon.md` updated.
- [ ] Prerequisite setup documented, with the federated credential subject.
- [ ] `dev template-smoke` asserts the workflow file is present in the generated app.
- [ ] `dev lint-skills` passes.

## Notes

- Not in scope: a `dev bootstrap-oidc` command that creates the OIDC login. Archived task 323 left
  that as an open question ("Steven is not convinced this belongs in Architecture"). This task only
  documents the setup. If the command is wanted here, reopen 323.
- The first instance repo (see task 491) has a working version of this job and of the bootstrap
  command to read from: its `.github/workflows/workflow.yml` deploy job and
  `tools/dev-cli/endpoints/` `deploy` and `bootstrap-oidc` commands. Its own task 057 moves that job
  from push-to-master to release-only.
- A generated app ships no CI workflow at all today. This task adds the deploy job only. A CI
  workflow for instances is a separate question.
- Task 527 makes a web-only (yarp off) app reachable on the Azure targets. A web-only instance needs
  it before this job is useful, but this task does not depend on it.

## Session

- Created: f7eccb81-87c9-4fb4-a0c4-0fb7417d4a2f (2026-11-25)

## Results (required before done)

### How to validate (required before done)

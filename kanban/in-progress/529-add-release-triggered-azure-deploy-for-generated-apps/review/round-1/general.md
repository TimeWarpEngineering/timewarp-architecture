# Round 1 — general
**Date:** 2026-11-26
**Scope reviewed:** branch `task/529-add-release-triggered-azure-deploy-for-generated` vs `master`. App workflow source, `AppCiWorkflowShape`, the release-only scan that replaced the never-deploy scan, `tw-deploy` (SKILL.md, `references/release-deploy.md`, the Neon reference), and the dev-cli and AppHost comments named in the task.

## Summary

The generated app workflow (`.github/app-workflows/workflow.yml`, shipped as the app's `.github/workflows/workflow.yml`) gains a `deploy` job. It runs only on `release: published` or on `workflow_dispatch` when the `deploy` input is on, in the `Production` environment, with `azure/login@v3` over OIDC and no client secret. The job calls `dotnet run --file tools/dev-cli/dev.cs -- deploy --target $env:DEPLOY_TARGET --yes --no-browser`. The target comes from `DEPLOY_TARGET`, default `aca-neon`. Neon connection strings are copied from Actions secrets into `Parameters__` environment variables and are not written to a file or echoed. Classify and `ci` skip a deploy run; `ci-group` and `green-key` stay off because classify does not run. Push and pull request still publish and do not deploy. This repository's own workflow is unchanged. `tw-deploy` states that a merge never deploys and that a published release or a manual dispatch may, and `references/release-deploy.md` documents the one-time setup, including the federated subject `repo:<owner>/<repo>:environment:Production`.

The shape checker requires that job, its trigger, the Production environment, OIDC login, the one deploy command, and the CI skip, and it refuses `id-token` and any deploy, deprovision, or open outside the job. The old "nothing may ever deploy" scan now allows only that job and full-line comments, which the test states. That matches the task's contract change. Recorded `## Results` and `### How to validate` cover build, the two dev-cli test files, skill lint, template-smoke, version check, and audit. No gate was re-run.

GitHub's current release-event table sets `GITHUB_SHA` to the tagged commit and `GITHUB_REF` to `refs/tags/<tag_name>`. `actions/checkout@v7` checks that pair out, so a published release deploys the tagged commit. `inputs.deploy` is a boolean in the `inputs` context, so an unchecked dispatch stays on CI.

No issues.

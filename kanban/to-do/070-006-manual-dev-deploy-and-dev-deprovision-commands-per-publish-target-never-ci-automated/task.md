# Manual dev deploy and dev deprovision commands per publish target (never CI-automated)

Child of [[070-wire-aspire-publish-for-portable-deploy-compose-kubernetes]].

## Description

Deploying a generated app is a **deliberate operator action**, never a side effect of a merge.
Add `dev deploy` and `dev deprovision` to the template's dev CLI as thin wrappers over
`aspire deploy` / `aspire destroy`, target-aware through the existing `Publish:Target` switch
(`compose` | `kubernetes`, plus `aca` once 070-007 lands).

Reference implementation: the crunchit repo (`tools/dev-cli/endpoints/deploy-command.cs`,
`deprovision-command.cs`). There, both commands have been run successfully against Azure
Container Apps. **Do not copy crunchit's CI `deploy` job.** The template ships no workflow step
that deploys anywhere.

## Requirements

- `dev deploy [--target compose|kubernetes]` (default: the `Publish:Target` default)
  - Preflight: Aspire CLI ≥ 13.6 (reuse `services/aspire-publish.cs`). For kubernetes, Helm ≥ 4.2
    on PATH and a current kubectl context, printed before deploying. Ask for confirmation unless
    `--yes`.
  - Runs `aspire deploy --apphost … --environment Production -- --Publish:Target=<target>`
    (`--non-interactive` with `--yes`).
  - kubernetes: any kubectl context works. A local **kind** cluster is just a context. The skill
    documents the kind recipe: a local registry as `registry-endpoint` and ingress-nginx installed
    as cluster infrastructure. The command does not create clusters.
- `dev deprovision [--target …]` runs `aspire destroy`.
  - `aspire destroy` only knows deployments recorded under `~/.aspire/deployments` on the machine
    that deployed. When it has no record, say so and print the manual removal for that target
    (`docker compose down -v`, or `helm uninstall` plus deleting the PVC). Never fall back to a
    destructive command silently. Require confirmation (`--yes`), because it deletes data.
- Runtime neutrality: no hard-coded `docker`. Compose deploy honours `ASPIRE_CONTAINER_RUNTIME`.
- No CI job, workflow step or `dev workflow` mode calls `dev deploy`. CI keeps publish plus
  safety checks only (070-003 and 070-004).
- dev-cli tests cover argument parsing, preflight refusals and the no-record message, without a
  real deploy.
- Update `skills/tw-deploy` to say how to deploy (`dev deploy` and `dev deprovision`, operator-run,
  never automated) and give the kind recipe. Point the AppHost Design region at it.

## Checklist

- [ ] `dev deploy` with preflight, confirmation and target switch
- [ ] `dev deprovision` with the no-record guidance and confirmation
- [ ] dev-cli tests
- [ ] `tw-deploy` skill: manual deploy, kind recipe, never CI-automated
- [ ] Gates: `dev build` 0/0, dev-cli-tests, `ganda repo audit`, `dev template-smoke`

## Notes

- Workers never deploy, never start the maintainer's AppHost, and never touch clusters. A real
  deploy to kind or Azure is the maintainer's check after merge.
- Steve, 2026-10-07: "this should not be automated in architecture. `dev deploy` would be preferred."

## Session

- Created: 2026-10-07 (cockpit, from the 070-005 Azure decision)

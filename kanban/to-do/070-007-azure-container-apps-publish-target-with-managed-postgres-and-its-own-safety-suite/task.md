# Azure Container Apps publish target with managed Postgres and its own safety suite

Child of [[070-wire-aspire-publish-for-portable-deploy-compose-kubernetes]]. Depends on
[[070-006-manual-dev-deploy-and-dev-deprovision-commands-per-publish-target-never-ci-automated]].

## Description

The 070-005 decision (Steve, 2026-10-07) makes an **existing AKS cluster through the Kubernetes
target** the portable Azure default. This task adds **Azure Container Apps** (ACA) as a second,
Azure-only target. It fits small or idle apps (consumption billing, can scale to zero, built-in
HTTPS ingress) and is the target Steve already runs in production elsewhere.

Reference implementation: the crunchit repo AppHost (`AddAzureContainerAppEnvironment`,
`AddAzurePostgresFlexibleServer(...).WithPasswordAuthentication().RunAsContainer(...)`). Its
`dev deploy` and `dev deprovision` have deployed and destroyed successfully. **crunchit is
simpler**: it has one public `web-server` and no YARP, api or grpc. This template must keep its
multi-service topology.

## Requirements

- `Publish:Target=aca` (publish mode only; run mode unchanged). Add the `Aspire.Hosting.Azure.*`
  packages to CPM.
- **Only the YARP ingress is external** (`WithExternalHttpEndpoints` on YARP only). web, api and
  grpc are internal.
- **Aspire dashboard disabled** in the ACA environment. crunchit currently deploys it publicly.
  The safety suite must fail if it is present.
- Postgres: **Azure Database for PostgreSQL Flexible Server**, not a container on Azure Files.
  Run mode keeps the container (`RunAsContainer`). The password is a secret parameter, stored in
  Key Vault by Aspire.
- Migrations: decide and document how the idempotent SQL script, or the bundle, runs against
  Flexible Server. Do not use `EnsureCreated`.
- Entra and other secrets are parameters or secrets, never literals. No `UseMock`, no
  Development/Testing workload, no browser-log forwarding, no REPL.
- Safety suite `AcaPublish_Given_` over the generated Bicep/manifest, in-proc and with no Azure
  credentials. It covers the same rule set as Compose and Kubernetes, plus "no dashboard" and
  "only YARP external".
- `dev publish aca` and a CI publish check, with **no deploy in CI**.
- `dev deploy --target aca` and `dev deprovision --target aca`, extending 070-006.
  - Preflight: `az login`; subscription from `az account show` when `Azure__SubscriptionId` is unset.
  - Deprovision: when `aspire destroy` has no record, print `az group delete` and the purge of
    soft-deleted Key Vaults, behind confirmation.
- `skills/tw-deploy` gets an Azure Container Apps section covering when to choose it over AKS,
  the cost model, Flexible Server, and the deploy problems to expect:
  - the first deploy takes a long time;
  - Postgres can report "server is busy" until it is Ready, so retry;
  - soft-deleted Key Vault names must be purged before a redeploy can reuse them.

## Checklist

- [ ] ACA environment behind `Publish:Target=aca`; only YARP external; dashboard off
- [ ] Flexible Server in publish mode, container in run mode; migration path decided
- [ ] `AcaPublish_Given_` safety suite
- [ ] `dev publish aca` + CI publish check (no deploy)
- [ ] `dev deploy` / `dev deprovision` ACA support
- [ ] `tw-deploy` ACA section
- [ ] Gates: `dev build` 0/0, aspire-tests, `dev template-smoke`, `ganda repo audit`

## Notes

- Workers never deploy to Azure. The maintainer runs `dev deploy --target aca` after merge.
- Side finding for crunchit (not this repo): its Aspire dashboard is deployed publicly. It is
  worth checking there.

## Session

- Created: 2026-10-07 (cockpit, from the 070-005 Azure decision)

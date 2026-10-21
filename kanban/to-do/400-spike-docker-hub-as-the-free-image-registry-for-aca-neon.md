# Spike: Docker Hub as the free image registry for aca-neon

## Description

`aca-neon` aims for free tier, and Azure Container Registry Basic (about $5 a month) is its only
cost. Steve suggests Docker Hub as a free registry.

Known blocker from task 317: Aspire 13.6.2 refuses a non-ACR registry for an Azure Container Apps
environment, failing with "The container registry configured for the Azure Container App
Environment 'aca-env' is not an Azure Container Registry. Only Azure Container Registry resources
are supported." The environment pulls by managed identity only. That is why GHCR was rejected and
why `skills/tw-deploy/references/azure-container-apps-neon.md` documents ACR Basic as the one cost.

Azure Container Apps itself can pull from Docker Hub with a username and access token
(`configuration.registries` with a password secret). The question is whether the template can
use that through or around Aspire, without reconstructing Aspire internals.

## Checklist

- [ ] Check the latest Aspire (13.6.x and any newer release) for non-ACR registry support on Container Apps
- [ ] Try: Aspire publishes the Bicep, images are pushed to Docker Hub, and each container app gets a Docker Hub registry entry plus token secret through `PublishAsAzureContainerApp` customization; record exactly where it fails or works
- [ ] Docker Hub free plan limits: private repositories (web, api, grpc images; ingress uses a public image), pull rate limits for Container Apps cold starts, token scope; whether images would have to be public
- [ ] If it works: live proof on aca-neon (deploy, migrate, smoke 200), no ACR in the resource group, then deprovision; write the follow-up implementation task
- [ ] If it does not: record why in the aca-neon reference's "Why the registry stays ACR" section and close
- [ ] Recommendation for Steve: ACR Basic vs Docker Hub, with the trade-offs

## Session

- Created: c2bec172-f0eb-4648-9edb-30256e7756e3 (2026-10-21)

## Notes

- Origin: Steve 2026-10-21, "create a task to use Docker Image Registry I think that works and is free".
- Spike output is a finding and a recommendation; Steve decides before the template changes.

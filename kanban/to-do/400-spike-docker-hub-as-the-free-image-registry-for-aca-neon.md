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

## Research notes: container registry options

- ACA has a free tier; good for scale-to-zero serverless containers.
- ACR has no free tier: Basic ~ $5/month (~$0.17/day), billed while the registry exists. Practical: delete ACR between projects, recreate via CLI when needed.
- ACA can pull directly from public registries like Docker Hub (`docker.io/user/image`); public images need no ACR.
- Docker Hub free tier works for public images: anonymous pulls limited to 100 per 6 h (a free account raises it); small images push in 10-30 s, cached layers speed rebuilds; GitHub Actions can push to Docker Hub free from public repos.
- Aspire 13.1+: add a `ContainerRegistryResource` via `AddContainerRegistry("docker.io", ...)`, then `WithContainerRegistry` on the project resource; Aspire tags and pushes there.
- ACR is still preferred for production (managed identity wires credential-less pulls from ACA); Docker Hub works end to end for free/dev; Aspire local dev already pulls infra images (Redis, Postgres) from Docker Hub.
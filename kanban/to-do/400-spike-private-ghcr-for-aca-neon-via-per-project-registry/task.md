# Spike: private GHCR for aca-neon via Aspire's per-project registry

## Description

`aca-neon` aims for free tier, and Azure Container Registry Basic (about $5 a month) is its only
cost. A free private registry would remove it. GitHub Container Registry (ghcr.io) private
packages are the candidate: a template user keeps their images private.

Known blocker from task 317: Aspire 13.6.2 refuses a non-ACR registry set on the Azure Container
Apps **environment** ("The container registry configured for the Azure Container App Environment
'aca-env' is not an Azure Container Registry. Only Azure Container Registry resources are
supported."). The environment pulls by managed identity only.

Untested lead: research notes say Aspire 13.1+ has a **per-project** hook,
`AddContainerRegistry(...)` plus `WithContainerRegistry(...)` on each project resource, so Aspire
tags and pushes there. That is a different hook from the environment registry that threw. Steve
doubts it works; the spike settles it.

## Goal (implementer restatement)

`aca-neon` is meant to stay inside free tiers. Azure Container Registry Basic is the one monthly charge. This spike decides whether Aspire 13.6.2 can send the web, api, and grpc images to a private `ghcr.io` package through the per-project registry hook, and whether the Container Apps environment then stops creating that ACR.

The per-project hook is `AddContainerRegistry` plus `WithContainerRegistry` on each project. It is a different attachment from the environment registry that task 317 already saw throw. The type and method names come from the Aspire 13.6.2 assemblies, not from the research notes.

Acceptance:

- Those APIs are confirmed on Aspire.Hosting 13.6.2 (names and signatures).
- web, api, and grpc are pointed at `ghcr.io/<owner>/<image>`, and `aspire publish` for `aca-neon` is run. `aspire deploy` runs only if publish does not already show an ACR.
- If the environment still provisions an ACR when every project has its own registry, that is the result: there is no saving, and the walk stops.
- A ghcr pull is a `configuration.registries` entry with a token secret, not a managed identity. It is applied only through the `PublishAsAzureContainerApp` callback, and only if the ACR is actually gone.
- The GitHub token's scopes (`read:packages` to pull, `write:packages` to push), where it would live (AppHost user secret, copied by `dev deploy` the way the Neon strings are, never on the command line and never printed), and its expiry are recorded.
- A path that works gets a live aca-neon proof (deploy, migrate, smoke 200, a cold start after a long idle, no ACR in the group, then deprovision) and a follow-up implementation task. A path that fails is written into the aca-neon guide's "Why the registry stays ACR" section, with the Aspire version and the docs cited, so Steve can decide. The template is not switched to GHCR as part of this spike.

## Ruled out (Steve 2026-10-21)

- **Docker Hub:** private repositories are not free beyond one, and a template user would not
  publish their app images publicly.
- **Deploy, then delete ACR:** Container Apps pulls the image whenever a replica starts on a node
  without it cached: every new revision, scale-out, scale from zero, and platform maintenance. With
  min replicas 0 the first cold start after deleting ACR fails to pull and the app is down.

## Checklist

- [x] Confirm the per-project API exists in Aspire 13.6.2 with `dotnet-inspect` (type and method names, signatures), not from the notes
- [x] Wire web, api and grpc to `ghcr.io/<owner>/<image>`; `aspire publish` for aca-neon. `aspire deploy` and a GitHub token were skipped because the publish still contained ACR (next item)
- [x] Check whether the Container Apps environment still creates an ACR when every project has its own registry. If it does, there is no saving: record that and stop
- [x] Check how each container app pulls: the published `registries` entry is the ACR server plus a managed identity. `Username` / `PasswordSecretRef` exist on the provisioning type and were not wired through `PublishAsAzureContainerApp`, because the environment still provisions ACR
- [x] Token: scope (`read:packages` for pull, `write:packages` for push), where it lives (AppHost user secret, copied by `dev deploy` like the Neon strings, never on argv or printed), expiry
- [ ] If it works: live proof on aca-neon (deploy, migrate, smoke 200, a cold start after 10+ minutes idle still pulls), no ACR in the resource group, then deprovision; write the follow-up implementation task
- [x] If it does not: record exactly where it fails in the aca-neon reference's "Why the registry stays ACR" section and close
- [x] Recommendation for Steve, with the Aspire version and docs cited

## Session

- Created: c2bec172-f0eb-4648-9edb-30256e7756e3 (2026-10-21)
- Implementation: 2026-10-22. Per-project GHCR probe on Aspire.Hosting 13.6.2. `aspire publish` for aca-neon still wrote `aca-env-acr` at SKU Basic. Probe wiring removed. No token, no deploy.

## Notes

- Origin: Steve 2026-10-21, "create a task to use Docker Image Registry", then refocused: "forget the docker registry option ... If this works (and I have my doubts) then we could use GitHub private registry like we were trying."
- Spike output is a finding and a recommendation; Steve decides before the template changes.
- Deploys are operator-run, never CI (`tw-deploy`).

## Results

Aspire 13.6.2 can name a per-project `ghcr.io` push target. `AddAzureContainerAppEnvironment` still publishes ACR Basic, and each built container app still pulls with that registry's managed identity. Private GHCR does not remove the about-$5 ACR line. The template is unchanged aside from the AppHost design note that points at this finding. The operator guide's "Why the registry stays ACR" section is the record. No follow-up implementation task: the live-proof checklist item stays open because the condition (it works, and the resource group has no ACR) was not met. No GitHub token was created.

`dotnet-inspect` on `Aspire.Hosting` 13.6.2:

- `AddContainerRegistry(IDistributedApplicationBuilder, string, string, string?)` and the `ParameterResource` overload, both returning `IResourceBuilder<ContainerRegistryResource>`
- `WithContainerRegistry<TDestination, TContainerRegistry>(IResourceBuilder<TDestination>, IResourceBuilder<TContainerRegistry>)` where the registry type implements `IContainerRegistry`

The probe, in the `aca-neon` branch only, called `AddContainerRegistry("ghcr", "ghcr.io", "spike-owner")` and `WithContainerRegistry` on web, api, grpc, and the ingress, then `aspire publish` (CLI 13.6.1, AppHost `13.6.2+af07e33`). Publish succeeded. `aca-env-acr.bicep` is `Microsoft.ContainerRegistry/registries` with `sku.name` `Basic`. api, grpc, and web `registries` entries set `server` to `AZURE_CONTAINER_REGISTRY_ENDPOINT` and `identity` to `AZURE_CONTAINER_REGISTRY_MANAGED_IDENTITY_ID`. `Azure.Provisioning.AppContainers` 1.2.0 also has `Username` and `PasswordSecretRef` on `ContainerAppRegistryCredentials`; Aspire's `AddContainerRegistryParameters` sets neither. A `PublishAsAzureContainerApp` callback could overwrite that list and was not added, because the environment module would still provision the registry. The probe was deleted after publish.

GHCR auth, if a later Aspire release stopped provisioning ACR, would be a classic PAT (`read:packages` to pull, `write:packages` to push) in an AppHost user secret, copied by `dev deploy` the way the Neon strings are. Expiration is chosen at creation; an organization can cap it. Docs and the exact publish shape are in the guide.

### How to validate

Smoke:

```bash
dotnet run tools/dev-cli/dev.cs -- publish aca-neon
rg -n "Microsoft.ContainerRegistry|name: 'Basic'|registries:|identity:" artifacts/aspire-output/aca-neon -g '*.bicep'
```

Expect: `aca-env-acr.bicep` declares `Microsoft.ContainerRegistry/registries` with `sku.name` `Basic`. api-server, grpc-server, and web-server set `registries[].identity` to `aca_env_outputs_azure_container_registry_managed_identity_id`. No module sets `registries[].server` to `ghcr.io`. The AppHost has no `AddContainerRegistry("ghcr"` call. The guide section "Why the registry stays ACR" names Aspire 13.6.2, the `af07e33` sources, and the task 400 publish.

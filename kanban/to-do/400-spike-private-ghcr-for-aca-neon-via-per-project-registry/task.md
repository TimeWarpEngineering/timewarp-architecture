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
- The GitHub token's scopes (`read:packages` to pull, `write:packages` to push) and its expiry are recorded. A later Aspire release that stopped provisioning ACR would keep that token in an AppHost user secret, and `dev deploy` should copy it the way it copies the Neon strings: off the command line and never printed. The guide also says `dev deploy` does not forward a registry token today.
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
- Implement oracle: implemented by implementer-cursor (cursor, model grok-4.7-high), session not reported, max-turns uncapped (no --max-turns flag; bound 4), Done — 2026-10-21T23:52:47Z
- Review oracle round 1: claude-opus-5-5 (headless), effort 2, roster general, 2026-10-22. 0 bug, 1 suggestion, 1 nit, all open. Fix pass requested for M1 and M2. Artifacts: `review/review-framework.md`, `review/round-1/merged.md`.
- Review oracle: review by reviewer-claude (claude, model claude-opus-5-5), session not reported, max-turns 120 — 2026-10-21T23:55:16Z
- Fix pass (implement-fix-2): rewrote "Why the registry stays ACR" as present-tense Aspire 13.6.2 facts. Cited https://aspire.dev/app-host/container-registry/. The probe log stays in this file's Results. 2026-10-22.
- Review oracle round 2: claude-opus-5-5 (headless), effort 1, roster general, turn cap 80, 2026-10-22. It reviewed the fix delta `94b1b38f1..28369a335`. M1 and M2 are fixed. New M3 (suggestion) is open. Fix pass requested for M3. Artifacts: `review/round-2/general.md`, `review/round-2/merged.md`.
- Fix pass (implement-fix-3): the Token paragraph no longer says `dev deploy` copies a GHCR token. It says `dev deploy` does not forward a registry token, and that a later release with no ACR should keep the token in an AppHost user secret and copy it the way the Neon strings are copied. 2026-10-22.
- Review oracle round 3: claude-opus-5-5 (headless), effort 1, roster general, turn cap 80, 2026-10-22. It reviewed the fix delta `a53c41364..e2ab3757a`. M3 is fixed and there are no new findings. Disposition: clean. Artifacts: `review/round-3/general.md`, `review/round-3/merged.md`, `review/disposition.md`.

## Review

- Round 1: Blocked — open M1, M2
- Round 2: Blocked — open M3
- Round 3: Done
## Notes

- Origin: Steve 2026-10-21, "create a task to use Docker Image Registry", then refocused: "forget the docker registry option ... If this works (and I have my doubts) then we could use GitHub private registry like we were trying."
- Spike output is a finding and a recommendation; Steve decides before the template changes.
- Deploys are operator-run, never CI (`tw-deploy`).

## Results

Aspire 13.6.2 can name a per-project `ghcr.io` push target. `AddAzureContainerAppEnvironment` publishes ACR Basic, and each built container app pulls with that registry's managed identity. Private GHCR leaves the about-$5 ACR line in place. The template is unchanged aside from the AppHost design note that points at this finding. No follow-up implementation task: the live-proof checklist item stays open because the condition (it works, and the resource group has no ACR) was not met. No GitHub token was created.

The operator guide's "Why the registry stays ACR" section states the Aspire 13.6.2 facts: the API table, the default-ACR mechanics, the pull shape, the token scopes, the recommendation, and the revisit condition. The token's home is intended design: `dev deploy` does not forward a registry token, and a later release that publishes this environment with no ACR should keep the token in an AppHost user secret and copy it the way the Neon strings are copied. Task ids stay in parentheses (task 317, task 400). The run narrative stays here. [Container registry configuration](https://aspire.dev/app-host/container-registry/) is the cited page for the ACA `WithContainerRegistry(environment)` example. Round-1 ledger: `review/round-1/merged.md`. Round-2 ledger: `review/round-2/merged.md`.

Review disposition: **clean**. The review ran 3 rounds with roster general: effort 2 in round 1, effort 1 in rounds 2 and 3. Final counts (`review/round-3/merged.md`): 0 bugs, 2 suggestions (M1 and M3, both fixed), and 1 nit (M2, fixed), with 0 open and 0 wontfix. Paths: `review/review-framework.md`, `review/round-3/merged.md`, `review/disposition.md`. Round 3 verified that M3 is fixed in `e2ab3757a`. `rg -i "ghcr|github.*token" tools/dev-cli` is empty, which is why the guide does not claim a copy exists. This pass ran `./bin/dev lint-skills` with vally 0.19.0 ahead of the installed 0.6.0: 9 skills passed and the eval spec is valid. `ganda repo audit` passed 32 and failed 0. `https://aspire.dev/app-host/container-registry/` returned 200, titled "Container registry configuration", and contains `WithContainerRegistry(environment)`. The existing `artifacts/aspire-output/aca-neon` still has `aca-env-acr` at SKU Basic and managed-identity pulls; this pass did not republish.

`dotnet-inspect` on `Aspire.Hosting` 13.6.2:

- `AddContainerRegistry(IDistributedApplicationBuilder, string, string, string?)` and the `ParameterResource` overload, both returning `IResourceBuilder<ContainerRegistryResource>`
- `WithContainerRegistry<TDestination, TContainerRegistry>(IResourceBuilder<TDestination>, IResourceBuilder<TContainerRegistry>)` where the registry type implements `IContainerRegistry`

The probe, in the `aca-neon` branch only, called `AddContainerRegistry("ghcr", "ghcr.io", "spike-owner")` and `WithContainerRegistry` on web, api, grpc, and the ingress, then `aspire publish` (CLI 13.6.1, AppHost `13.6.2+af07e33`). Publish succeeded. `aca-env-acr.bicep` is `Microsoft.ContainerRegistry/registries` with `sku.name` `Basic`. api, grpc, and web `registries` entries set `server` to `AZURE_CONTAINER_REGISTRY_ENDPOINT` and `identity` to `AZURE_CONTAINER_REGISTRY_MANAGED_IDENTITY_ID`. `Azure.Provisioning.AppContainers` 1.2.0 also has `Username` and `PasswordSecretRef` on `ContainerAppRegistryCredentials`; Aspire's `AddContainerRegistryParameters` sets neither. A `PublishAsAzureContainerApp` callback could overwrite that list and was not added, because the environment module would still provision the registry. The probe was deleted after publish.

GHCR auth, if a later Aspire release stopped provisioning ACR, would be a classic PAT (`read:packages` to pull, `write:packages` to push) in an AppHost user secret. `dev deploy` should then copy it the way it copies the Neon strings. It does not forward a registry token today. Expiration is chosen at creation; an organization can cap it. Docs and the exact publish shape are in the guide.

### How to validate

Smoke:

```bash
rg -n "Task 400 published|No GitHub token was created|Steve decides|/tmp/task-400|was not run|was not added|did not add|No token was created|lives as an AppHost user secret" skills/tw-deploy/references/azure-container-apps-neon.md
rg -n "copies it" skills/tw-deploy/references/azure-container-apps-neon.md
rg -n "does not forward a registry token" skills/tw-deploy/references/azure-container-apps-neon.md
rg -n "should copy it" skills/tw-deploy/references/azure-container-apps-neon.md
rg -n "https://aspire.dev/app-host/container-registry/" skills/tw-deploy/references/azure-container-apps-neon.md
rg -i "ghcr|github.*token" tools/dev-cli
curl -fsS -o /dev/null -w "%{http_code}\n" https://aspire.dev/app-host/container-registry/
dotnet run tools/dev-cli/dev.cs -- publish aca-neon
rg -n "Microsoft.ContainerRegistry|name: 'Basic'|identity:" artifacts/aspire-output/aca-neon -g '*.bicep'
rg -n 'AddContainerRegistry\("ghcr"' source/container-apps/aspire/projects/aspire-app-host/program.cs
```

Expect: the first two `rg` commands print nothing. `does not forward a registry token` and `should copy it` each hit the Token paragraph once. The container-registry link `rg` hits the docs list and the ACA example sentence. `rg` over `tools/dev-cli` prints nothing. `curl` prints `200`; that page is titled "Container registry configuration" and its ACA example is `WithContainerRegistry(environment)`. `aca-env-acr.bicep` declares `Microsoft.ContainerRegistry/registries` with `sku.name` `Basic`. api-server, grpc-server, and web-server set `registries[].identity` to `aca_env_outputs_azure_container_registry_managed_identity_id`. No module sets `registries[].server` to `ghcr.io`. The AppHost `rg` prints nothing. The guide section names Aspire 13.6.2, the `af07e33` sources, and task 400.

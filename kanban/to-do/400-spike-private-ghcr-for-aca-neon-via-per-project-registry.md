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

## Ruled out (Steve 2026-10-21)

- **Docker Hub:** private repositories are not free beyond one, and a template user would not
  publish their app images publicly.
- **Deploy, then delete ACR:** Container Apps pulls the image whenever a replica starts on a node
  without it cached: every new revision, scale-out, scale from zero, and platform maintenance. With
  min replicas 0 the first cold start after deleting ACR fails to pull and the app is down.

## Checklist

- [ ] Confirm the per-project API exists in Aspire 13.6.2 with `dotnet-inspect` (type and method names, signatures), not from the notes
- [ ] Wire web, api and grpc to `ghcr.io/<owner>/<image>` with a GitHub token; `aspire publish` and `aspire deploy` for aca-neon
- [ ] Check whether the Container Apps environment still creates an ACR when every project has its own registry. If it does, there is no saving: record that and stop
- [ ] Check how each container app pulls from ghcr.io: a `configuration.registries` entry with a token secret (no managed identity). Wire it through `PublishAsAzureContainerApp` customization only, never by reconstructing Aspire internals
- [ ] Token: scope (`read:packages` for pull, `write:packages` for push), where it lives (AppHost user secret, copied by `dev deploy` like the Neon strings, never on argv or printed), expiry
- [ ] If it works: live proof on aca-neon (deploy, migrate, smoke 200, a cold start after 10+ minutes idle still pulls), no ACR in the resource group, then deprovision; write the follow-up implementation task
- [ ] If it does not: record exactly where it fails in the aca-neon reference's "Why the registry stays ACR" section and close
- [ ] Recommendation for Steve, with the Aspire version and docs cited

## Session

- Created: c2bec172-f0eb-4648-9edb-30256e7756e3 (2026-10-21)

## Notes

- Origin: Steve 2026-10-21, "create a task to use Docker Image Registry", then refocused: "forget the docker registry option ... If this works (and I have my doubts) then we could use GitHub private registry like we were trying."
- Spike output is a finding and a recommendation; Steve decides before the template changes.
- Deploys are operator-run, never CI (`tw-deploy`).

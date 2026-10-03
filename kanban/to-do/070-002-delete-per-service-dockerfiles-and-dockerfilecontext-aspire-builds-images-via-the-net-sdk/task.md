# Delete per-service Dockerfiles and DockerfileContext; Aspire builds images via the .NET SDK

## Description

Child of 070. Aspire publishes `AddProject` resources as container images using the .NET SDK
container build, so hand-written Dockerfiles are not needed. Leftovers in the repo:

- `source/container-apps/grpc/projects/grpc-server/Dockerfile`, which has `ARG dotnet_version=10.0`
- `<DockerfileContext>` in `api-server.csproj`, `grpc-server.csproj`, `web-server.csproj` and
  `yarp.csproj`
- any Visual Studio Docker tooling remnants that go with them, such as
  `DockerDefaultTargetOS`, `.dockerignore`, launchSettings `Docker` profiles, and the
  `Microsoft.VisualStudio.Azure.Containers.Tools.Targets` package, if present

**Cleanup round: land this before task 272** (.NET 11). 272's step 4 would otherwise bump the
grpc Dockerfile.

## Requirements

1. Confirm with evidence that nothing uses the Dockerfiles:
   - not `aspire publish`;
   - not CI (`.github/workflows/*`);
   - not `dev` commands;
   - not the template config;
   - not the yarp container setup.

   The yarp ingress uses the `mcr.microsoft.com/dotnet/nightly/yarp` image, so check that
   `yarp.csproj` is really dead weight there too.
2. Delete the Dockerfile(s), the `<DockerfileContext>` lines, and any matching VS container
   tooling remnants. Keep template flag regions valid.
3. If container-image settings are needed for SDK publish (`ContainerRepository`,
   `ContainerFamily`, base image), add them only where `aspire publish` needs them. Otherwise rely
   on Aspire defaults. Record the choice in the AppHost Design region.
4. Update task 272's checklist note (step 4, Dockerfile) so it no longer refers to a deleted file.
   Edit 272 on this branch.

## Checklist

- [ ] Evidence that nothing uses the Dockerfiles (recorded in Notes)
- [ ] Dockerfile(s), `<DockerfileContext>` and VS container remnants removed
- [ ] Any needed SDK container properties added, with the reason in the Design region
- [ ] 272's Dockerfile step updated
- [ ] Gates: `dev build` 0/0, `dev test`, `dev template-smoke`, `ganda repo audit`
- [ ] Do **not** start an AppHost
- [ ] Implementation review; host `open-pr`

## Session

- Created: 2026-10-03 (rewrite of 070)

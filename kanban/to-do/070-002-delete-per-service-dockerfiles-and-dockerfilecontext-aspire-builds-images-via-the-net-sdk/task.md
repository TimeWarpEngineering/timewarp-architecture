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

- [x] Evidence that nothing uses the Dockerfiles (recorded in Notes)
- [x] Dockerfile(s), `<DockerfileContext>` and VS container remnants removed
- [x] Any needed SDK container properties added, with the reason in the Design region (none needed; reason recorded)
- [x] 272's Dockerfile step updated
- [x] Gates: `dev build` 0/0, `dev test`, `dev template-smoke`, `ganda repo audit`
- [x] Do **not** start an AppHost
- [x] Implementation review (disposition: clean); host `open-pr`

## Notes

Evidence that nothing used the Dockerfile (searched with `git ls-files` and `grep -rni` for
`Dockerfile`, `DockerfileContext`, `DockerDefaultTargetOS`, `Containers.Tools`, `docker`):

- **Only one Dockerfile was tracked:** `source/container-apps/grpc/projects/grpc-server/Dockerfile`.
  There was no `.dockerignore`, no launchSettings `Docker` profile, and no
  `Microsoft.VisualStudio.Azure.Containers.Tools.Targets` reference.
- **`aspire publish`:** the AppHost uses `AddProject<…>` for api/grpc/web. Nothing calls
  `WithDockerfile`, `AddDockerfile` or `PublishAsDockerFile`. Aspire builds project images through
  the .NET SDK container build, so it never reads a Dockerfile. No script, CI step or `dev` command
  runs `aspire publish` or `aspire deploy`.
- **CI:** nothing in `.github/` mentions docker or a Dockerfile.
- **`dev` commands:** the only docker use is the `docker` CLI in `db nuke`/`db reset`, for volume
  and container cleanup. They do no image builds.
- **Template config:** `.template.config/` and `timewarp-templates/` never mention docker.
- **yarp:** the AppHost ingress is `AddYarp(...)`, the `mcr.microsoft.com/dotnet/nightly/yarp`
  container image. `yarp.csproj` is not an Aspire resource, but it is **not** dead weight. It is
  the standalone gateway (an alternative ingress mode, as the AppHost Design region describes),
  and `tests/common/timewarp-testing` references it for the in-proc YARP host
  (`yarp-integration-tests`). Only its `DockerfileContext` and `DockerDefaultTargetOS` were
  dead, so the project stays.
- **SDK container properties:** none were added. `Microsoft.NET.Sdk.Web` enables SDK container
  support by default, Aspire supplies the repository and tag for each resource, and the base
  image follows the TFM. The reason is recorded in the AppHost `program.cs` Design region.

## Results

- Deleted the grpc-server `Dockerfile`.
- Removed `<DockerDefaultTargetOS>` and `<DockerfileContext>` from `api-server.csproj`,
  `grpc-server.csproj`, `web-server.csproj` and `yarp.csproj`. These were not inside any template
  flag region.
- The AppHost `program.cs` Design region now records the container-image decision.
- Task 272's step 4, its Notes table row and its dependency-order item no longer mention a
  Dockerfile. They now say to check only for a `ContainerBaseImage` pin added later.
- Gates (all in this worktree): `dev build` 0 warnings / 0 errors; `dev test` passed;
  `dev template-smoke` succeeded; `ganda repo audit` passes all checks.

### Review disposition

- Rounds: 1; effort 1; roster: general
- Final counts: 0 bug / 0 suggestion / 0 nit (0 open, 0 fixed, 0 wontfix)
- Disposition: **clean**
- Artifacts: `review/review-framework.md`, `review/round-1/merged.md`, `review/disposition.md`

### How to validate

**Smoke:**

```bash
git ls-files | grep -iE 'dockerfile|dockerignore' | grep -v '^kanban/'
grep -rnE 'DockerfileContext|DockerDefaultTargetOS' source tests
./bin/dev build
./bin/dev template-smoke
```

**Expect:** both greps print nothing; `dev build` reports 0 warnings / 0 errors;
template-smoke ends with `Template smoke SUCCEEDED`.

## Session

- Created: 2026-10-03 (rewrite of 070)
- 2026-10-04: implemented (headless implementer); gates green
- 2026-10-04: implementation review (headless review oracle, Claude Opus 5.5, effort 1) — clean

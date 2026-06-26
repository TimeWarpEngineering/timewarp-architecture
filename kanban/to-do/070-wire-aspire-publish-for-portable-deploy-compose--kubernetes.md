# Wire Aspire publish for portable deploy (Docker Compose + Kubernetes)

Build-out follow-up to [[063-relocate-devops-deploy-infra-to-root]], which removed the
hand-rolled Azure-locked DevOps tree and locked in the strategy below.

## Goal

A **portable** deploy story driven entirely by the Aspire AppHost: deploy the same app to
**standalone hardware**, **on-prem Kubernetes**, or **Azure** with no Azure lock-in. The AppHost
(`source/container-apps/aspire/aspire-app-host/program.cs`) is the single source of truth; all
deployment artifacts are GENERATED via `aspire publish`.

Confirmed strategy (Aspire 13.4 first-party publishers — `Azure | Kubernetes | Docker Compose`):

| Target               | Publisher       | Artifact       | Run with                          |
|----------------------|-----------------|----------------|-----------------------------------|
| Standalone hardware  | Docker Compose  | `compose.yaml` | `docker compose up`               |
| On-prem K8s          | Kubernetes      | manifests      | `kubectl apply` (any cluster)     |
| Azure                | Kubernetes→AKS  | same manifests | `kubectl apply` / `aspire deploy` |

## Scope / Checklist

- [ ] Add publisher environment(s) to the AppHost — `AddDockerComposeEnvironment(...)` and
      `AddKubernetesEnvironment(...)` (confirm exact APIs/packages via aspire docs at build time).
      Add the matching `Aspire.Hosting.Docker` / `Aspire.Hosting.Kubernetes` packages to CPM +
      the app-host csproj (guard with template `#if` flags consistent with existing yarp/cosmos).
- [ ] Verify `aspire publish` emits `compose.yaml` and the K8s manifests; choose an output
      location — propose root **`devops/`** (kebab) as the home for generated artifacts + a README.
- [ ] Relocate `TimeWarp.Architecture/DevOps/Ports.md` → root `devops/` (the only surviving file
      from 063); update its content if Aspire owns port assignment.
- [ ] Decide the per-service `Dockerfile` question (api-server, grpc-server): if `aspire publish`
      builds images via the .NET SDK container target, DELETE the two `Dockerfile`s and the
      `<DockerfileContext>` lines in api/grpc/web/yarp csproj (option 2 from 047). Otherwise document
      why they stay.
- [ ] Add a CI path under existing `.github/workflows/` that runs `aspire publish` (and optionally
      `aspire deploy`) — coordinate with the existing workflows rather than re-introducing the old
      Azure-DevOps pipeline.
- [ ] Document the three deploy paths (standalone / on-prem / Azure) in a root `devops/README.md`.
- [ ] Confirm `dev build` stays green after AppHost package/API changes.

## Notes

- Template-flag aware: the AppHost uses `#if api/web/grpc/yarp/cosmosdb`. Publisher wiring and any
  Dockerfile removal must respect those flags so generated templates stay valid.
- Coordinates with [[061-migrate-remaining-ps1-scripts-to-dev-cli-endpoints]]: the old DevOps `.ps1`
  were deleted as obsolete under 063, so 061 does not port them.
- The legacy `TimeWarp.Architecture/` tree is otherwise dead (no projects/solution); after Ports.md
  moves, `TimeWarp.Architecture/DevOps/` can be removed entirely.

## Session

- Created: 2026-06-26 (build-out spun off from 063)

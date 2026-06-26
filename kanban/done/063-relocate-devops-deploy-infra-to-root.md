# Evaluate DevOps/ — likely obsoleted by Aspire (then relocate the remainder)

Spun out of [[047-migrate-timewarparchitecture-to-root]] (wrapper teardown).

## Why / decision

`TimeWarp.Architecture/DevOps/` (~75 files) is a **hand-rolled** deploy story: Bicep IaC,
hand-written Kubernetes manifests + per-resource `.ps1`, `Docker/BuildImages.ps1`, and the
`provision-build-deploy.ps1` (Bicep → BuildImages → kubectl) chain. Much of this **predates Aspire
and is likely obsolete** — Aspire now owns the publish/deploy model. So this task is
**evaluate-first, not relocate-first**: decide per-area whether Aspire replaces it; delete the
dead, only relocate (to root `devops/`) what genuinely survives.

## What Aspire provides today (recon 2026-06-24, aspire.dev docs)

- **Deploy to Azure Kubernetes Service (AKS)** — first-party path: provisions the AKS cluster,
  container registry, ingress, identity, and observability. Replaces the hand-written K8s
  manifests + nginx-ingress helm + ACR import scripts.
- **`aspire publish` + "Building custom deployment pipelines"** — builds container images from
  AppHost resources and integrates with CD. Replaces `Docker/BuildImages.ps1` (manual
  `docker build`/`push` per service) and likely the per-service `Dockerfile`s.
- **Azure provisioning + role assignments** from the AppHost — overlaps much of the Bicep IaC.
- Community **Aspir8 / `aspirate`** generates K8s manifests straight from the AppHost (if a
  manifest-based flow is still wanted, this replaces the hand-written tree).

## Scope

- [x] **Decide the deployment strategy** with the user — DECIDED (see Decision below).
- [x] Delete what Aspire obsoletes: Bicep tree, hand-written K8s manifests + per-resource
      `.ps1`, helm/ACR scripts, `BuildImages.ps1`, Azure-DevOps Pipelines, and the
      `provision-build-deploy`/`deprovision`/`rollout-restart`/`variables` `.ps1` chain. Also
      removed `Pulumi/` (placeholder stubs), `Overview.md` (documented the deleted flow), and
      `Model.mdj` (stale StarUML). Done across commits on 2026-06-26.
- [~] Relocate genuinely-surviving content → root `devops/`: only `Ports.md` survives (4 port
      mappings). Deferred to the build-out follow-up so it lands next to the generated artifacts.
- [~] Drop per-service `Dockerfile`s (api/grpc) + `<DockerfileContext>` csproj lines if superseded
      by `aspire publish` — deferred to the build-out follow-up (decide while wiring publishers).

## Decision (2026-06-26)

**Goal: a PORTABLE deploy story — deploy to on-prem K8s, Azure, OR standalone hardware — with
the Aspire AppHost as the single source of truth. No Azure lock-in.**

Strategy (confirmed against aspire.dev docs, Aspire 13.4 — first-party publishers for
`Azure | Kubernetes | Docker Compose`):

| Target               | Aspire publisher        | Artifact        | Run with                       |
|----------------------|-------------------------|-----------------|--------------------------------|
| Standalone hardware  | Docker Compose          | `compose.yaml`  | `docker compose up`            |
| On-prem K8s          | Kubernetes              | manifests       | `kubectl apply` (any cluster)  |
| Azure                | Kubernetes → AKS        | same manifests  | `kubectl apply` / `aspire deploy` |

- The AppHost (`source/container-apps/aspire/aspire-app-host/program.cs`) already models the full
  topology (api/grpc/web/yarp, CosmosDB w/ emulator, YARP ingress + routes, service discovery), so
  infrastructure is GENERATED, never hand-written again.
- On-prem K8s and Azure AKS share the SAME Kubernetes artifacts → portability is real.
- Aspir8 NOT required — Aspire's built-in publishers cover it.

**Outcome of THIS task:** evaluation + obsolete-deletion complete. The build-out (wiring publisher
environments, generating into root `devops/`, dropping Dockerfiles, relocating Ports.md) moves to a
dedicated follow-up task so 063 stays scoped to "evaluate & delete."

## Notes

- Coordinates with [[061-migrate-remaining-ps1-scripts-to-dev-cli-endpoints]]: if the DevOps `.ps1`
  are deleted-as-obsolete here, 061 doesn't port them. Settle the strategy before either touches them.
- Don't relocate-then-delete: evaluate first so we don't carefully port stale infra Aspire replaces.
- Tye-era Docker (compose, monolith dockerfile, RunDocker) was already removed under 047.

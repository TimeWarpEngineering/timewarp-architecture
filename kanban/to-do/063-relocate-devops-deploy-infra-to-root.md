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

- [ ] **Decide the deployment strategy** with the user: adopt `aspire publish` / AKS (and/or
      Aspir8) and DELETE the hand-rolled DevOps tree, vs. keep some of it. This is the gating call.
- [ ] Delete what Aspire obsoletes: hand-written K8s manifests + per-resource `.ps1`, helm/ACR
      scripts, `BuildImages.ps1`, the Bicep tree if AKS/azd provisioning replaces it, and
      `provision-build-deploy.ps1`.
- [ ] Relocate ONLY genuinely-surviving, non-Aspire content → root `devops/` (kebab), fixing the
      stale `Source/ContainerApps/...` paths and wrapper-root working dirs.
- [ ] If per-service `Dockerfile`s + `appsettings.Kubernetes_Docker.json` are superseded by
      `aspire publish`, drop them + the `<DockerfileContext>` csproj lines (this is option 2 from
      047's original scope question — revisit once the strategy is chosen).

## Notes

- Coordinates with [[061-migrate-remaining-ps1-scripts-to-dev-cli-endpoints]]: if the DevOps `.ps1`
  are deleted-as-obsolete here, 061 doesn't port them. Settle the strategy before either touches them.
- Don't relocate-then-delete: evaluate first so we don't carefully port stale infra Aspire replaces.
- Tye-era Docker (compose, monolith dockerfile, RunDocker) was already removed under 047.

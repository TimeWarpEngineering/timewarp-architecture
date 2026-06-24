# Relocate DevOps/ deploy infra to root

Spun out of [[047-migrate-timewarparchitecture-to-root]] (wrapper teardown).

## Why

`TimeWarp.Architecture/DevOps/` (~75 files) holds the deployment story the user chose to KEEP:
Bicep, Kubernetes manifests/scripts, `Docker/BuildImages.ps1`, and the top-level provision/
deprovision/rollout orchestration. It needs to move to a root `devops/` (kebab) tree, and its
many stale paths fixed.

## Scope

- [ ] Move `TimeWarp.Architecture/DevOps/**` → root `devops/`.
- [ ] Fix stale paths: scripts reference the pre-migration `Source/ContainerApps/...` layout and
      wrapper-root working dirs (e.g. `BuildImages.ps1` does `Push-Location $PSScriptRoot/../..`
      then `docker build -f .\source\container-apps\...` — wrong base dir post-migration).
      Per-service Dockerfiles are at `source/container-apps/*/Dockerfile` now.
- [ ] Repoint `provision-build-deploy.ps1` chain (Bicep → Docker BuildImages → Kubernetes deploy).
- [ ] `appsettings.Kubernetes_Docker.json` per service stays with the services; confirm the K8s
      manifests reference the right image names/paths.

## Notes

- The `.ps1` here are also in scope for [[061-migrate-remaining-ps1-scripts-to-dev-cli-endpoints]]
  (port to a `dev deploy …` group). **Coordinate:** either (a) relocate the folder here first then
  port in 061, or (b) port-and-delete in 061 and let this task cover only the non-ps1 manifests
  (yaml/bicep). Pick one to avoid double-moving.
- This is deployment infra — port carefully, do not delete.

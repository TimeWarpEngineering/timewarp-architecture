# Kubernetes Helm publish environment with production-safe outputs

## Description

Child of 070. Add the Kubernetes target. In Aspire 13.6 it publishes a **Helm chart**
(`aspire publish -o <dir>`), and `aspire deploy` runs `helm install/upgrade` against the current
kubectl context (Helm ≥ 4.2). Reference: aspire.dev `deploy-to-kubernetes-clusters`,
`kubernetes-integration`, `deployment/kubernetes/persistent-volumes`, `helm-charts`.

## Requirements

1. **Package and environment.** Add `Aspire.Hosting.Kubernetes` (13.6.x) and
   `builder.AddKubernetesEnvironment("k8s")`.
   - Use `WithHelm(...)` for namespace, release name and chart version.
   - Configure the registry with `AddContainerRegistry` + `WithContainerRegistry`, which is
     preview (`ASPIRECOMPUTE003`, suppressed narrowly, with a reason). Supply the registry as a
     parameter.
   - It must be publish-only; run mode is unchanged and flags stay valid.
2. **Production-safe chart**, verified by a test over the generated chart:
   - no mock auth;
   - no browser-log forwarding;
   - no dashboard REPL;
   - secrets land in `secrets.<resource>`, not ConfigMaps;
   - only the ingress has an externally reachable Service.
3. **Postgres.** Use a StatefulSet with a persistent volume (Aspire's persistent-volume
   guidance), a secret password parameter, and a defined migration step (bundle/script from
   task 155, as a Job or a documented manual step).
4. **Ingress decision** (carried from retired 070-001):
   - (a) A cluster ingress controller (for example ingress-nginx via `AddHelmChart`) forwards to
     the YARP ingress.
   - (b) YARP is bypassed in K8s, and the controller routes per service using the generated
     `WebServerApiRoutePrefixes` (task 107).

   Pick one and record it in the AppHost Design region.
5. Use `PublishAsKubernetesService` only where needed (replicas, probes); defaults are otherwise
   fine.
6. Extend the CI publish check from 070-003 to also publish the Helm chart and run its safety
   test. If Helm is available on the runner, also run `helm lint`.

## Checklist

- [x] `Aspire.Hosting.Kubernetes` + `AddKubernetesEnvironment` + `WithHelm` + registry parameter
- [x] Production-safety test over the generated chart
- [x] Postgres StatefulSet/PVC, secret parameter and migration step
- [x] Ingress decision recorded and implemented
- [x] CI publish check covers the Helm chart (+ `helm lint` if available)
- [x] Gates: `dev build` 0/0, `dev test`, `dev template-smoke`, `ganda repo audit`
- [x] Do **not** start an AppHost or deploy to any cluster
- [x] Implementation review (disposition: clean)
- [ ] Host `open-pr`

## Notes

- Depends on 070-003 for the CI publish-check scaffolding.
- Maintainer check after merge: `aspire deploy` to a local kind cluster with a local registry,
  then confirm the app is reachable through the chosen ingress.

## Session

- Created: 2026-10-03 (rewrite of 070)
- 2026-10-06 implement (ganda task work, claude): Kubernetes target, chart safety suite, `dev publish
  kubernetes`, CI step. Found and fixed the EF bundle-in-chart defect (Helm 5 MiB file limit).
- 2026-10-06 review (ganda task work, claude review oracle; effort 3, roster: general subagent):
  2 rounds, disposition clean.

## Results

**Shipped.**

- `Aspire.Hosting.Kubernetes` **13.6.0-preview.1.26479.8** (no stable 13.6 release; same train as
  the EF Core preview pin). `AddKubernetesEnvironment("k8s")` + `WithHelm` (namespace and release
  name parameters, chart-version parameter default `1.0.0`) + dashboard off.
  `AddContainerRegistry` (registry-endpoint / registry-repository parameters) +
  `WithContainerRegistry` with `ASPIRECOMPUTE003` suppressed around those two calls only.
- **Target switch:** Aspire assigns each compute resource to exactly one compute environment, so
  Compose and Kubernetes cannot both publish one model. `Publish:Target` (`compose` default |
  `kubernetes`; anything else throws) is read in publish mode only — run mode is unchanged.
  `aspire publish -- --Publish:Target=kubernetes` emits the chart.
- **Ingress decision (a):** a cluster `Ingress` (`cluster-ingress`, class = `ingress-class`
  parameter, default `nginx`) whose default backend is the YARP ingress. YARP keeps the routing
  table, so it is the same in run mode, Compose and Kubernetes. The controller is cluster
  infrastructure and the chart does not install it. Rationale is in the AppHost Design region.
- **Postgres:** the `postgres-data` volume binds by name to a Kubernetes persistent volume
  (`postgres-storage-capacity` parameter, default 10Gi). This renders a single-replica
  StatefulSet with a `postgres-data` PVC. The password is a Secret. Migrations are a documented
  manual step: the idempotent SQL script through `kubectl exec … psql` (exact command in the
  Design region).
- **Defect found + fixed:** the EF migration **bundle** (~100 MB) was published into the chart
  directory, and Helm refuses to load any chart file over 5 MiB. That broke `helm lint`, and
  `aspire deploy` would have failed the same way. The bundle is now published for Compose only.
  `Publish_Should_StayLoadableByHelm` guards this even when Helm is not installed.
- `PublishAsKubernetesService` is not used. The defaults (1 replica, Aspire's fsGroup) are already
  correct.
- aspire-tests `KubernetesPublish_Given_` (9 facts): only the Ingress → YARP is reachable (all
  Services are ClusterIP, no hostPort); no dashboard; no `UseMock`; no Development/Testing
  workload; secret-shaped keys only in Secrets, and `values.yaml` `secrets.*` defaults are empty;
  postgres runs as a StatefulSet on a PVC; chart files stay ≤ 5 MiB; run mode has no Kubernetes
  wiring; an unknown target is rejected. A mutation check (LoadBalancer service, password in a
  ConfigMap, Development env) failed 3 of the facts, as expected.
- `dev publish kubernetes` (new) and `dev publish compose` share `services/aspire-publish.cs`.
  `helm lint` runs when `helm` is on PATH; ubuntu runners have it. `dev workflow` PR/merge now runs
  both publish steps, and workflow.yml uploads the chart directory.

**Gates:** `dev build` 0/0 · `dev test` all suites passed · `dev template-smoke` SUCCEEDED ·
`ganda repo audit` passes · `dev check-version` passes. `helm lint` passed using a throwaway Helm
v3.19 binary (not installed on this machine). No `dev run` and no cluster deploy. aspire-tests
boots its usual test AppHosts, and the publish facts run the publish pipeline in-proc only.

### How to validate

**Smoke:**

```bash
dev publish kubernetes          # aspire publish -- --Publish:Target=kubernetes + safety suite (+ helm lint if helm on PATH)
helm lint artifacts/aspire-output/kubernetes
helm template t artifacts/aspire-output/kubernetes | grep -E '^kind:|type: |ingressClassName'
dev publish compose             # unchanged Compose target still passes
```

**Expect:** `KubernetesPublish_Given_` reports 9/9 passed and the run ends with "Helm chart is
production-safe". `helm lint` reports 0 charts failed. The rendered chart contains Deployments for
api/grpc/web-server and the ingress, a `postgres-statefulset` StatefulSet, a `postgres-data` PVC,
`*-secrets` Secrets, only `ClusterIP` Services, and one Ingress (`ingressClassName: nginx`) whose
default backend is `ingress-service`. `artifacts/aspire-output/kubernetes/efmigrations/` contains
only `web-migrations.sql`. `dev publish compose` still passes 7/7.

**Maintainer follow-up (not done here):** run `aspire deploy -- --Publish:Target=kubernetes` against
a local kind cluster with a local registry and an ingress controller installed. Then confirm the
app is reachable through `cluster-ingress` and apply the migration script.

### Review disposition

- **Rounds:** 2 (round 1 general review; round 2 re-verification of the fixes). Effort 3, roster:
  general.
- **Final counts:** bug 0 · suggestion 2 fixed · nit 3 fixed · 0 open · 0 wontfix.
- **Disposition: clean.** Fixes:
  - M1: stale StatefulSet/volumeClaimTemplate comment.
  - M2: an `OptionalMapping` helper for values.yaml sections that a flag combination may not emit.
  - M3: the Design region documents the two postgres password keys that must match under plain helm.
  - M4: the Design region says the parameters are baked in at publish time and shows the
    `aspire deploy` switch.
  - M5: the unknown-target test asserts on the exception message.
- **Gates after the fixes:** `dev build` 0/0; `KubernetesPublish_Given_` 9/9.
- **Artifacts:** `review/review-framework.md`, `review/round-1/{general,merged}.md`,
  `review/round-2/{general,merged}.md`, `review/disposition.md`.

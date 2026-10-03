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

- [ ] `Aspire.Hosting.Kubernetes` + `AddKubernetesEnvironment` + `WithHelm` + registry parameter
- [ ] Production-safety test over the generated chart
- [ ] Postgres StatefulSet/PVC, secret parameter and migration step
- [ ] Ingress decision recorded and implemented
- [ ] CI publish check covers the Helm chart (+ `helm lint` if available)
- [ ] Gates: `dev build` 0/0, `dev test`, `dev template-smoke`, `ganda repo audit`
- [ ] Do **not** start an AppHost or deploy to any cluster
- [ ] Implementation review; host `open-pr`

## Notes

- Depends on 070-003 for the CI publish-check scaffolding.
- Maintainer check after merge: `aspire deploy` to a local kind cluster with a local registry,
  then confirm the app is reachable through the chosen ingress.

## Session

- Created: 2026-10-03 (rewrite of 070)

---
name: tw-deploy
description: "**TIMEWARP SKILL** — deploy a generated app from its Aspire AppHost: the target matrix (Docker Compose, Kubernetes/Helm, Azure), `aspire publish` / `aspire deploy` per target, production-safety rules, secrets and parameters, Postgres migrations per target, the ingress topology, and container-runtime neutrality. Invoke before deploying, before adding a publish target, or before touching publish-mode wiring in the AppHost. WHEN: deploy the app, aspire publish, aspire deploy, docker compose, Helm chart, Kubernetes, AKS, Azure, production secrets, run migrations in production, ingress controller, Podman."
when-to-use: deploy, deployment, aspire publish, aspire deploy, dev publish, compose.yaml, docker compose, Helm, helm install, Kubernetes, kubectl, AKS, Azure, Azure Container Apps, Publish:Target, production safety, secrets, parameters, .env, values.yaml, migrations in production, ingress controller, container runtime, Podman, ASPIRE_CONTAINER_RUNTIME
---

# Deploy (Aspire publish targets)

The AppHost (`source/container-apps/aspire/projects/aspire-app-host/program.cs`) is the single
source of truth for deployment. Every deployment artifact — `docker-compose.yaml`, `.env`, the
Helm chart, the migration script — is **generated** by `aspire publish` / `aspire deploy`. There
are no hand-written Dockerfiles, manifests, or charts, and there is no `devops/` tree. The
AppHost Design region holds the per-line reasoning; this skill is the operator's map.

## Detection — when to invoke

| Signal | Where |
|--------|-------|
| Deploying the app anywhere other than `dev run` | any target below |
| Adding or changing a publish target / compute environment | AppHost `program.cs` |
| Adding a setting that must differ in production | parameters, not literals |
| Touching code guarded by `IsPublishMode` / `IsRunMode` | AppHost `program.cs` |
| Adding a schema migration that must reach production | migrations section |

## Target matrix

| Target | AppHost environment | `Publish:Target` | Artifact | Run with |
|--------|---------------------|------------------|----------|----------|
| Standalone hardware | `AddDockerComposeEnvironment("compose")` | `compose` (default) | `docker-compose.yaml` + `.env` + `efmigrations/` | `aspire deploy`, or `docker compose up` / `podman compose up` |
| Kubernetes (on-prem, any distribution) | `AddKubernetesEnvironment("k8s")` + `WithHelm` | `kubernetes` | Helm chart directory | `aspire deploy`, or `helm upgrade --install` |
| Azure | an existing AKS cluster is a Kubernetes target | `kubernetes` | Helm chart | `aspire deploy` against the AKS kubectl context |

**One target per publish.** Aspire assigns every compute resource to exactly one compute
environment, so Compose and Kubernetes are alternatives selected by configuration, never both in
one model. `Publish:Target` is read in publish mode only; any other value throws. Run mode
(`dev run`) always ignores it.

## Publishing and deploying

`aspire publish` only writes files — no image build, no deploy, no container runtime needed.
`aspire deploy` builds images through the detected container runtime and applies them.

```bash
# Compose (default target)
dev publish compose                         # publish to artifacts/aspire-output/compose + safety suite
aspire publish --apphost <apphost.csproj> --output-path out/compose
aspire deploy  --apphost <apphost.csproj>   # fills .env, builds images, brings the stack up

# Kubernetes (Helm chart)
dev publish kubernetes                      # publish to artifacts/aspire-output/kubernetes + safety suite + helm lint
aspire publish --apphost <apphost.csproj> --output-path out/k8s -- --Publish:Target=kubernetes
aspire deploy  --apphost <apphost.csproj> -- --Publish:Target=kubernetes   # helm upgrade --install, current kubectl context
```

- **Kubernetes prerequisites:** Helm ≥ 4.2, a kubectl context pointing at the cluster, a
  container registry the cluster can pull from (`registry-endpoint` + `registry-repository`
  parameters), and an ingress controller already installed in the cluster.
- **Generated output is never committed.** `dev publish` writes under `artifacts/` (git-ignored);
  CI uploads the output as a workflow artifact. Regenerate, do not hand-edit.
- **`dev publish <target>` is the gate.** It runs the target's production-safety suite in
  aspire-tests against the generated files. Run it after any change to publish-mode wiring.

## Production-safety rules

These never appear in published output. Each is enforced by the target's aspire-tests suite —
extend the suite when you add a new publish-mode branch.

| Never ships | Why | How it is kept out |
|-------------|-----|--------------------|
| Mock authentication (`Authentication__UseMock`) | it signs requests in as a fake principal | forwarded in run mode only; the server also fail-closes outside Development/Testing |
| Browser-log forwarding | ships client telemetry to a dev dashboard | Development-only; publish runs as Production |
| Dashboard Postgres REPL (`WithRepl`) | an authenticated psql shell for anyone with dashboard access | Development-only |
| Aspire dashboard in the deployment | a second, unauthenticated UI and port | `WithDashboard(enabled: false)` on every environment — point `OTEL_EXPORTER_OTLP_ENDPOINT` at your own collector |
| Any host port / externally reachable service besides the ingress | every extra exposure is an unaudited entry point | Compose: only the ingress publishes a port (`ingress-port`). Kubernetes: every Service is `ClusterIP`; the cluster Ingress is the only way in |
| Secret literals | literals end up in images, repos and logs | secrets are parameters (below) |

Rules for new code:

- Gate publish-only behavior on `builder.ExecutionContext.IsPublishMode` / `IsRunMode`, **never on a
  template flag** — every flag combination must publish.
- Anything dev-convenient (REPL, mock auth, log forwarding, dashboard commands) is run-mode or
  Development-only. Publish runs as Production.
- Without the `yarp` flag there is no ingress: the published output exposes nothing, and the
  operator adds their own edge.

## Secrets and parameters

Every value that differs per deployment is an `AddParameter`, never a literal. Secrets are
`AddParameter(name, secret: true)` (or a generated secret such as Postgres' password).

| Target | Where parameter values live |
|--------|-----------------------------|
| Compose | `.env` beside `docker-compose.yaml`; `aspire deploy` / `aspire do prepare-compose` fill it |
| Kubernetes | `values.yaml`: secrets under `secrets.<resource>` (rendered into `<resource>-secrets` Secret objects, empty defaults), never ConfigMaps |

- Supply values non-interactively with `Parameters__<name>` environment variables or AppHost
  configuration/user secrets; interactive `aspire deploy` prompts for the rest. CI must supply all
  of them.
- Parameters such as `ingress-class`, `postgres-storage-capacity` and `helm-chart-version` are
  baked into the chart at publish time; change them by re-publishing, not `helm --set`.
- Under a plain `helm install` (no `aspire deploy`), the Postgres password appears under two keys
  — `secrets.postgres.postgres_password` and `secrets.web_server.postgres_password` — and both must
  carry the same value. `aspire deploy` fills both from the one parameter.
- Entra settings (`entra-enabled`, `entra-tenant-id`, `entra-client-id`, `entra-client-secret`,
  `entra-public-origin`) are parameters with Entra off by default; a passkey-only deployment needs
  none of them.

## Postgres and migrations per target

Migrations are explicit in every deployed environment: the AppHost never auto-migrates a
deployment. `web-server` does not migrate at startup and tolerates a not-yet-migrated database.
The published idempotent SQL script (`efmigrations/web-migrations.sql`) is safe to re-run.

| Target | Storage | Apply migrations |
|--------|---------|------------------|
| Compose | named volume `postgres-data`; `POSTGRES_DB` creates the database on first start | `docker compose exec -T postgres sh -c 'PGPASSWORD="$POSTGRES_PASSWORD" psql -U postgres -d postgres-db -v ON_ERROR_STOP=1' < efmigrations/web-migrations.sql` |
| Kubernetes | `postgres-data` PersistentVolumeClaim (`postgres-storage-capacity`, default 10Gi), single-replica StatefulSet | `kubectl exec -i -n <namespace> statefulset/postgres-statefulset -- sh -c 'PGPASSWORD="$POSTGRES_PASSWORD" psql -U postgres -d postgres-db -v ON_ERROR_STOP=1' < efmigrations/web-migrations.sql` |

- Run the script after the database is up and before (or right after) the app starts serving.
- Compose also publishes the self-contained migration bundle; it is a host binary, not an image,
  so the SQL script is the default path. The Helm chart carries the script only — Helm rejects
  chart files over 5 MiB.
- `-T` / `-i` with `PGPASSWORD` from the container's own environment is required: the image
  enforces password auth even on the in-container socket, and a piped session cannot prompt.

## Ingress topology

All public traffic enters through the **YARP ingress**, which owns the routing table (generated
web `/api` prefixes, the api catch-all, `/grpc` prefix strip, original-Host forwarding for web
routes). The routing is therefore identical in run mode, Compose and Kubernetes.

- **Compose:** the ingress is the only service with a host port. Put TLS in front of it (your
  reverse proxy or load balancer).
- **Kubernetes:** the chart carries one `networking.k8s.io` Ingress (`cluster-ingress`) whose
  default backend is the YARP ingress' http endpoint. TLS terminates at the cluster's ingress
  controller. The class is the `ingress-class` parameter (default `nginx`).
- **The chart does not install an ingress controller.** A controller is cluster-scoped
  infrastructure; an app chart that installs one collides with every other release. Install one
  per cluster.
- **Do not replace YARP with per-service controller routes.** That forks the routing table into a
  second implementation per target and loses original-Host forwarding and the `/grpc` prefix strip,
  which every controller vendor expresses differently.
- Behind any proxy that terminates TLS, set `Authentication:Entra:PublicOrigin` on web-server
  explicitly when Entra is on; it is not derived from the ingress URL.

## Azure

An existing **AKS** cluster is a Kubernetes target: point the kubectl context at it, use the
cluster's registry (for example an Azure Container Registry) as `registry-endpoint`, choose the
controller's class as `ingress-class`, and run the Kubernetes commands above. Nothing in the
AppHost is Azure-specific, so the same chart runs on any cluster.

Aspire's Azure-provisioning environments (`AddAzureKubernetesEnvironment`,
`AddAzureContainerAppEnvironment`) are separate compute environments with their own artifacts
(Bicep plus provisioning). Adding one is a new `Publish:Target` value with its own
production-safety suite — follow the same pattern as the existing targets, never a second
environment beside them.

## Container-runtime neutrality

- Generated Compose output is a standard compose file: Docker Compose, Podman Compose, or any
  compliant runtime runs it.
- Aspire picks the runtime it detects; `ASPIRE_CONTAINER_RUNTIME` overrides it (`docker`,
  `podman`). Tooling must go through Aspire's runtime selection — **never hard-code the `docker`
  CLI** in scripts or dev commands.
- `aspire publish` needs no runtime at all, so publish gates run anywhere; only `aspire deploy`
  and image builds need one. New runtimes (for example WSL containers) become usable when Aspire
  supports them, with no AppHost change.

## Related

- AppHost `program.cs` Design region — per-decision reasoning behind every rule above.
- aspire-tests `compose-publish-tests` / `kubernetes-publish-tests` — the production-safety suites.

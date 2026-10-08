---
name: tw-deploy
description: "**TIMEWARP SKILL** — deploy a generated app from its Aspire AppHost: the target matrix (Docker Compose, Kubernetes/Helm, Azure: AKS or Azure Container Apps), `aspire publish` / `aspire deploy` per target, operator-run `dev deploy` / `dev deprovision` (never CI), a local kind recipe, production-safety rules, secrets and parameters, Postgres migrations per target, the ingress topology, and container-runtime neutrality. Invoke before deploying, before adding a publish target, or before touching publish-mode wiring in the AppHost. WHEN: deploy the app, dev deploy, dev deprovision, tear down a deployment, kind cluster, aspire publish, aspire deploy, docker compose, Helm chart, Kubernetes, AKS, Azure, Azure Container Apps, ACA, Flexible Server, Key Vault purge, production secrets, run migrations in production, ingress controller, Podman."
when-to-use: deploy, deployment, dev deploy, dev deprovision, deprovision, aspire destroy, kind, local registry, aspire publish, aspire deploy, dev publish, compose.yaml, docker compose, Helm, helm install, Kubernetes, kubectl, AKS, Azure, Azure Container Apps, ACA, Bicep, Flexible Server, az login, Key Vault, Publish:Target, production safety, secrets, parameters, .env, values.yaml, migrations in production, ingress controller, container runtime, Podman, ASPIRE_CONTAINER_RUNTIME
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
| Azure (shared cluster, portable default) | an existing AKS cluster is a Kubernetes target | `kubernetes` | Helm chart | `aspire deploy` against the AKS kubectl context |
| Azure Container Apps (Azure-only) | `AddAzureContainerAppEnvironment("aca-env")` | `aca` | Bicep (`main.bicep` + one module per resource) + `efmigrations/` | `aspire deploy` into the `az login` subscription |

**One target per publish.** Aspire assigns every compute resource to exactly one compute
environment, so Compose, Kubernetes and Azure Container Apps are alternatives selected by configuration, never both in
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

# Azure Container Apps (Bicep) — publishing needs no Azure credentials
dev publish aca                             # publish to artifacts/aspire-output/aca + safety suite
aspire publish --apphost <apphost.csproj> --output-path out/aca -- --Publish:Target=aca
aspire deploy  --apphost <apphost.csproj> -- --Publish:Target=aca   # provisions + pushes images, az CLI credential
```

- **Kubernetes prerequisites:** Helm ≥ 4.2, a kubectl context pointing at the cluster, a
  container registry the cluster can pull from (`registry-endpoint` + `registry-repository`
  parameters), and an ingress controller already installed in the cluster.
- **Generated output is never committed.** `dev publish` writes under `artifacts/` (git-ignored);
  CI uploads the output as a workflow artifact. Regenerate, do not hand-edit.
- **`dev publish <target>` is the gate.** It runs the target's production-safety suite in
  aspire-tests against the generated files. Run it after any change to publish-mode wiring.
- **Deploying is a deliberate, operator-run action — never automated in CI.** CI publishes and
  runs the production-safety suites only; it never runs `aspire deploy`, `helm upgrade` or
  `docker compose up` against a real environment. A deploy is run by an operator who has chosen
  the target, the context and the parameter values, with `dev deploy` (below).

## Deploying: `dev deploy` and `dev deprovision`

Both verbs are thin, operator-run wrappers over `aspire deploy` / `aspire destroy` for one
`Publish:Target`. **No CI job, workflow step or `dev workflow` mode calls them, and none may.** A
merge never deploys.

```bash
dev deploy                                  # compose (the Publish:Target default): preflight, plan, prompt
dev deploy --target kubernetes              # Helm chart to the CURRENT kubectl context
dev deploy --target aca                     # Azure Container Apps in the az CLI's subscription (az login first)
dev deploy --target compose --yes           # no prompt: aspire deploy --non-interactive

dev deprovision --target kubernetes         # preflight, then aspire destroy asks before deleting
dev deprovision --target kubernetes --yes   # aspire destroy --yes --non-interactive — deletes the deployment and its data
dev deprovision --target aca                # aspire destroy, then purge the soft-deleted Key Vault
```

- **`dev deploy`** runs `aspire deploy --apphost <csproj> --environment Production --
  --Publish:Target=<target>`. Preflight: Aspire CLI ≥ 13.6; for kubernetes, Helm ≥ 4.2 on PATH and
  a current kubectl context, which is printed before anything happens — check it. It then asks for
  confirmation; `--yes` skips the prompt and adds `--non-interactive`, so every parameter must
  already have a value (interactive runs prompt for missing ones, and Aspire remembers them in
  the deployment state). Without a terminal and without `--yes` it refuses. For aca the preflight
  requires `az login` and prints the subscription and where it came from, in this order: the
  `Azure__SubscriptionId` environment variable, then the AppHost user secret `Azure:SubscriptionId`
  (read with `dotnet user-secrets list --project <apphost>`), then the one `az account show`
  reports. Only that last fallback is passed to `aspire deploy` as `Azure__SubscriptionId`, so the
  az CLI never overrides a subscription you pinned. A subscription Aspire remembered in its own
  deployment state is not checked; pin it in user secrets if `az account` may point elsewhere.
  Location and resource group come from `Azure__Location` / `Azure__ResourceGroup`, or Aspire's prompt.
- **Any kubectl context works** — AKS, an on-prem cluster, or a local kind cluster. `dev deploy`
  never creates a cluster, installs an ingress controller or switches context.
- **`dev deprovision`** runs `aspire destroy` for the same target after the same preflight (for
  kubernetes it prints the kubectl context first). It deletes the deployment's data (Compose volumes,
  the Kubernetes postgres claim), so Aspire asks for confirmation; `--yes` skips the prompt. Without
  a terminal and without `--yes` it refuses.
- **`aspire destroy` is local.** It only knows deployments that `aspire deploy` recorded on the
  machine — and from the checkout — that deployed. When it fails, `dev deprovision` prints the manual
  removal for the target and exits with Aspire's exit code: `<runtime> compose down --volumes` for
  the Compose project (`<runtime> compose ls` finds it), or `helm uninstall <release> --namespace
  <namespace>` (the AppHost's `helm-release-name` and `k8s-namespace` parameters) plus deleting the
  `postgres-data` PersistentVolumeClaim, or for aca `az group delete --name <resource-group>` (az
  asks for confirmation) plus the Key Vault purge below. It never falls back to a destructive command
  on its own.
- **Runtime:** Compose deploy and teardown go through Aspire, so `ASPIRE_CONTAINER_RUNTIME` picks
  the runtime; the verbs call no container CLI themselves.
- **Without the `dev` CLI** (it lives in the template's source repository), run the same commands
  the verbs wrap, and do the preflight yourself (`helm version`, `kubectl config current-context`):

  ```bash
  aspire deploy  --apphost <apphost.csproj> --environment Production -- --Publish:Target=<target>
  aspire destroy --apphost <apphost.csproj> --environment Production -- --Publish:Target=<target>
  ```

  When `aspire destroy` fails or reports nothing to destroy, nothing was removed; use the manual
  removal above.

### Local Kubernetes with kind

A kind cluster is just another kubectl context. Give it a local registry the nodes can pull from,
install ingress-nginx once as cluster infrastructure, then deploy. (kind also runs on Podman with
`KIND_EXPERIMENTAL_PROVIDER=podman`; substitute `podman` for `docker` below.)

```bash
# 1. Local registry, reachable from the host as localhost:5001
docker run -d --restart=always -p 127.0.0.1:5001:5000 --network bridge --name kind-registry registry:2

# 2. Cluster whose containerd reads per-registry config
cat <<'YAML' | kind create cluster --name app --config=-
kind: Cluster
apiVersion: kind.x-k8s.io/v1alpha4
containerdConfigPatches:
- |-
  [plugins."io.containerd.grpc.v1.cri".registry]
    config_path = "/etc/containerd/certs.d"
YAML

# 3. Map localhost:5001 inside every node to the registry container, and join the networks
for node in $(kind get nodes --name app); do
  docker exec "$node" mkdir -p /etc/containerd/certs.d/localhost:5001
  printf '[host."http://kind-registry:5000"]\n' | docker exec -i "$node" cp /dev/stdin /etc/containerd/certs.d/localhost:5001/hosts.toml
done
docker network connect kind kind-registry

# 4. Ingress controller — cluster infrastructure, installed once, never by the app chart
helm upgrade --install ingress-nginx ingress-nginx \
  --repo https://kubernetes.github.io/ingress-nginx --namespace ingress-nginx --create-namespace
kubectl wait --namespace ingress-nginx --for=condition=ready pod \
  --selector=app.kubernetes.io/component=controller --timeout=180s

# 5. Deploy (kubectl context is now kind-app); answer the parameter prompts:
#    registry-endpoint=localhost:5001, registry-repository=<app>, k8s-namespace, helm-release-name,
#    ingress-class=nginx
dev deploy --target kubernetes

# 6. Migrate (see Postgres and migrations), then reach the ingress
kubectl port-forward --namespace ingress-nginx service/ingress-nginx-controller 8080:80

# Tear down the app (data included), then the cluster and registry when done
dev deprovision --target kubernetes --yes
kind delete cluster --name app && docker rm -f kind-registry
```

## Production-safety rules

These never appear in published output. Each is enforced by the target's aspire-tests suite —
extend the suite when you add a new publish-mode branch.

| Never ships | Why | How it is kept out |
|-------------|-----|--------------------|
| Mock authentication (`Authentication__UseMock`) | it signs requests in as a fake principal | forwarded in run mode only; the server also fail-closes outside Development/Testing |
| Browser-log forwarding | ships client telemetry to a dev dashboard | Development-only; publish runs as Production |
| Dashboard Postgres REPL (`WithRepl`) | an authenticated psql shell for anyone with dashboard access | Development-only |
| Aspire dashboard in the deployment | a second, unauthenticated UI and port | `WithDashboard(false)` on every environment — point `OTEL_EXPORTER_OTLP_ENDPOINT` at your own collector. ACA deploys it as a public `dotNetComponents` resource unless disabled |
| Any host port / externally reachable service besides the ingress | every extra exposure is an unaudited entry point | Compose: only the ingress publishes a port (`ingress-port`). Kubernetes: every Service is `ClusterIP`; the cluster Ingress is the only way in. ACA: only the ingress container app has `external: true`; web, api and grpc are internal |
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
| Azure Container Apps | `@secure()` Bicep parameters (no defaults) that become container-app secrets read through `secretRef`. The Postgres connection string lives in a Key Vault Aspire provisions, which web-server reads with its managed identity; the Postgres password is also on web-server as the container-app secrets `postgres-db-password` and `postgres-db-uri`, built from the `@secure()` parameter |

- Supply values non-interactively with `Parameters__<name>` environment variables or AppHost
  configuration/user secrets; interactive `aspire deploy` prompts for the rest. A non-interactive
  run (`dev deploy --yes`) must supply all of them.
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
| Azure Container Apps | Azure Database for PostgreSQL Flexible Server (managed storage and backups); the Bicep creates `postgres-db` | the published bundle from the operator's machine, through a temporary firewall rule — see Azure Container Apps below |

- Run the script after the database is up and before (or right after) the app starts serving.
- Compose also publishes the self-contained migration bundle; it is a host binary, not an image,
  so the SQL script is the default path. The Helm chart carries the script only — Helm rejects
  chart files over 5 MiB.
- `-T` / `-i` with `PGPASSWORD` from the container's own environment is required: the image
  enforces password auth even on the in-container socket, and a piped session cannot prompt.

## Ingress topology

All public traffic enters through the **YARP ingress**, which owns the routing table (generated
web `/api` prefixes, the api catch-all, `/grpc` prefix strip, and the public-host forwarding for
web routes). The routing is therefore identical in run mode, Compose and Kubernetes.

- **Public host travels in `X-Forwarded-Host`.** YARP sends the destination host as `Host` on
  every route and *sets* `X-Forwarded-Host` to the browser's host on web-server routes,
  overwriting any value the client sent. web-server reads it only to select the passkey RP ID
  from `WebAuthn:AllowedRpIds`; a value outside that list is rejected. Nothing else consumes
  forwarded headers (no `UseForwardedHeaders`). If you put another proxy in front of the ingress,
  it may set `X-Forwarded-Host` too; YARP still overwrites it with the `Host` it received, so make
  that proxy preserve the public `Host`.

- **Compose:** the ingress is the only service with a host port. Put TLS in front of it (your
  reverse proxy or load balancer).
- **Azure Container Apps:** the ingress container app is the only one with external ingress (ACA
  terminates HTTPS on its public FQDN); web, api and grpc have internal ingress only.
- **Kubernetes:** the chart carries one `networking.k8s.io` Ingress (`cluster-ingress`) whose
  default backend is the YARP ingress' http endpoint. TLS terminates at the cluster's ingress
  controller. The class is the `ingress-class` parameter (default `nginx`).
- **The chart does not install an ingress controller.** A controller is cluster-scoped
  infrastructure; an app chart that installs one collides with every other release. Install one
  per cluster.
- **Do not replace YARP with per-service controller routes.** That forks the routing table into a
  second implementation per target and loses the `X-Forwarded-Host` overwrite and the `/grpc` prefix strip,
  which every controller vendor expresses differently.
- Behind any proxy that terminates TLS, set `Authentication:Entra:PublicOrigin` on web-server
  explicitly when Entra is on; it is not derived from the ingress URL.

## Azure

An existing **AKS** cluster is a Kubernetes target: point the kubectl context at it, use the
cluster's registry (for example an Azure Container Registry) as `registry-endpoint`, choose the
controller's class as `ingress-class`, and run the Kubernetes commands above. The Kubernetes
target uses nothing Azure-specific, so the same chart runs on any cluster.

- **Do not use `AddAzureKubernetesEnvironment`.** It provisions and owns the cluster
  (`aspire destroy` deletes it), ties cluster lifecycle to one app, and its documented ingress
  path depends on preview features. Share an existing cluster instead.
- **Do not host Postgres data on ACA container volumes.** They are Azure Files (SMB) shares; a
  database belongs on a managed Postgres service there.
- Adding any Azure-provisioning environment is a new `Publish:Target` value with its own
  production-safety suite — follow the same pattern as the existing targets, never a second
  environment beside them.

### Azure Container Apps (`Publish:Target=aca`)

**When to choose it over AKS.** Choose ACA for a small or mostly idle app that has no cluster to
share: there are no nodes to run, patch or pay for, HTTPS ingress with a managed certificate is
built in, and billing is per use. Choose AKS (the Kubernetes target) when a cluster already exists,
when several apps share it, or when the deployment must stay portable — the ACA Bicep runs only on
Azure, while the Helm chart runs on any cluster.

**Cost model.**

- Container apps bill per vCPU-second and GiB-second on the consumption workload profile, with a
  monthly free grant. Aspire publishes every app with `minReplicas: 1`, so the apps are always on;
  scaling an app to zero is an explicit `PublishAsAzureContainerApp` change (and makes the next
  request wait for a cold start).
- The Flexible Server is the fixed cost: Burstable `Standard_B1ms`, 32 GB storage, 7-day backups,
  billed while the server runs whether or not the apps get traffic.
- Smaller items: the Azure Container Registry (Basic), Log Analytics ingestion per GB, Key Vault
  operations.

**What the AppHost provisions** (all publish-only; `dev run` keeps the Postgres container):

- A Container Apps environment `aca-env` with its own registry, Log Analytics workspace and managed
  identity, and **no Aspire dashboard** (`WithDashboard(false)`).
- Container apps for ingress, web-server, api-server and grpc-server; **only the ingress is
  external**.
- **Azure Database for PostgreSQL Flexible Server** with password authentication. The admin user
  and password are the `postgres-username` / `postgres-password` parameters (the password is a
  secret). The connection string is stored in a Key Vault (`postgres-kv`) and web-server reads it
  through a Key Vault-backed container-app secret. Key Vault is not the only copy of the password:
  web-server also gets it as two plain container-app secrets, `postgres-db-password` and
  `postgres-db-uri`, built from the `@secure()` parameter (Aspire's `WithReference` emits them;
  web-server does not read them). `aca-publish-tests` pins that set.
- **Server firewall: `AllowAllAzureIps` (0.0.0.0–0.0.0.0) with public network access on.** That
  admits any Azure-hosted IP in any tenant, not only this deployment's container apps; the admin
  password is the barrier. VNet integration (private access) is the hardening step.
  `aca-publish-tests` fails on any other or wider rule in the Bicep.
- Entra settings are the same parameters as the other targets; the client secret is a secure
  parameter that becomes a container-app secret.

**Deploy.**

```bash
az login                                    # and az account set --subscription <id>
dev publish aca                             # optional: inspect artifacts/aspire-output/aca first
dev deploy --target aca                     # prompts for location, resource group and parameters
```

**Migrations (Flexible Server).** Run the published migration bundle from the operator's machine.
The firewall only admits Azure-hosted IPs, so open a rule for your own IP for the duration.
The bundle is idempotent (applies only pending migrations) and needs no `psql`; never rely on
`EnsureCreated`.

```bash
dev publish aca                             # writes artifacts/aspire-output/aca/efmigrations/web-migrations (the bundle)
SERVER=$(az postgres flexible-server list --resource-group <rg> --query "[0].name" --output tsv)
HOST=$(az postgres flexible-server show --resource-group <rg> --name "$SERVER" --query fullyQualifiedDomainName --output tsv)
az postgres flexible-server firewall-rule create --resource-group <rg> --name "$SERVER" \
  --rule-name operator-migrate --start-ip-address <your-ip> --end-ip-address <your-ip>
artifacts/aspire-output/aca/efmigrations/web-migrations \
  --connection "Host=$HOST;Database=postgres-db;Username=<postgres-username>;Password=<postgres-password>;SSL Mode=Require"
az postgres flexible-server firewall-rule delete --resource-group <rg> --name "$SERVER" --rule-name operator-migrate --yes
```

**Where the Postgres username and password are.** They are generated parameters, so you need the
values the deploy actually used:

- On the machine (and checkout) that ran `aspire deploy`: Aspire's deployment state,
  `~/.aspire/deployments/<apphost-hash>/production.json`, keys `Parameters:postgres-username` and
  `Parameters:postgres-password` — or the AppHost user secrets if you set them there
  (`dotnet user-secrets list --project <apphost.csproj>`).
- From anywhere: the Key Vault secret `connectionstrings--postgres-db` holds the full connection
  string. The vault uses RBAC and the Bicep grants you no role, so grant yourself
  `Key Vault Secrets User` on `postgres-kv` first:

  ```bash
  VAULT=$(az keyvault list --resource-group <rg> --query "[?tags.\"aspire-resource-name\"=='postgres-kv'].name" --output tsv)
  az role assignment create --assignee "$(az ad signed-in-user show --query id --output tsv)" \
    --role "Key Vault Secrets User" --scope "$(az keyvault show --name "$VAULT" --query id --output tsv)"
  az keyvault secret show --vault-name "$VAULT" --name connectionstrings--postgres-db --query value --output tsv
  ```

- Not from the published `main.bicep`: its `postgres_username` default is whatever the publishing
  machine's deployment state held (a fresh value on CI or another checkout), so it is not a record
  of the deployed login.

The idempotent SQL script (`efmigrations/web-migrations.sql`) is the alternative when `psql` is at
hand: `psql "host=$HOST dbname=postgres-db user=<postgres-username> sslmode=require" -v
ON_ERROR_STOP=1 -f efmigrations/web-migrations.sql`, through the same firewall rule.

**Deploy problems to expect.**

- **The first deploy is slow.** It provisions the registry, Log Analytics workspace, Container
  Apps environment, Key Vault and Flexible Server before any image is pushed. Let it finish; later
  deploys only update what changed.
- **"Server is busy" from Postgres.** The Flexible Server accepts the create request before it is
  Ready, and the database or firewall step can fail while it is still provisioning. Wait for the
  server to show Ready (`az postgres flexible-server show ... --query state`) and re-run
  `dev deploy --target aca`; the deploy is idempotent.
- **Soft-deleted Key Vault names.** Deleting the resource group soft-deletes the Key Vault and
  keeps its name reserved. The Bicep derives the vault name from the resource group, so a redeploy
  into a group with the same name fails until the old vault is purged. `dev deprovision --target
  aca` prints the purge:

  ```bash
  az keyvault list-deleted --query "[?properties.tags.\"aspire-resource-name\"=='postgres-kv'].name" --output tsv
  az keyvault purge --name <vault>
  ```

- **Web hop host.** The ingress reaches web-server over ACA's internal https ingress
  (`https://web-server.internal.<domain>`; Aspire's https upgrade stays on). That works because
  `Host` is the destination host (ACA routes and validates TLS by it) and the browser's public host
  travels in `X-Forwarded-Host`, which web-server reads for passkey RP-ID selection (task 070-008,
  the same on every target). There is no aca-specific web route.

**Deprovision.** `dev deprovision --target aca` runs `aspire destroy`, then prints the Key Vault
purge. When `aspire destroy` has no record of the deployment (another machine or checkout deployed
it), it prints the manual removal: `az group delete --name <resource-group>` (az asks for
confirmation) followed by the purge.

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
- `tools/dev-cli/endpoints/deploy-command.cs` / `deprovision-command.cs` — `dev deploy` /
  `dev deprovision`; their Design regions record the preflight and the `aspire destroy` hand-off.
- aspire-tests `compose-publish-tests` / `kubernetes-publish-tests` / `aca-publish-tests` — the
  production-safety suites.

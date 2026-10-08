---
name: tw-deploy
description: "**TIMEWARP SKILL** — deploy a generated app from its Aspire AppHost: the target matrix (Docker Compose, Kubernetes/Helm, Azure: AKS or Azure Container Apps), `aspire publish` / `aspire deploy` per target, operator-run `dev deploy` / `dev deprovision` / `dev open` / `dev deploy migrate` (never CI), a local kind recipe, production-safety rules, secrets and parameters, Postgres migrations per target, the ingress topology, and container-runtime neutrality. Invoke before deploying, before adding a publish target, or before touching publish-mode wiring in the AppHost. WHEN: deploy the app, dev deploy, dev deprovision, dev open, dev deploy migrate, open the deployed app, migrate the deployed database, tear down a deployment, kind cluster, aspire publish, aspire deploy, docker compose, Helm chart, Kubernetes, AKS, Azure, Azure Container Apps, ACA, Flexible Server, Key Vault purge, production secrets, run migrations in production, ingress controller, Podman."
when-to-use: deploy, deployment, dev deploy, dev deprovision, dev open, dev deploy migrate, port-forward, deprovision, aspire destroy, kind, local registry, aspire publish, aspire deploy, dev publish, compose.yaml, docker compose, Helm, helm install, Kubernetes, kubectl, AKS, Azure, Azure Container Apps, ACA, Bicep, Flexible Server, az login, Key Vault, Publish:Target, production safety, secrets, parameters, .env, values.yaml, migrations in production, ingress controller, container runtime, Podman, ASPIRE_CONTAINER_RUNTIME
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

## Deploying: `dev deploy`, `dev deploy migrate`, `dev open`, `dev deprovision`

`dev deploy` / `dev deprovision` are thin, operator-run wrappers over `aspire deploy` /
`aspire destroy` for one `Publish:Target`; `dev deploy migrate` and `dev open` are the two steps
after a deploy (apply the migrations, reach the app), keyed on the same `--target`. **No CI job,
workflow step or `dev workflow` mode calls them, and none may.** A merge never deploys.

```bash
dev deploy                                  # compose (the Publish:Target default): preflight, plan, prompt
dev deploy --target kubernetes              # Helm chart to the CURRENT kubectl context
dev deploy --target aca                     # Azure Container Apps in the az CLI's subscription (az login first)
dev deploy --target compose --yes           # no prompt: aspire deploy --non-interactive

dev deploy migrate --target kubernetes      # psql the published migration script into the deployed postgres
dev open --target kubernetes                # port-forward the ingress controller and open the browser

dev deprovision --target kubernetes         # preflight, then aspire destroy asks before deleting
dev deprovision --target kubernetes --yes   # aspire destroy --yes --non-interactive — deletes the deployment and its data
dev deprovision --target aca                # aspire destroy, then purge the soft-deleted Key Vault
```

- **`dev deploy`** runs `aspire deploy --apphost <csproj> --environment Production --
  --Publish:Target=<target> --Parameters:<name>=<value> …`, forwarding the target's deploy
  parameters (below) after the preflight (below). It then asks for confirmation; `--yes` skips the
  prompt and adds `--non-interactive`. Without a terminal and without `--yes` it refuses. Aspire runs
  attached to the terminal, so a prompt it still shows (e.g. aca's location and resource group)
  works. For aca the preflight
  requires `az login` and prints the subscription and where it came from, in this order: the
  `Azure__SubscriptionId` environment variable, then the AppHost user secret `Azure:SubscriptionId`
  (read with `dotnet user-secrets list --project <apphost>`), then the one `az account show`
  reports. Only that last fallback is passed to `aspire deploy` as `Azure__SubscriptionId`, so the
  az CLI never overrides a subscription you pinned. A subscription Aspire remembered in its own
  deployment state is not checked; pin it in user secrets if `az account` may point elsewhere.
  Location and resource group come from `Azure__Location` / `Azure__ResourceGroup`, or Aspire's prompt.
- **Deploy configuration is committed in the AppHost `appsettings.json`** `Parameters` section.
  The kubernetes target needs `k8s-namespace`, `helm-release-name`, `registry-endpoint` and
  `registry-repository` (the AppHost parameters without a default in code); compose and aca need
  none. None of them is a secret. The template sets the three that identify the app —
  `k8s-namespace`, `helm-release-name`, `registry-repository` — to the app's kebab name (a DNS-1123
  label: `Contoso.Shop` → `contoso-shop`), and `registry-endpoint` to `localhost:5001`, the kind
  recipe's registry, so a kind deploy needs no configuration:

  ```json
  "Parameters": {
    "k8s-namespace": "contoso-shop",
    "helm-release-name": "contoso-shop",
    "registry-endpoint": "localhost:5001",
    "registry-repository": "contoso-shop"
  }
  ```

  A value that differs per machine or target overrides the committed one in the AppHost user
  secrets — typically `registry-endpoint` for AKS, an Azure Container Registry login server (pwsh):

  ```powershell
  dotnet user-secrets set 'Parameters:registry-endpoint' 'myregistry.azurecr.io' --project <apphost.csproj>
  ```

  or for one session with a `Parameters__<name>` environment variable
  (`${env:Parameters__registry-endpoint} = 'myregistry.azurecr.io'`; the name is matched
  case-insensitively). `dev deploy` resolves each in the order Aspire's configuration does —
  environment variable, then user secret, then `appsettings.Production.json` (none ships), then
  `appsettings.json` — and the plan prints each value and that source. A user secret left over for
  a committed value keeps winning; remove it with `dotnet user-secrets remove
  'Parameters:<name>' --project <apphost.csproj>`. The values appear in the `aspire` command line,
  so a forwarded parameter is never a secret — secrets (`postgres-password`, the Entra client
  secret) stay in user secrets or env vars, never in `appsettings.json`.
- **Preflight runs before `aspire` and reports every problem at once**, then exits non-zero with
  nothing run: Aspire CLI ≥ 13.6; for kubernetes, Helm ≥ 4.2, a current kubectl context (printed —
  check it) whose API answers (`kubectl get --raw /version`), every required parameter set (each
  missing one is listed with the command that sets it), and for a kind context (`kind-<cluster>`)
  that `kind get clusters` lists the cluster and the registry at `registry-endpoint` answers its
  `/v2/` API. The API and registry checks give up after 5 seconds and every other probe after 20
  (e.g. a credential plugin waiting for a login), reported as timed out; if a parameter is missing
  and `dotnet user-secrets list` failed, the report says the secrets could not be read.
- **Any kubectl context works** — AKS, an on-prem cluster, or a local kind cluster. `dev deploy`
  never creates a cluster, installs an ingress controller or switches context.
- **`dev deploy migrate`** applies the published idempotent migrations (safe to re-run) for the
  target, after a confirmation (`--yes` skips it; without a terminal and without `--yes` it
  refuses), prints one summary line, and exits with the underlying tool's exit code. It runs the
  file `dev publish <target>` wrote under `artifacts/aspire-output/<target>/efmigrations/`; when that
  is missing it **refuses with the exact `dev publish <target>` command** rather than publishing for
  you (publishing is its own gated step, and you should know which script runs). Per target it runs
  what the Postgres and migrations table below lists: compose — the running Compose project from
  `<runtime> compose ls` (`--project-name` picks one when several run; the runtime is
  `ASPIRE_CONTAINER_RUNTIME`, else docker); kubernetes — `kubectl exec` in the `k8s-namespace`
  parameter's namespace on the current context (same preflight as `dev deploy`, minus Helm and the
  registry); aca — the bundle through a temporary firewall rule that is **always deleted**, even on
  failure (Azure Container Apps below).
- **`dev open`** opens the deployed app in the browser (`--no-browser` prints the URL; with no
  opener on PATH it prints it too — on WSL it uses `wslview`, then `explorer.exe`). kubernetes: if
  the ingress controller's Service has an external (LoadBalancer) address, it opens that;
  otherwise it runs `kubectl port-forward --namespace ingress-nginx
  service/ingress-nginx-controller 8080:80` in the foreground, opens `http://localhost:8080`, and
  Ctrl+C stops the forward. `--port` picks another local port (a taken one is refused with that
  hint); `--controller-namespace` / `--controller-service` name a controller other than the kind
  recipe's. compose: `http://localhost:<port>` from `INGRESS_PORT` in the published `.env`, else the
  `ingress-port` parameter (default 8080). aca: `https://<fqdn>` of the `ingress` container app
  (`az containerapp show --name ingress --resource-group <rg> --query
  properties.configuration.ingress.fqdn`).
- **aca resource group** for both: `--resource-group`, else `Azure__ResourceGroup`, else the AppHost
  user secret `Azure:ResourceGroup` — the values `dev deploy --target aca` uses; the subscription
  comes from the same preflight as `dev deploy`.
- **`dev deprovision`** runs `aspire destroy` for the same target after the same preflight, minus
  the registry check (for kubernetes it prints the kubectl context first). Deploy parameters are
  optional there: each one that is set is forwarded the same way (`--Parameters:<name>=<value>`), and
  a missing one is never refused. It deletes the deployment's data (Compose volumes,
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
  the runtime; the verbs call no container CLI themselves (the registry check is an HTTP request).
- **Without the `dev` CLI** (it lives in the template's source repository), run the same commands
  the verbs wrap, and do the preflight yourself (`helm version`, `kubectl config current-context`,
  `kubectl get --raw /version`):

  ```bash
  aspire deploy  --apphost <apphost.csproj> --environment Production -- --Publish:Target=<target> --Parameters:<name>=<value>
  aspire destroy --apphost <apphost.csproj> --environment Production -- --Publish:Target=<target> [--Parameters:<name>=<value>]
  ```

  When `aspire destroy` fails or reports nothing to destroy, nothing was removed; use the manual
  removal above. `dev deploy migrate` runs the commands in the Postgres and migrations table, and
  `dev open` runs `kubectl port-forward --namespace ingress-nginx
  service/ingress-nginx-controller 8080:80` (kind) and opens `http://localhost:8080`.

### Local Kubernetes with kind

A kind cluster is just another kubectl context: give it a local registry the nodes can pull from,
install ingress-nginx once as cluster infrastructure, then deploy, migrate and open. The full
pwsh and bash recipe (registry, cluster, node registry mapping, ingress, deploy, tear down) is in
[local-kubernetes-with-kind.md](references/local-kubernetes-with-kind.md).

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

Every value that differs per deployment is an `AddParameter`, never a literal in code; a
non-secret default every machine shares (the app's identity) is committed in the AppHost
`appsettings.json` `Parameters` section, not in the `AddParameter` call. Secrets are
`AddParameter(name, secret: true)` (or a generated secret such as Postgres' password).

| Target | Where parameter values live |
|--------|-----------------------------|
| Compose | `.env` beside `docker-compose.yaml`; `aspire deploy` / `aspire do prepare-compose` fill it |
| Kubernetes | `values.yaml`: secrets under `secrets.<resource>` (rendered into `<resource>-secrets` Secret objects, empty defaults), never ConfigMaps |
| Azure Container Apps | `@secure()` Bicep parameters (no defaults) that become container-app secrets read through `secretRef`. The Postgres connection string lives in a Key Vault Aspire provisions, which web-server reads with its managed identity; the Postgres password is also on web-server as the container-app secrets `postgres-db-password` and `postgres-db-uri`, built from the `@secure()` parameter |

- Non-secret deploy configuration that names the app is committed in the AppHost `appsettings.json`
  `Parameters` section (see Deploying). Per-machine values and secrets go in AppHost user secrets
  `Parameters:<name>` or `Parameters__<name>` environment variables, which override the committed
  value. `dev deploy` refuses until the target's required ones resolve and forwards them to Aspire;
  interactive `aspire deploy` prompts for any other unset parameter.
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

Apply them with **`dev deploy migrate --target <target>`** (Deploying above). The last column is
what it runs — run it yourself in a generated app without the `dev` CLI (`<runtime>` is `docker`
or `podman`; `dev deploy migrate` passes the running project's `--project-name` and `--file` from
`<runtime> compose ls`):

| Target | Storage | Apply migrations (what `dev deploy migrate` runs) |
|--------|---------|------------------|
| Compose | named volume `postgres-data`; `POSTGRES_DB` creates the database on first start | `<runtime> compose exec -T postgres sh -c 'PGPASSWORD="$POSTGRES_PASSWORD" psql -U postgres -d postgres-db -v ON_ERROR_STOP=1' < efmigrations/web-migrations.sql` |
| Kubernetes | `postgres-data` PersistentVolumeClaim (`postgres-storage-capacity`, default 10Gi), single-replica StatefulSet | `kubectl exec -i -n <namespace> statefulset/postgres-statefulset -- sh -c 'PGPASSWORD="$POSTGRES_PASSWORD" psql -U postgres -d postgres-db -v ON_ERROR_STOP=1' < efmigrations/web-migrations.sql` |
| Azure Container Apps | Azure Database for PostgreSQL Flexible Server (managed storage and backups); the Bicep creates `postgres-db` | the published bundle from the operator's machine, through a temporary firewall rule — see Azure Container Apps below |

The script is piped on stdin (`< efmigrations/web-migrations.sql` in bash; in pwsh
`Get-Content -Raw efmigrations/web-migrations.sql | <command>`).

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

Choose ACA for a small or mostly idle app with no cluster to share; choose AKS (the Kubernetes
target) when a cluster exists, is shared, or the deployment must stay portable. The publish-only
AppHost provisions a Container Apps environment (no dashboard), ingress/web/api/grpc container
apps (only the ingress external) and an Azure Database for PostgreSQL Flexible Server whose firewall
rule is `AllowAllAzureIps` (the admin password is the barrier; `aca-publish-tests` pins the rule).
Deploy with `dev deploy --target aca`, migrate with `dev deploy migrate --target aca --resource-group <rg>`,
open with `dev open --target aca --resource-group <rg>`, remove with `dev deprovision --target aca`.

Cost model, what the AppHost provisions, the deploy commands, the migration bundle by hand
(firewall rule, connection string), where the Postgres credentials live, expected deploy problems
(slow first deploy, "Server is busy", soft-deleted Key Vault names, web hop host) and deprovision
detail: [azure-container-apps.md](references/azure-container-apps.md).

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
- `tools/dev-cli/endpoints/deploy-migrate-command.cs` / `open-command.cs` — `dev deploy migrate` /
  `dev open`; `services/deploy-operate.cs` records why migrate refuses instead of publishing, how the
  Compose project is found, and the firewall-rule cleanup.
- aspire-tests `compose-publish-tests` / `kubernetes-publish-tests` / `aca-publish-tests` — the
  production-safety suites.

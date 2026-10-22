# Azure Container Apps with Neon (`Publish:Target=aca-neon`)

Low-cost hosting for a generated app: Azure Container Apps on the consumption plan, and Neon
(Lakebase) Free for Postgres. The AppHost path is the same Aspire ACA publisher as `aca`
(`AddAzureContainerAppEnvironment("aca-env")`). Postgres is **not** a Flexible Server and **not**
a Key Vault. It is two connection strings you set as secrets.

`aca` (Flexible Server) is unchanged. Run mode (`dev run`) never reads `Publish:Target` and still
starts the local Postgres container.

## Cost

| Piece | What you pay | Notes |
|-------|----------------|-------|
| Neon Free | $0 | Per project: 100 CU-hours (0.25 CU × 400 hours), autoscaling 0.25–2 CU, 1 GB storage, 5 GB public egress. Compute **suspends after 5 minutes**. That cannot be turned off. https://neon.com/docs/introduction/plans |
| ACA consumption | $0 while you stay inside the monthly free grant | Container apps publish with **min replicas 0**, max replicas 1, 0.25 CPU, 0.5Gi. Idle replicas do not bill. https://azure.microsoft.com/pricing/details/container-apps/ |
| Container registry | **not free** | Aspire's environment provisions **ACR Basic** (about $5 per month). This is the exception to a $0 bill. GitHub Container Registry cannot replace it; see [Why the registry stays ACR](#why-the-registry-stays-acr). Deleting the resource group deletes the registry. |
| Log Analytics | usually inside the 5 GB/month free ingestion | The environment creates a workspace. A noisy app can exceed the free tier. |
| Key Vault, Flexible Server | not provisioned | `aca-neon` does not declare them. |

## Why the registry stays ACR

Aspire 13.6.2 provisions **ACR Basic** for this environment when every project points at a private
GitHub Container Registry. There is no monthly saving, so the template keeps the registry Aspire
creates.

Packages: `Aspire.Hosting` and `Aspire.Hosting.Azure.AppContainers` **13.6.2**
(`13.6.2+af07e33f8d1ef733645138b3f8e36067d073148d`). Published Bicep comes from those AppHost
packages. Docs: [ASPIRECOMPUTE003](https://aspire.dev/diagnostics/aspirecompute003/) (preview),
[Container registry configuration](https://aspire.dev/app-host/container-registry/), and
[Azure Container Registry integration](https://aspire.dev/integrations/cloud/azure/azure-container-registry/azure-container-registry-get-started/).
Source for this build:
[ContainerRegistryResourceBuilderExtensions.cs](https://github.com/microsoft/aspire/blob/af07e33f8d1ef733645138b3f8e36067d073148d/src/Aspire.Hosting/ContainerRegistryResourceBuilderExtensions.cs),
[AzureContainerAppExtensions.cs](https://github.com/microsoft/aspire/blob/af07e33f8d1ef733645138b3f8e36067d073148d/src/Aspire.Hosting.Azure.AppContainers/AzureContainerAppExtensions.cs),
[ContainerRegistryInfrastructure.cs](https://github.com/microsoft/aspire/blob/af07e33f8d1ef733645138b3f8e36067d073148d/src/Shared/ContainerRegistryInfrastructure.cs),
[BaseContainerAppContext.cs](https://github.com/microsoft/aspire/blob/af07e33f8d1ef733645138b3f8e36067d073148d/src/Aspire.Hosting.Azure.AppContainers/BaseContainerAppContext.cs).

### Per-project API (Aspire 13.6.2)

These methods are on `Aspire.Hosting` 13.6.2 and match the authored source above:

| Method | Signature |
|--------|-----------|
| `AddContainerRegistry` | `(IDistributedApplicationBuilder, string name, string endpoint, string? repository = null)` → `IResourceBuilder<ContainerRegistryResource>` |
| `AddContainerRegistry` | `(IDistributedApplicationBuilder, string name, IResourceBuilder<ParameterResource> endpoint, IResourceBuilder<ParameterResource>? repository = null)` → same |
| `WithContainerRegistry` | `<TDestination, TContainerRegistry>(IResourceBuilder<TDestination>, IResourceBuilder<TContainerRegistry>)` where `TDestination : IResource` and `TContainerRegistry : IResource, IContainerRegistry`. Appends `ContainerRegistryReferenceAnnotation`. |

Both `AddContainerRegistry` overloads are `ASPIRECOMPUTE003`. Each one also subscribes to `BeforeStart` and adds a `RegistryTargetAnnotation` for that registry on **every** resource. `ResourceExtensions.GetContainerRegistry` then picks, in order, the explicit annotation, then `DeploymentTargetAnnotation.ContainerRegistry`, then a single `RegistryTargetAnnotation`.

[Container registry configuration](https://aspire.dev/app-host/container-registry/) shows the ACA
example as `WithContainerRegistry(environment)`, which uses the environment's own ACR and Azure
credentials. That is the managed-identity path. A `ghcr.io` `ContainerRegistryResource` is a
different registry.

### The environment still creates ACR

`AddAzureContainerAppEnvironment` always calls `CreateDefaultAzureContainerRegistry` with the name `{environment}-acr`, assigns that `AzureContainerRegistryResource` to `DefaultContainerRegistry`, and adds it to the model in publish mode. The environment Bicep uses a `ContainerRegistryReferenceAnnotation` only when that registry is an `AzureProvisioningResource`. A generic `ContainerRegistryResource` (`ghcr.io`) is not one, so the callback keeps the default ACR. `ContainerRegistryInfrastructure` sets SKU `Premium` when the registry has a private endpoint, and **Basic** otherwise. Azure Container Registry has no free tier.

`AzureContainerAppEnvironmentResource.ContainerRegistry` throws when a `WithContainerRegistry` annotation **on the environment** is not an `AzureContainerRegistryResource`:

> The container registry configured for the Azure Container App Environment 'aca-env' is not an Azure Container Registry. Only Azure Container Registry resources are supported. Use '.WithAzureContainerRegistry()' to configure an Azure Container Registry.

That throw is the environment attachment (task 317). With `WithContainerRegistry(ghcr)` on every
project, `aspire publish` still writes `aca-env-acr/aca-env-acr.bicep`
(`Microsoft.ContainerRegistry/registries`, `sku.name` `Basic`) and `aca-env/aca-env.bicep` (that
registry as `existing`, an `AcrPull` role assignment, and outputs
`AZURE_CONTAINER_REGISTRY_ENDPOINT` and `AZURE_CONTAINER_REGISTRY_MANAGED_IDENTITY_ID`) (task 400).

### How each container app pulls

`ContainerAppContext.BuildContainerApp` calls `AllocateContainerRegistryParameters`, which binds those two environment outputs. `AddContainerRegistryParameters` writes `configuration.registries` with `Server` and `Identity` only. For the built projects the published modules match that. `api-server.bicep` (grpc and web are the same shape):

```bicep
registries: [
  {
    server: aca_env_outputs_azure_container_registry_endpoint
    identity: aca_env_outputs_azure_container_registry_managed_identity_id
  }
]
```

The image is a separate parameter (`api_server_containerimage`, and the same for grpc and web).
At deploy time `GetFullRemoteImageNameAsync` fills it from the project's registry, so an explicit
`WithContainerRegistry(ghcr)` names `ghcr.io/<owner>/<resource>`. The pull entry stays the ACR
login server and its managed identity. The ingress resource is the public
`mcr.microsoft.com/dotnet/nightly/yarp` image, so its module has no `registries` entry.
`WithContainerRegistry` on the ingress leaves that public image in place.

`Azure.Provisioning.AppContainers` 1.2.0 `ContainerAppRegistryCredentials` also has `Username` and
`PasswordSecretRef`. Aspire 13.6.2 writes `Server` and `Identity` only.
`PublishAsAzureContainerApp` runs after the registries list is assigned, so a callback can
overwrite that list. The environment module still provisions ACR Basic, which is the cost a
private registry has to remove.

### Token

GitHub documents GHCR authentication as a [personal access token (classic)](https://docs.github.com/en/packages/working-with-a-github-packages-registry/working-with-the-container-registry): `read:packages` to download images, `write:packages` to download and upload. The token UI also selects the `repo` scope unless the token is created from `https://github.com/settings/tokens/new?scopes=write:packages`. Expiration is chosen when the token is created; an organization can enforce a maximum lifetime ([Managing your personal access tokens](https://docs.github.com/en/authentication/keeping-your-account-and-data-secure/managing-your-personal-access-tokens)).

`dev deploy` does not forward a registry token. If a later Aspire release publishes this
environment with no ACR, the token belongs in an AppHost user secret, and `dev deploy`
should copy it into the aspire process the way it copies the Neon strings: off the command
line, and absent from the plan.

### Recommendation

Keep ACR Basic on Aspire 13.6.2. Leave the template on the registry `AddAzureContainerAppEnvironment` provisions. The per-project hook can name a push target. It does not stop `{environment}-acr` from being published, and the container app pull credential stays a managed identity for that ACR. Revisit when an Aspire release can publish this environment with no `AzureContainerRegistryResource` and a container app can pull through `Username` and `PasswordSecretRef` without an ACR role assignment.

## Neon project

1. Create a project on the Free plan: https://neon.com/docs/introduction/plans
2. Copy two connection strings from the Neon console (connection pooling docs:
   https://neon.com/docs/connect/connection-pooling):
   - **Pooled** — host contains `-pooler`. The app uses this.
   - **Direct** — same database, host does **not** contain `-pooler`. Migrations use this.
3. Put these knobs on the strings. `dev deploy` refuses when either string is missing or wrong.
   `dev deploy migrate` checks only the direct string. A refusal does not print the string. Use the
   key/value form. `Host` and `Server` both name the host. A `postgresql://` URI is refused.
   `Channel Binding` must be `Disable`. Npgsql rejects `Disabled`.

Pooled (app):

```text
Host=<endpoint>-pooler.<region>.aws.neon.tech;Database=neondb;Username=<role>;Password=<password>;SSL Mode=Require;Timeout=30;Max Auto Prepare=0;No Reset On Close=true;Channel Binding=Disable
```

Direct (migrations):

```text
Host=<endpoint>.<region>.aws.neon.tech;Database=neondb;Username=<role>;Password=<password>;SSL Mode=Require;Timeout=30
```

- `SSL Mode=Require` (or `VerifyFull`). Neon requires TLS.
- `Timeout=30` (seconds) covers a cold start. Neon Free may be suspended.
- `Max Auto Prepare=0`, `No Reset On Close=true`, `Channel Binding=Disable` are for PgBouncer
  transaction mode on the pooler. Npgsql's defaults (auto-prepare, channel binding) fail against it
  or hold server state the pooler drops. https://neon.com/docs/guides/dotnet-entity-framework
- Do **not** set `Keepalive` above 0 on the pooled string. A ping would keep Neon compute awake and
  burn the 100 CU-hour grant.
- The direct string does not need the pooler knobs. Do not point migrations at the pooler.
- A semicolon inside the password breaks the check (and usually the driver). Neon passwords do not
  need one.

## Where the secrets go

Set them on the **AppHost** user secrets, or as environment variables for one shell. Do **not**
commit them and do **not** put them in `appsettings.json`. `dev deploy` refuses a value found in
appsettings. It does **not** put these on the `aspire` command line (they would show up in the
process list and in the plan), and the plan does not print them.

`aspire deploy` runs with `--environment Production`. That AppHost loads user secrets only when
the environment is Development, so Production does not see `dotnet user-secrets`. `dev deploy`
copies a user-secret value into the aspire process as `Parameters__<name>` when that variable is
not already set. An environment variable you set yourself is inherited and wins.

The pooled string is the web-server container-app secret `ConnectionStrings__postgres-db` (`secretRef`
to an `@secure()` parameter). The direct string is not published into Azure. `dev deploy migrate`
reads it from the same user secrets and passes it to the bundle on your machine.

pwsh, from the repo:

```powershell
$apphost = "source/container-apps/aspire/projects/aspire-app-host/aspire-app-host.csproj"
dotnet user-secrets set "Parameters:neon-pooled-connection-string" "<pooled string>" --project $apphost
dotnet user-secrets set "Parameters:neon-direct-connection-string" "<direct string>" --project $apphost
```

One session only:

```powershell
${env:Parameters__neon-pooled-connection-string} = "<pooled string>"
${env:Parameters__neon-direct-connection-string} = "<direct string>"
```

The names match the AppHost parameters `neon-pooled-connection-string` and
`neon-direct-connection-string`. web-server receives the pooled value as
`ConnectionStrings__postgres-db` (the same key as every other target). The migration bundle,
started by `dev deploy migrate` on your machine, receives the direct value as its `--connection`.
Publish output has no `web-migrations` container and no direct parameter.

Subscription, location and resource group follow the same rule. An `Azure__SubscriptionId`,
`Azure__Location` or `Azure__ResourceGroup` environment variable is inherited. Otherwise `dev deploy`
copies the AppHost user secrets `Azure:SubscriptionId`, `Azure:Location` and `Azure:ResourceGroup`
into those variables. With none of those set, the subscription falls back to `az account show` and
Aspire prompts for the location and resource group.

The published Entra client secret cannot be empty: Azure Container Apps rejects an empty
container-app secret. The same placeholder is published for every target, not only this one.
When `Parameters:entra-client-secret` is unset, the AppHost uses the placeholder `unused` (it is
not a credential). With Entra enabled and that placeholder still set, sign-in fails with AADSTS
`invalid_client`. Set that user secret, or `Parameters__entra-client-secret`, before turning Entra
on. `dev deploy` copies the user secret the same way as the Neon strings.

## Region

Put the Container Apps environment and the Neon project in the same region, near the users. A Neon
host looks like `ep-….ap-southeast-1.aws.neon.tech`; the AWS region is the label before `aws`. The
nearby Azure region for `ap-southeast-1` is `southeastasia`. Set that as `Azure:Location` (or
`Azure__Location`) before deploy. `dev deploy --target aca-neon` warns when the pooled host's region
is far from `Azure:Location` and names the nearby Azure region. A nearby Azure region for that AWS
region (for example `eastus2` when Neon is in `us-east-1`) stays quiet. It does not print the connection
string, and it does not refuse.

## Deploy

One command, after `az login` and the two Neon secrets. Set `Azure:Location` and
`Azure:ResourceGroup` as well. Passkeys accept the ingress host on the first deploy
(`ingress.<environment domain>` is added to `WebAuthnOptions:AllowedRpIds`). A custom domain also
needs `Parameters:passkey-rp-host` set to that bare DNS name.

```bash
dev deploy --target aca-neon   # publish, deploy, migrate, smoke, open the app (--no-browser prints the URL)
```

`dev deploy` publishes the migrations bundle, deploys, migrates on the direct connection, then
requests `GET /web-server/alive` and `GET /api/identity/entra/offered`. It retries a Neon cold
start. It does not request `/api/health`.

Operator steps, when you want one of them alone:

```bash
dev publish aca-neon          # Bicep only; no Azure or Neon credentials. Also the safety suite.
dev deploy migrate --target aca-neon
dev open --target aca-neon
```

`dev deploy migrate --target aca-neon` runs the published bundle
(`artifacts/aspire-output/aca-neon/efmigrations/web-migrations`) with `--connection` set to the
**direct** secret. It does not need `az login` or the pooled string. There is no Flexible Server,
no Key Vault, and no firewall rule. The command redacts the string as `<direct Neon connection string>`.

web-server's EF retry on this target only is `Postgres__RetryCount=6` and
`Postgres__RetryDelaySeconds=8`. `RetryDelaySeconds` is the maximum delay between tries (EF backs
off up to that cap), not a fixed wait. The default elsewhere stays 4 tries and a 2 second maximum.
Together with `Timeout=30` that covers Neon's wake-up. The startup environment check calls
`CanConnectAsync` once per process start, which is what wakes Neon. It is not a poll.

## Smoke

After migrate (the first request may wait while Neon wakes):

```bash
curl -fsS "https://<ingress-fqdn>/web-server/alive"
curl -fsS "https://<ingress-fqdn>/api/identity/entra/offered"
```

- `/web-server/alive` does not query Postgres. Expect HTTP 200. Publish emits no HTTP probe
  (Compose, Kubernetes, and Azure Container Apps), so nothing polls this path. `/health` is
  Development-only and is not mapped in this deployment.
- `/api/health` is anonymous and runs the `PostgresDbContext` check. That opens a connection and
  wakes Neon. Do not point an uptime monitor at it.
- `/api/identity/entra/offered` is anonymous and reads site settings from Postgres. Expect HTTP 200
  and a small JSON body (`offered`). A 500 after a cold start usually means the replica is still
  waiting on Neon, or migrate has not been run.

`dev open --target aca-neon` prints or opens `https://<ingress-fqdn>`.

## Scale-to-zero

min replicas 0 means a quiet app has no container and no probe traffic. Neon suspends on its own
5 minutes after the last connection. The next request pays a cold start (container, then Neon).
Do not add a health check that opens Postgres on a timer. A signed-in cookie still validates
after that idle period because the Data Protection key ring is the Postgres table
`data_protection.keys`, not the container disk. See [Data Protection key ring](../SKILL.md#data-protection-key-ring).

## Extensions

The migrations in `source/container-apps/web/platform/postgres/migrations/` use core types only
(uuid, integer, bytea, timestamptz, text, varchar, boolean, bigint), schemas, indexes, and a few
`UPDATE` statements. They do not run `CREATE EXTENSION` or `HasPostgresExtension`. Neon supports
those types. `pg_trgm` is mentioned only as a future index in the profile entity configuration;
Neon lists `pg_trgm` for Postgres 14–18 (https://neon.com/docs/extensions/pg-extensions) if a later
migration adds it. `neon-extension-tests` fails when a migration starts creating an extension.

## Tear down

```bash
dev deprovision --target aca-neon
```

That deletes the Azure resource group (the container apps, the environment, the registry, and the
Log Analytics workspace). It does **not** purge a Key Vault, because this target does not create
one. It does **not** delete the Neon project. Suspend or delete that in the Neon console when you
are done with the data. ACR Basic keeps billing until the resource group is gone.

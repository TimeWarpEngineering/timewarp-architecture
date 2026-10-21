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

Aspire 13.6.2 cannot push this environment's images to GitHub Container Registry (`ghcr.io`) or any other non-ACR registry, and it cannot take a registry password as the pull credential.

`AzureContainerAppEnvironmentResource.ContainerRegistry` (package `Aspire.Hosting.Azure.AppContainers` 13.6.2) throws when the registry attached with `WithContainerRegistry` is not an `AzureContainerRegistryResource`:

> The container registry configured for the Azure Container App Environment 'aca-env' is not an Azure Container Registry. Only Azure Container Registry resources are supported. Use '.WithAzureContainerRegistry()' to configure an Azure Container Registry.

`AddContainerRegistry("ghcr", "ghcr.io")` creates a generic `ContainerRegistryResource`, which is the type that throw names. The container app's `registries` entry is the registry server plus `ContainerRegistryManagedIdentityId` (managed identity pull). There is no username or password secret for a GHCR credential. `ContainerRegistryInfrastructure` creates the registry at SKU Basic (`ContainerRegistrySkuName.Basic`) unless a private endpoint forces Premium. Azure Container Registry has no free tier, so Basic (about $5 per month) is the cost exception. ACR stays. It is not an opt-in beside GHCR, because GHCR is not a supported pull target for this publisher.

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

## Deploy, migrate, open

```bash
az login
dev publish aca-neon          # Bicep only; no Azure or Neon credentials
dev deploy --target aca-neon
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
Do not add a health check that opens Postgres on a timer.

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

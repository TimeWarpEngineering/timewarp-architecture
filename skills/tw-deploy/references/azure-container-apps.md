# Azure Container Apps (`Publish:Target=aca`)

Detail for the `tw-deploy` skill ([SKILL.md](../SKILL.md)).

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
dev deploy migrate --target aca --resource-group <rg>
dev open --target aca --resource-group <rg> # https://<fqdn> of the ingress container app
```

**Migrations (Flexible Server).** Run the published migration bundle from the operator's machine.
The firewall only admits Azure-hosted IPs, so open a rule for your own IP for the duration.
The bundle is idempotent (applies only pending migrations) and needs no `psql`; never rely on
`EnsureCreated`.

`dev deploy migrate --target aca` does all of it: it finds the one Flexible Server in the resource
group, reads the connection string from the Key Vault secret `connectionstrings--postgres-db` (see
below; without the role it refuses with the pwsh grant), takes your IPv4 from `--client-ip` or
`https://api.ipify.org`, creates the `operator-migrate` rule, runs the bundle with `--connection`
(the password is never printed), and deletes the rule in a `finally` — on failure and Ctrl+C too; if
the delete itself fails it prints the delete command. What it runs, by hand:

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

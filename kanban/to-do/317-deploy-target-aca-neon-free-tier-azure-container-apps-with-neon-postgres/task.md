# Deploy target aca-neon: free-tier Azure Container Apps with Neon Postgres

## Description

Add a low/no-cost hosting option: Neon (Lakebase) free-tier Postgres plus Azure Container Apps (consumption,
free monthly grant) for compute. Requested by Steven 2026-10-12.

## Current state (checked 2026-10-12, master 78fd5f4)

- `dev deploy [--target compose|kubernetes|aca]` (`tools/dev-cli/endpoints/deploy-command.cs`, logic in
  `services/aspire-deploy.cs`) runs `aspire deploy ... -- --Publish:Target=<t>`. Also `dev publish aca` and
  `dev deploy migrate --target aca`.
- AppHost (`source/container-apps/aspire/projects/aspire-app-host`): `Publish:Target` constants in
  `constants.cs` (`compose`, `kubernetes`, `aca`). The `aca` branch uses `AddAzureContainerAppEnvironment("aca-env")`
  + `AddAzurePostgresFlexibleServer` with password auth, connection string in Key Vault (`postgres-kv`).
  Run mode keeps the local `AddPostgres` container unchanged.
- ACA provisioning is Aspire's ACA publisher (Bicep via `aspire publish`/`aspire deploy`), not azd.
- ACA migrations: the published EF bundle run by the operator (`dev deploy migrate --target aca`) through a
  temporary Flexible Server firewall rule.

## Requirements

1. **Target option.** Add `aca-neon` as a new `Publish:Target` value and `dev deploy --target aca-neon`
   (also `dev publish aca-neon` and `dev deploy migrate --target aca-neon`), named like the existing targets.
   It reuses the `aca` environment but does NOT declare Flexible Server or `postgres-kv`; Postgres is an
   external connection string (`AddConnectionString`/secure parameter).
2. **Neon vs regular Postgres, as per-target configuration, not code forks.**
   - Connection strings: pooled (`-pooler` host) for the app, direct for migrations; `SSL Mode=Require`
     (and channel binding if needed). Npgsql/PgBouncer transaction mode: check prepared statements /
     `Max Auto Prepare`, `No Reset On Close`.
   - Extensions: verify every extension the migrations use is available on Neon.
   - Migration runner uses the direct (non-pooled) connection; no firewall-rule step.
   - Cold start / scale-to-zero (Free suspends after 5 min, can't be disabled): connection timeout and
     retry policy sized for wake-up; health checks must not keep compute awake 24/7 (100 CU-hours/month cap).
   - Local Aspire Postgres (run mode) unchanged.
3. **Deploy to ACA with Neon.**
   - Provision with the existing Aspire ACA publisher path.
   - Neon connection strings as secure parameters → container-app secrets; never committed. Document where
     the operator sets them (user secrets / env).
   - Stay inside free tiers: ACA consumption plan with min replicas 0, no Key Vault/Flexible Server, small
     CPU/memory; Neon Free (0.25-2 CU, 1 GB storage/project, 5 GB egress). Note the container registry
     (ACR Basic) is not free; document the cost or use an alternative (e.g. GHCR).
   - Smoke test: after deploy, hit the ingress health endpoint and one DB-backed endpoint (first request
     may wake Neon).

## Acceptance criteria

- [x] `dev deploy --target aca-neon` and `dev publish aca-neon` exist and appear in help with examples
- [x] Publish output for `aca-neon` contains no Flexible Server, no postgres Key Vault, and min replicas 0
- [x] Neon settings (pooled/direct strings, SSL, timeouts) are configuration for this target only; other targets' publish output unchanged (existing publish tests pass)
- [x] `dev deploy migrate --target aca-neon` applies migrations via the direct connection
- [x] Extension compatibility with Neon confirmed and recorded
- [x] Run mode still uses the local Postgres container
- [ ] One real deploy to ACA + Neon Free passes the smoke test; resulting monthly cost recorded (target $0 or noted exceptions like ACR)
- [x] Docs: operator guide for creating the Neon project, setting secrets, deploying, tearing down

## Links

- Neon Lakebase: https://neon.com/lakebase
- Neon pricing / Free plan: https://neon.com/pricing , https://neon.com/docs/introduction/plans
- Neon connection pooling: https://neon.com/docs/connect/connection-pooling
- Neon + EF Core / Npgsql: https://neon.com/docs/guides/dotnet-entity-framework
- ACA pricing (free grant): https://azure.microsoft.com/pricing/details/container-apps/
- Aspire ACA deployment: https://learn.microsoft.com/dotnet/aspire/deployment/azure/aca-deployment

## Goal (implementer restatement)

Add `aca-neon` as a fourth publish and deploy target. It uses the same Azure Container Apps environment as `aca`, but Postgres is an external Neon connection string, not Flexible Server and not a Key Vault. The app gets the pooled Neon host; migrations use the direct host. SSL, PgBouncer prepared-statement settings, and a wake-up connection timeout live in those strings (operator secrets), and a longer EF retry is set only on this target. Container apps scale to zero with a small CPU and memory so idle time does not burn the ACA free grant or Neon's compute-hours. Run mode still starts the local Postgres container. The other targets' publish output stays as it is.

Done when:

- `dev deploy --target aca-neon`, `dev publish aca-neon`, and `dev deploy migrate --target aca-neon` show up in help with examples.
- `aca-neon` Bicep has no Flexible Server, no Postgres Key Vault, and `minReplicas: 0`.
- Neon connection settings are this target's configuration; existing publish tests still pass.
- Migrate applies the bundle with the direct connection and does not open a firewall rule.
- The migrations' extension use is checked against Neon and written down.
- Run mode still uses the local Postgres container.
- One real ACA + Neon Free deploy passes a health check and one database-backed request, and the monthly cost is recorded.
- An operator guide covers creating the Neon project, setting secrets, deploying, and tearing down.

## Learnings

## Session

- Created: grok-bot (2026-10-12)

- 2026-10-21: `aca-neon` publish, deploy, migrate, open, and deprovision are wired. The pooled Neon reference is attached to web-server after `AddEFMigrations`, because that call snapshots the startup project's environment onto `ef-tool-web-migrations` and two `ConnectionStrings__postgres-db` references collide. The direct string stays off the Azure publish output; migrate reads it on the operator machine. `ganda repo audit --fix` pinned TimeWarp.Nuru and TimeWarp.Nuru.DevCli to 4.0.0-beta.5. No live deploy: AppHost user secrets do not contain the two Neon connection strings. Azure CLI is already logged in. ACR Basic (about $5/month) is the documented cost exception.

## Results

`Publish:Target=aca-neon` reuses `AddAzureContainerAppEnvironment("aca-env")` and does not declare Flexible Server or `postgres-kv`. web-server gets the pooled connection as `ConnectionStrings__postgres-db` plus `Postgres__RetryCount=6` and `Postgres__RetryDelaySeconds=8`. Container apps publish with min replicas 0, max replicas 1, 0.25 CPU, and 0.5Gi. `dev deploy migrate --target aca-neon` runs the published bundle with the direct connection and opens no firewall rule. Run mode still uses the local Postgres container. Migrations use core Postgres types only; that check is in `neon-extension-tests` and `skills/tw-deploy/references/azure-container-apps-neon.md`.

Publish suites passed: `AcaNeonPublish_Given_` 6, `AcaPublish_Given_` 12, `ComposePublish_Given_` 10, `KubernetesPublish_Given_` 11. `dev-cli-tests` passed 268. `ganda repo audit` passes after the Nuru pin. `publish aca-neon --help` and `deploy migrate --help` show the aca-neon examples. `dev deploy --target aca-neon` refuses when the two secrets are missing and does not print a connection string.

A live ACA + Neon deploy did not run. `az account show` is logged in as StevenTCramer@gmail.comMSDN. The AppHost user secrets have no `Parameters:neon-pooled-connection-string` and no `Parameters:neon-direct-connection-string`, and no Neon environment variable is set. Expected monthly cost once deployed, from the operator guide: Neon Free $0, ACA consumption $0 while inside the free grant, Log Analytics usually inside 5 GB, ACR Basic about $5 per month (the exception to $0).

### How to validate

Smoke: from `tests/container-apps/aspire/aspire-tests`, `dotnet test -c Release -- --filter-class AcaNeonPublish_Given_`, then the same for `AcaPublish_Given_`, `ComposePublish_Given_`, and `KubernetesPublish_Given_`. From `tests/tools/dev-cli-tests`, `dotnet test -c Release`. `dotnet run tools/dev-cli/dev.cs -- publish aca-neon --help` and `dotnet run tools/dev-cli/dev.cs -- deploy migrate --help`. After the two AppHost user secrets exist: `dev publish aca-neon`, `dev deploy --target aca-neon`, `dev deploy migrate --target aca-neon`, then `curl -fsS https://<ingress-fqdn>/web-server/alive` and `curl -fsS https://<ingress-fqdn>/api/identity/entra/offered`.

Expect: the four publish suites and dev-cli-tests pass. aca-neon Bicep has `minReplicas: 0`, `cpu: json('0.25')`, `memory: '0.5Gi'`, the pooled parameter on web-server only, and no Flexible Server, Key Vault, `postgres-kv`, firewall rule, or direct Neon parameter. Compose, Kubernetes, and aca publish output stay free of `minReplicas: 0`, the Neon parameter names, and `Postgres__RetryCount`. Help lists `aca-neon` and the migrate example names the direct connection and no firewall rule. The live curls return HTTP 200 (the database route may wait while Neon wakes). That live half is not done until the two secrets are set.

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
- [x] One real deploy to ACA + Neon Free passes the smoke test; resulting monthly cost recorded (target $0 or noted exceptions like ACR)
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

Review round 1 is fixed when:

- The operator guide and the compute callback say publish emits no HTTP probe, and that `/api/health` opens Postgres so an uptime monitor must not call it.
- The CPU assignment does not swap culture. The publish test accepts `0.25` and rejects `0,25`.
- Default and aca-neon EF retries both call `PostgresRetryPolicy.Configure` with bounds. `RetryDelaySeconds` is documented as the maximum back-off. Parsing is tested.
- `dev deploy migrate --target aca-neon` does not require `az login` or the pooled string. It checks the direct string only.
- CI uploads the aca-neon Bicep, and the workflow comments name that upload.
- A pooled string may use `Server` as the host. `Channel Binding=Disabled` is refused. `Disable` is accepted.
- Tests that claim secrets stay off the deploy command pass the secret values in, and fail if those values are forwarded or printed.
- The `unused` Entra placeholder is described for every publish target. Enabling Entra while it is still set fails sign-in with `invalid_client`.

## Learnings

## Session

- 2026-10-21: unblocked by Steve (Neon user secrets set; live deploy approved; free-registry check added).
- 2026-10-21: live deploy, migrate, and smoke passed. Aspire 13.6.2 cannot pull from GHCR, so ACR Basic stayed. The resource group was deleted after the smoke because the registry bills; the Neon project was kept. `aspire deploy` runs as Production and does not load user secrets, so `dev deploy` copies them into the child environment. An empty Entra client secret is rejected by Container Apps, so an unset secret publishes the placeholder `unused`.

- Created: grok-bot (2026-10-12)

- 2026-10-21: `aca-neon` publish, deploy, migrate, open, and deprovision are wired. The pooled Neon reference is attached to web-server after `AddEFMigrations`, because that call snapshots the startup project's environment onto `ef-tool-web-migrations` and two `ConnectionStrings__postgres-db` references collide. The direct string stays off the Azure publish output; migrate reads it on the operator machine. `ganda repo audit --fix` pinned TimeWarp.Nuru and TimeWarp.Nuru.DevCli to 4.0.0-beta.5. No live deploy: AppHost user secrets do not contain the two Neon connection strings. Azure CLI is already logged in. ACR Basic (about $5/month) is the documented cost exception.
- Implement oracle: implemented by implementer-grok (grok, model grok-4.7), session not reported, max-turns 500, Blocked — 2026-10-21T03:27:27Z
- Implement oracle: implemented by implementer-grok (grok, model grok-4.7), session not reported, max-turns 500, Done — 2026-10-21T07:16:14Z
- Review oracle round 1 (2026-10-21): reviewer claude (claude-opus-5-5, headless), effort 3, roster general, plus a Sonnet general sub-reviewer (agent a270e187bf5cb516b). 0 bugs, 5 suggestions, 3 nits, all open. A fix pass was requested for M1–M8 (`review/round-1/merged.md`).
- Review oracle: review by reviewer-claude (claude, model claude-opus-5-5), session not reported, max-turns 200 — 2026-10-21T07:25:52Z
- 2026-10-21: review-round-1 fix pass. The CPU culture swap is gone because `BicepSyntax.Value` already formats `0.25` invariantly. Both retry paths call `PostgresRetryPolicy.Configure`; `RetryDelaySeconds` is the maximum back-off. aca-neon migrate checks only the direct string and does not probe `az login`. CI uploads the aca-neon Bicep. `Server` is a host alias; a URI and `Channel Binding=Disabled` are refused. Secret tests pass real values and reject them on argv and the plan. The `unused` Entra placeholder is documented for every publish target. No new live deploy.

## Results

`Publish:Target=aca-neon` reuses `AddAzureContainerAppEnvironment("aca-env")` and does not declare Flexible Server or `postgres-kv`. web-server gets the pooled connection as `ConnectionStrings__postgres-db` plus `Postgres__RetryCount=6` and `Postgres__RetryDelaySeconds=8`. Container apps publish with min replicas 0, max replicas 1, 0.25 CPU, and 0.5Gi. `dev deploy migrate --target aca-neon` runs the published bundle with the direct connection and opens no firewall rule. Run mode still uses the local Postgres container. Migrations use core Postgres types only; that check is in `neon-extension-tests` and `skills/tw-deploy/references/azure-container-apps-neon.md`.

`aspire deploy` runs with `--environment Production`, and that AppHost loads user secrets only in Development. `dev deploy` copies a user-secret subscription, location, resource group, Entra client secret, and the two Neon strings into the child process environment when those variables are unset. They stay off the command line and out of the plan. Azure Container Apps rejects an empty container-app secret, and `AddParameter(name, value, secret: true)` ignores configuration, so an unset `Parameters:entra-client-secret` publishes the placeholder `unused` (Entra stays off; it is not a credential).

GitHub Container Registry cannot replace ACR on this publisher. `AzureContainerAppEnvironmentResource.ContainerRegistry` in Aspire.Hosting.Azure.AppContainers 13.6.2 throws unless the registry is an `AzureContainerRegistryResource`. Pull is managed identity, with no password secret for `ghcr.io`. The registry SKU is Basic. Evidence is in `skills/tw-deploy/references/azure-container-apps-neon.md`.

Live deploy on 2026-10-21, subscription `40025bcc-ef1f-480f-9739-544719aeeb86`, location `eastus`, resource group `rg-tw-architecture-317-aca-neon`. Aspire reported the pipeline succeeded, including `provision-web-server-containerapp`. Ingress was `https://ingress.graydesert-89341d91.eastus.azurecontainerapps.io`. `dev deploy migrate --target aca-neon` applied the bundle on the direct Neon connection (17 migrations, from `20260804154245_InitialPostgresModel` through `20261020162201_AddSiteSettingsEntraTenantAcceptance`) and printed the connection as `<direct Neon connection string>`. Smoke, first attempt each:

- `GET /web-server/alive` → HTTP 200, body `Healthy`
- `GET /api/identity/entra/offered` → HTTP 200, body `{"offered":false}`

Monthly cost: Neon Free $0 (project kept). ACA consumption $0 inside the free grant while the apps existed (min replicas 0, 0.25 CPU, 0.5Gi). Log Analytics stays inside the 5 GB free ingestion for a deploy this short. ACR Basic is the exception, about $5 per month while the registry exists; Azure may prorate the hours it was up. `dev deprovision --target aca-neon` started deletion of `rg-tw-architecture-317-aca-neon` (registry, environment, container apps, Log Analytics, managed identity). That stops the registry bill. The Neon project was not deleted.

Publish suites passed on the first implement: `AcaNeonPublish_Given_` 6, `AcaPublish_Given_` 12, `ComposePublish_Given_` 10, `KubernetesPublish_Given_` 11. `dev-cli-tests` passed 270. `ganda repo audit` passed (32 checks, 0 failed). `dev publish aca-neon` wrote `artifacts/aspire-output/aca-neon` and its safety suite passed. The generated Bicep has `minReplicas: 0` and no Flexible Server or `postgres-kv`.

Review round 1 fix pass (2026-10-21), re-run from each test directory so its `global.json` selects Microsoft.Testing.Platform. `dev-cli-tests` `dotnet test -c Release` passed 273 (the three new facts are the migrate secret list, `Server` as a host, and refusal of `Channel Binding=Disabled` plus a URI). `web-infrastructure-tests` `--filter-class Resolve_Given_` passed 4. `AcaNeonPublish_Given_` passed 6 against Bicep generated in that run, including `cpu` `0.25`. `AcaPublish_Given_` passed 12, including Flexible Server and Key Vault still present on `aca`. `ganda repo audit` passed again (32 checks, 0 failed, 1 skipped). No second live deploy: the 2026-10-21 smoke already passed and the resource group was deleted.

### How to validate

Smoke: from `tests/container-apps/aspire/aspire-tests`, `dotnet test -c Release --filter-class AcaNeonPublish_Given_`, then the same for `AcaPublish_Given_`. From `tests/tools/dev-cli-tests`, `dotnet test -c Release`. From `tests/container-apps/web/web-infrastructure-tests`, `dotnet test -c Release --filter-class Resolve_Given_`. Run `ganda repo audit`. With the two Neon strings in the AppHost user secrets, a live check is `dotnet run tools/dev-cli/dev.cs -- deploy --target aca-neon --yes`, then `dotnet run tools/dev-cli/dev.cs -- deploy migrate --target aca-neon --yes`, then `curl -fsS https://<ingress-fqdn>/web-server/alive`. Do not point an uptime monitor at `/api/health`; that route opens Postgres and wakes Neon. Tear the resource group down with `dev deprovision --target aca-neon` when the registry should stop billing. Leave the Neon project.

Expect: those suites and the audit pass. aca-neon Bicep has `minReplicas: 0`, cpu `0.25`, and no Flexible Server, Key Vault, or `postgres-kv`. aca Bicep still has Flexible Server and Key Vault. `PostgresRetryPolicy.Resolve("6", "8")` is count 6 and a maximum delay of 8 seconds; blank or unparsable text keeps 4 and 2 seconds. aca-neon migrate's secret list is only `neon-direct-connection-string`, and that scope does not probe Azure. A pooled string with `Server=` and `Channel Binding=Disable` is accepted; `Disabled` and a `postgresql://` URI are refused without the secret being echoed. Deploy arguments and the plan omit the secret values and the `neon-` names. The plan names the subscription source. Migrate names the direct connection and applies the bundle without a firewall rule. `/web-server/alive` returns HTTP 200 `Healthy`. After deprovision the resource group is gone and the Neon project still exists.

### Review

Round 1 (effort 3, general roster): 0 bug, 5 suggestion, 3 nit. The fix pass is on this branch. Round 2 has not dispositioned the ledger. Artifacts: `review/review-framework.md` and `review/round-1/general.md`, plus `review/round-1/merged.md`, which stays the live ledger.

### Registry cost (Steve, 2026-10-21)

Checked. Aspire 13.6.2 cannot push this environment's images to `ghcr.io` or take a registry password as the pull credential. ACR Basic stayed, and the resource group was deleted after the smoke so that charge does not continue. Detail is under Results and in the operator guide.

## Review

- Round 1: Blocked — open M1, M2, M3, M4, M5, M6, M7, M8. Fix pass landed 2026-10-21; round 2 has not dispositioned them.

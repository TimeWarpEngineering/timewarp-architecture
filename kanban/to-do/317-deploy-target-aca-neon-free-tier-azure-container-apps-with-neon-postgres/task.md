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

- [ ] `dev deploy --target aca-neon` and `dev publish aca-neon` exist and appear in help with examples
- [ ] Publish output for `aca-neon` contains no Flexible Server, no postgres Key Vault, and min replicas 0
- [ ] Neon settings (pooled/direct strings, SSL, timeouts) are configuration for this target only; other targets' publish output unchanged (existing publish tests pass)
- [ ] `dev deploy migrate --target aca-neon` applies migrations via the direct connection
- [ ] Extension compatibility with Neon confirmed and recorded
- [ ] Run mode still uses the local Postgres container
- [ ] One real deploy to ACA + Neon Free passes the smoke test; resulting monthly cost recorded (target $0 or noted exceptions like ACR)
- [ ] Docs: operator guide for creating the Neon project, setting secrets, deploying, tearing down

## Links

- Neon Lakebase: https://neon.com/lakebase
- Neon pricing / Free plan: https://neon.com/pricing , https://neon.com/docs/introduction/plans
- Neon connection pooling: https://neon.com/docs/connect/connection-pooling
- Neon + EF Core / Npgsql: https://neon.com/docs/guides/dotnet-entity-framework
- ACA pricing (free grant): https://azure.microsoft.com/pricing/details/container-apps/
- Aspire ACA deployment: https://learn.microsoft.com/dotnet/aspire/deployment/azure/aca-deployment

## Learnings

## Session

- Created: grok-bot (2026-10-12)

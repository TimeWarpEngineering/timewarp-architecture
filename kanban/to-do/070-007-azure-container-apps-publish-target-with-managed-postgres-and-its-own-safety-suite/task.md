# Azure Container Apps publish target with managed Postgres and its own safety suite

Child of [[070-wire-aspire-publish-for-portable-deploy-compose-kubernetes]]. Depends on
[[070-006-manual-dev-deploy-and-dev-deprovision-commands-per-publish-target-never-ci-automated]].

## Description

The 070-005 decision (Steve, 2026-10-07) makes an **existing AKS cluster through the Kubernetes
target** the portable Azure default. This task adds **Azure Container Apps** (ACA) as a second,
Azure-only target. It fits small or idle apps (consumption billing, can scale to zero, built-in
HTTPS ingress) and is the target Steve already runs in production elsewhere.

Reference implementation: the crunchit repo AppHost (`AddAzureContainerAppEnvironment`,
`AddAzurePostgresFlexibleServer(...).WithPasswordAuthentication().RunAsContainer(...)`). Its
`dev deploy` and `dev deprovision` have deployed and destroyed successfully. **crunchit is
simpler**: it has one public `web-server` and no YARP, api or grpc. This template must keep its
multi-service topology.

## Depends on

- 070-006

## Requirements

- `Publish:Target=aca` (publish mode only; run mode unchanged). Add the `Aspire.Hosting.Azure.*`
  packages to CPM.
- **Only the YARP ingress is external** (`WithExternalHttpEndpoints` on YARP only). web, api and
  grpc are internal.
- **Aspire dashboard disabled** in the ACA environment. crunchit currently deploys it publicly.
  The safety suite must fail if it is present.
- Postgres: **Azure Database for PostgreSQL Flexible Server**, not a container on Azure Files.
  Run mode keeps the container (`RunAsContainer`). The password is a secret parameter, stored in
  Key Vault by Aspire.
- Migrations: decide and document how the idempotent SQL script, or the bundle, runs against
  Flexible Server. Do not use `EnsureCreated`.
- Entra and other secrets are parameters or secrets, never literals. No `UseMock`, no
  Development/Testing workload, no browser-log forwarding, no REPL.
- Safety suite `AcaPublish_Given_` over the generated Bicep/manifest, in-proc and with no Azure
  credentials. It covers the same rule set as Compose and Kubernetes, plus "no dashboard" and
  "only YARP external".
- `dev publish aca` and a CI publish check, with **no deploy in CI**.
- `dev deploy --target aca` and `dev deprovision --target aca`, extending 070-006.
  - Preflight: `az login`; subscription from `az account show` when `Azure__SubscriptionId` is unset.
  - Deprovision: when `aspire destroy` has no record, print `az group delete` and the purge of
    soft-deleted Key Vaults, behind confirmation.
- `skills/tw-deploy` gets an Azure Container Apps section covering when to choose it over AKS,
  the cost model, Flexible Server, and the deploy problems to expect:
  - the first deploy takes a long time;
  - Postgres can report "server is busy" until it is Ready, so retry;
  - soft-deleted Key Vault names must be purged before a redeploy can reuse them.

## Checklist

- [x] ACA environment behind `Publish:Target=aca`; only YARP external; dashboard off
- [x] Flexible Server in publish mode, container in run mode; migration path decided
- [x] `AcaPublish_Given_` safety suite
- [x] `dev publish aca` + CI publish check (no deploy)
- [x] `dev deploy` / `dev deprovision` ACA support
- [x] `tw-deploy` ACA section
- [x] Gates: `dev build` 0/0, aspire-tests, `dev template-smoke`, `ganda repo audit`

## Results

- **AppHost** (`program.cs`, `constants.cs`): `Publish:Target=aca` declares
  `AddAzureContainerAppEnvironment("aca-env").WithDashboard(enable: false)`. Only the YARP ingress gets
  `WithExternalHttpEndpoints`, so web, api and grpc have internal ingress. Postgres is
  `AddAzurePostgresFlexibleServer(...).WithPasswordAuthentication()` in the aca branch only. Run mode
  never reads `Publish:Target`, so it keeps the `AddPostgres` container with its volume and REPL. The
  password is a generated `@secure()` parameter. The connection string goes into the `postgres-kv` Key
  Vault, which web-server reads through a Key Vault-backed secret with its own managed identity. Key
  Vault is not the only copy of the password: Aspire's `WithReference` also puts it on web-server as
  two container-app secrets built from the parameter, `postgres-db-password` and `postgres-db-uri`
  (web-server does not read them). The firewall is Aspire's `AllowAllAzureIps` rule, which admits any
  Azure-hosted IP in any tenant; the password is the barrier, and VNet integration is the hardening
  step. The Entra settings use the same parameters as the other targets. CPM adds
  `Aspire.Hosting.Azure.AppContainers` and `Aspire.Hosting.Azure.PostgreSQL` at 13.6.0.
- **Migrations, decided:** the operator runs the published EF bundle
  (`efmigrations/web-migrations`) from their own machine with `--connection`, through a temporary
  firewall rule for their IP (the server otherwise admits Azure-hosted IPs only). The skill says
  where to find the deployed username and password. The idempotent SQL script with
  `psql` is the alternative. There is no `EnsureCreated`. The reasoning is in the AppHost Design region
  and `tw-deploy`.
- **Safety suite** `AcaPublish_Given_` (`tests/container-apps/aspire/aspire-tests/aca-publish-tests.cs`):
  it publishes the Bicep in-proc with no Azure credentials, or reads `TIMEWARP_ACA_OUTPUT`. It has 9
  facts:
  - only the ingress is external (each app's `ingress` block is sliced and `external` read; an
    ingress block without `external` fails)
  - no dashboard (no `dotNetComponents` or `AspireDashboard`)
  - no mock auth
  - no Development or Testing app
  - secrets are always a `secretRef`, a Key Vault reference or a `@secure()` parameter without a
    default, and no module contains a literal credential
  - Postgres is a Flexible Server with the connection string from Key Vault, and there is no Azure
    Files share
  - the postgres-derived secrets are pinned: web-server has exactly `connectionstrings--postgres-db`
    (Key Vault), `postgres-db-password` and `postgres-db-uri` (from the `@secure()` parameter); no
    other app has any
  - the Flexible Server firewall is pinned to the single `AllowAllAzureIps` 0.0.0.0–0.0.0.0 rule
  - run mode has no Azure resources

  I checked that the suite catches the problems it targets. It fails on a publish with the dashboard
  on, on an external web-server, and on an inline Entra secret. Review round 1: it also fails on an
  external web-server whose `external` is not the first ingress key, on an ingress block without
  `external`, on an extra postgres secret copy, and on a wider or extra firewall rule.
- **`dev publish aca`** is a new command. It is wired into the `dev workflow` PR/merge pipeline after
  `publish kubernetes`. `workflow.yml` uploads the Bicep and the SQL script. CI never deploys.
- **`dev deploy` / `dev deprovision --target aca`**:
  - The preflight requires `az login` (`az account show`).
  - The subscription comes from the `Azure__SubscriptionId` environment variable, else the AppHost
    user secret `Azure:SubscriptionId` (`dotnet user-secrets list --project <apphost>`), else
    `az account show`. Only the az fallback is passed to `aspire deploy` as `Azure__SubscriptionId`,
    so it never overrides a pinned subscription. The plan prints the source.
  - When `aspire destroy` fails, the verb prints `az group delete --name <resource-group>` (az asks
    for confirmation) and the purge of the soft-deleted Key Vault.
  - After a successful destroy it prints the purge.
  - Tests are added in `dev-cli-tests` (118/118 after review round 1).
- **`skills/tw-deploy`** has a new Azure Container Apps section covering when to choose it over AKS,
  the cost model, what gets provisioned, deploy, migrations, deprovision, and the deploy problems to
  expect (a slow first deploy, "server is busy" so retry, purging soft-deleted Key Vault names). The
  target matrix and the safety, secrets, migrations and ingress tables now include ACA.
- **Open question for the maintainer: the aca web-route host strategy** (AppHost
  `#region Open Questions`, also in the skill's "expect" list). Under `Publish:Target=aca` the http web
  hop does not hold: ACA upgrades internal ingress to `https://web-server.internal.<domain>`, and the
  web routes carry the client's original `Host` there. ACA routes internal ingress by host and .NET
  checks the TLS name against `Host`, so every web route (not only passkeys) is EXPECTED to fail
  through the ACA ingress until the maintainer decides. Candidate: aca-only web routes without the
  original-Host transform, with web-server's RP-ID host accessor reading `X-Forwarded-Host` — a
  security-design change that needs maintainer sign-off. `WithHttpsUpgrade(false)` does not help:
  ACA redirects internal plain http, and YARP passes that redirect back to the browser.
- **Gates run in this worktree:**
  - `dev build`: 0 warnings, 0 errors
  - aspire-tests: 34/34
  - `dev publish aca` (CLI publish + suite): passed
  - `dev template-smoke`: passed, all combinations
  - `ganda repo audit`: passed
  - `dev check-version`: OK (2.0.0-beta.20 is ahead of beta.19)
  - Review round 1 re-run: `dev build` 0/0; `AcaPublish_Given_` 9/9 (in-proc and via `dev publish
    aca`); dev-cli-tests 118/118; `ganda repo audit` passed. The full aspire-tests suite was not
    re-run (it boots AppHosts); only the aca suite changed.

### How to validate

**Smoke:**

```bash
dev build
cd tests/container-apps/aspire/aspire-tests && dotnet test -c Release -- --filter-class AcaPublish_Given_ && cd -
dev publish aca
ls artifacts/aspire-output/aca
grep -rn "external:" artifacts/aspire-output/aca/*/*.bicep
grep -rlc "AspireDashboard" artifacts/aspire-output/aca || echo "no dashboard"
cd tests/tools/dev-cli-tests && dotnet test -c Release -- --filter-class Targets_Given_ && cd -
```

**Expect:**

- `dev build` reports 0 warnings and 0 errors.
- `AcaPublish_Given_` shows 9/9 passed, with no Azure login needed.
- `dev publish aca` ends with "Azure Container Apps publish output is production-safe".
  `artifacts/aspire-output/aca` contains `main.bicep`, `aca-env/`, `ingress/`, `web-server/`,
  `api-server/`, `grpc-server/`, `postgres/`, `postgres-kv/` and `efmigrations/`.
- Only `ingress/ingress.bicep` shows `external: true`; every other app shows `external: false`.
- The dashboard grep prints "no dashboard".
- `Targets_Given_` passes, with `aca` among the valid targets.
- Maintainer only, after the web-route host decision: `az login && dev deploy --target aca`. Until
  then web routes through the ingress FQDN are expected to fail (the open question above); api and
  grpc routes are not affected.

## Notes

- Workers never deploy to Azure. The maintainer runs `dev deploy --target aca` after merge.
- Side finding for crunchit (not this repo): its Aspire dashboard is deployed publicly. It is
  worth checking there.

## Session

- Created: 2026-10-07 (cockpit, from the 070-005 Azure decision)
- 2026-10-07 implementer (ganda task work): implemented the ACA target, safety suite, `dev publish aca` + CI,
  deploy/deprovision ACA support, `tw-deploy` section; all gates green. Open question recorded (original-Host
  through ACA internal ingress) for the first maintainer deploy.
- 2026-10-07 implementer: review round 1 fixes (M1 docs-only — routing unchanged pending the maintainer's
  aca web-route host decision; M2–M7). Suite 9/9, dev-cli-tests 118/118.

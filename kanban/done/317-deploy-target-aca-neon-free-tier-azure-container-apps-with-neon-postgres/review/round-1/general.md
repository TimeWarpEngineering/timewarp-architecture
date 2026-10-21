# Round 1 — general
**Date:** 2026-10-21
**Scope reviewed:** `git diff master...HEAD` excluding kanban (28 files, about 1.5k lines): AppHost `program.cs`, `aca-neon-compute.cs`, `constants.cs`; `postgres-db-module-server.cs`; dev-cli deploy, preflight, migrate, publish, open, deprovision, workflow, and `neon-connection.cs`; aspire-tests and dev-cli-tests; `skills/tw-deploy`.

## Summary

The change adds `aca-neon` as a fourth `Publish:Target`. It reuses the `aca-env` Container Apps environment, declares no Flexible Server or `postgres-kv`, and models Neon as two secret parameters exposed as connection strings. web-server gets the pooled string and migrations get the direct one. dev-cli resolves both secrets from user secrets or the environment and copies them into the `aspire` child environment, never onto argv or into the plan. Migrate runs the bundle on the direct string with no firewall rule. The `aca` path is unchanged in behavior. Its `WaitFor` was only reordered, and the migration reference name resolves to `postgres-db` as before. Run mode is untouched because `publishTarget` is forced to compose there.

The task's acceptance criteria are met, and Results records a live deploy, migrate, and smoke. The smoke endpoint `/api/identity/entra/offered` really reads `ISiteSettingsStore`, so it does reach the database. `dev check-version` passes (2.0.0-beta.24 is ahead of nuget's beta.23).

No correctness bugs and no secret leaks were found. The findings are inaccurate design and doc claims in shipped files (skills ship in generated apps), tests weaker than what they claim, one duplicated policy, and a preflight that is stricter than migrate needs.

## Issues

### Issue 1 — Severity: suggestion
- File: skills/tw-deploy/references/azure-container-apps-neon.md:143; source/container-apps/aspire/projects/aspire-app-host/aca-neon-compute.cs:10
- Description: The guide says "ACA's probe uses this path, so probes do not keep Neon awake", and the Design region says "the ingress already probes /alive". No probe is published. `program.cs:139` (task 377 Design) records "no EndpointProbeAnnotation; Compose, Kubernetes and ACA emit none". The conclusion holds (nothing probes the database), but the stated reason is wrong, and the guide ships in generated apps.
- Suggestion: Reword both to say no HTTP probe is published, so nothing on a schedule touches Postgres. Optionally add a warning that `/api/health` (mapped anonymously in every environment at `web-server/program.cs:583`, including the `PostgresDbContext` check) wakes Neon, so an operator should not point an uptime monitor at it.
- Status: open

### Issue 2 — Severity: suggestion
- File: source/container-apps/aspire/projects/aspire-app-host/aca-neon-compute.cs:41-60; tests/container-apps/aspire/aspire-tests/aca-neon-publish-tests.cs:186
- Description: The Design region says that assigning `Resources.Cpu` under the invariant culture keeps the Bicep literal a dot. Azure.Provisioning stores a `BicepValue<double>` and writes Bicep later, during `aspire publish` composition, after the `finally` has restored the culture. The swap may do nothing. The guard regex `cpu:\s*(?:json\(\s*)?'?0[.,]25'?` accepts `0,25`, so the test passes on exactly the regression the hack claims to prevent.
- Suggestion: Tighten the regex to `0\.25`. Then either prove the swap matters (publish under a comma culture) or remove it and its Design claim.
- Status: open

### Issue 3 — Severity: suggestion
- File: source/container-apps/web/platform/postgres/postgres-db-module-server.cs:149-175
- Description: `NpgsqlOptions` rebuilds `EnableRetryOnFailure(count, delay, errorCodesToAdd: null)` instead of going through `PostgresRetryPolicy`. That policy's Design region says it lives in infrastructure so tests build contexts with "the exact production policy", and the override path breaks that. Nothing tests the override parsing (unset, valid, garbage). `RetryDelaySeconds` is passed as EF's `maxRetryDelay`, which caps an exponential back-off and is not a fixed delay. The constant's doc ("covers a cold Neon compute start") and the guide (`azure-container-apps-neon.md:128`) read it as a delay.
- Suggestion: Add a `PostgresRetryPolicy.Configure(builder, maxRetryCount, maxRetryDelay)` overload and call it from both paths. Add a small test of the configuration parsing. Name or document the knob as a maximum delay.
- Status: open

### Issue 4 — Severity: suggestion
- File: tools/dev-cli/services/aspire-deploy-preflight.cs:190-215 (and `PreflightScope.Migrate`, aspire-deploy.cs:142)
- Description: `dev deploy migrate --target aca-neon` uses only the direct Neon string. Preflight still requires both secrets, shape-checks the pooled one, and runs the `az account show` probe, because `IsAzureContainerApps` covers aca-neon in every scope. An expired `az` login or an unset pooled string blocks a migrate that needs neither.
- Suggestion: For the Migrate scope on aca-neon, skip the az probe and resolve and check only `neon-direct-connection-string`.
- Status: open

### Issue 5 — Severity: suggestion
- File: tools/dev-cli/endpoints/workflow-command.cs:17; .github/workflows/workflow.yml:226, 293-303
- Description: The edit changed "workflow.yml uploads all three outputs" to "uploads every publish output". `dev publish all` now publishes and gates aca-neon in CI, but workflow.yml uploads only the compose, kubernetes, and aca artifacts. The comment at workflow.yml:226 still lists "(compose, kubernetes and aca)". The new comment is false.
- Suggestion: Add an `aspire-aca-neon` upload step (Bicep only, since there is no SQL script beyond the bundle's), or make the comment name the three uploaded targets. Update the workflow.yml:226 comment as well.
- Status: open

### Issue 6 — Severity: nit
- File: tools/dev-cli/services/neon-connection.cs:156-158, 42
- Description: `IsChannelBindingDisabled` accepts `Disabled`, but Npgsql's `ChannelBinding` enum value is `Disable`. Preflight passes a string that Npgsql then rejects at runtime in the container. `Server=` (an Npgsql alias for Host) is reported as "must include a Host".
- Suggestion: Drop `Disabled`. Accept `server` as a host alias, or say in the guide that the key/value form with `Host=` is required (not the `postgresql://` URI Neon shows by default).
- Status: open

### Issue 7 — Severity: nit
- File: tests/tools/dev-cli-tests/aspire-deploy-tests.cs:129-135, 524; tests/container-apps/aspire/aspire-tests/aca-publish-tests.cs:241
- Description: `AcaNeonDeploy_Should_NotForwardTheConnectionStrings` passes an empty parameter list to `BuildDeployArguments`, and `BuildDeployPlanLines` never takes secrets, so the `ShouldNotContain("neon-pooled")` assertions cannot fail. The real guarantee is structural: secrets sit on `DeployPreflight.Secrets`, not `Parameters`. `Publish_Should_KeepAcaScaleAndPostgres` asserts only absences, but its name implies it checks the Flexible Server.
- Suggestion: Assert the structural property (resolved Neon secrets are not in `Parameters`, and `AddSecretParameterEnvironment` writes only `Parameters__*` env entries), or rename the tests to what they check.
- Status: open

### Issue 8 — Severity: nit
- File: skills/tw-deploy/SKILL.md:346; source/container-apps/aspire/projects/aspire-app-host/program.cs:400-418
- Description: The `unused` Entra client-secret placeholder applies in publish mode for every target, including compose and kubernetes `aspire deploy` (where the resolved parameter fills `.env` / values). SKILL.md scopes it to Azure Container Apps. No test pins the placeholder or the configured-value path. An operator who sets `Entra:Enabled=true` and forgets the secret now gets an AAD `invalid_client` at sign-in instead of an empty secret.
- Suggestion: Make SKILL.md say it applies to every published target, and mention the Entra-enabled consequence.
- Status: open

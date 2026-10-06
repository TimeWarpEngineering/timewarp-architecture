# Round 1 — general
**Date:** 2026-10-06
**Scope reviewed:** HEAD~1..HEAD

## Summary
The Compose environment is correctly publish-only (verified in the decompiled 13.6.0 `AddDockerComposeEnvironment`: in run mode the resource is never added to the model). Every new branch sits inside an existing flag block and is gated on `IsPublishMode`/`IsRunMode`, and the AppHost builds 0/0. The generated `docker-compose.yaml`/`.env` in `artifacts/aspire-output/compose` meets the production-safety list, and the guard tests would catch the main regressions (an extra host port, the dashboard, literal secrets, UseMock, a Development environment). There is one real defect: the documented by-hand migration command cannot authenticate against the published postgres. The other findings are robustness and accuracy suggestions.

## Issues
### Issue 1 — Severity: bug
- File: source/container-apps/aspire/projects/aspire-app-host/program.cs:116
- Description: The chosen migration path, `docker compose exec -T postgres psql -U postgres -d postgres-db -v ON_ERROR_STOP=1 < efmigrations/web-migrations.sql`, will fail with `fe_sendauth: no password supplied`. The published postgres service carries `POSTGRES_INITDB_ARGS: "--auth-host=scram-sha-256 --auth-local=scram-sha-256"` (Aspire.Hosting.PostgreSQL default, visible in the generated compose), so even the in-container unix-socket connection needs a password. psql does not read `POSTGRES_PASSWORD`, and `-T` (stdin is the SQL file) means it cannot prompt. The same command is repeated in task.md Results ("Migration path", "How to validate" step 3), so the maintainer's post-merge check would fail at the migration step.
- Suggestion: Supply the password from the container's own env, e.g. `docker compose exec -T postgres sh -c 'PGPASSWORD="$POSTGRES_PASSWORD" psql -U postgres -d postgres-db -v ON_ERROR_STOP=1' < efmigrations/web-migrations.sql`, and update the Design region and task.md to match.
- Status: open

### Issue 2 — Severity: suggestion
- File: source/container-apps/aspire/projects/aspire-app-host/program.cs:293
- Description: The ingress port rewrite assumes every `service.Ports` entry is a bare container port (`$"{placeholder}:{containerPort}"`). Aspire.Hosting.Docker `AddPorts` emits `"<exposed>:<internal>"` whenever an endpoint has an explicit `Port`. The `Ingress:Port`/`Ingress:HttpPort` `WithEndpoint` calls just below are not mode-gated, and `appsettings.Development.json` sets them to 63610/63620. Publishing under the Development environment (or with those keys in any config source) would therefore produce `"${INGRESS_PORT}:63620:5000"`, plus a second https mapping on the same host port, which is an invalid compose file. The guard only catches this if it runs with that config (`ShouldHaveSingleItem`), and the in-proc path pins Production.
- Suggestion: Gate the `Ingress:Port`/`Ingress:HttpPort` endpoint overrides on `IsRunMode`. Alternatively, build the mapping from the container part only (`entry.Split(':')[^1]`) and from the http endpoint only.
- Status: open

### Issue 3 — Severity: suggestion
- File: source/container-apps/aspire/projects/aspire-app-host/program.cs:182
- Description: The `Authentication__UseMock` forward is not publish-gated. If `Authentication:UseMock=true` is present in any AppHost config source during `aspire publish` (env var, user secrets, args), the compose output carries `Authentication__UseMock: "true"`. The Design region's "mock auth ... never emitted" therefore depends on config hygiene rather than code. CI's guard and web-server's Production fail-closed gate both mitigate this, but the requirement says "no mock auth" in the output.
- Suggestion: Add `builder.ExecutionContext.IsRunMode &&` to the forward condition, so publish never emits it regardless of config, and reword the Design bullet to match.
- Status: open

### Issue 4 — Severity: suggestion
- File: source/container-apps/aspire/projects/aspire-app-host/program.cs:154
- Description: With `yarp` off, a valid flag combination, web-server is external only in run mode and no service gets a host port. The published Compose stack therefore has no reachable entry point. The test Design codifies this as "no ingress means NO service may publish a host port", so the no-yarp output passes the guard but cannot be used on standalone hardware.
- Suggestion: Either keep web-server external when published with no ingress (it is then the edge) and adjust the ports rule, or record explicitly in the AppHost Design region that Compose publish requires the yarp ingress.
- Status: open

### Issue 5 — Severity: nit
- File: tools/dev-cli/endpoints/publish-compose-command.cs:13
- Description: The Design region says the rules live in the test suite because "it ships in generated apps; tools/ does not". `.template.config/template.json` excludes only `tools/agent-identity-cli/**` and `tests/tools/dev-cli-tests/**`, so `tools/dev-cli` (including this command) does ship in generated apps.
- Suggestion: Reword the rationale (for example, "one rule set shared by in-proc `dev test` and the CLI artifact check").
- Status: open

### Issue 6 — Severity: nit
- File: tests/container-apps/aspire/aspire-tests/compose-publish-tests.cs:179
- Description: The ".env secret keys must be empty" loop can never fail. `DockerComposePublishingContext` always saves the .env with `includeValues: false`, so this asserts Aspire's behaviour, not the AppHost's. Separately, the literal-secret check covers only keys containing PASSWORD/SECRET and values containing `Password=`. A literal embedded in a URI-shaped value (for example `POSTGRES_DB_URI: postgresql://postgres:<literal>@...`) would pass.
- Suggestion: Drop or comment the tautological loop. Optionally assert that `://user:` credentials in any value are `${...}` placeholders.
- Status: open

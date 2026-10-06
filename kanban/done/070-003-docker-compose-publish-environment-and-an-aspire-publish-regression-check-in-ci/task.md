# Docker Compose publish environment and an aspire publish regression check in CI

## Description

Child of 070. Add the Docker Compose publish target so `aspire publish` emits a runnable
`compose.yaml` for standalone hardware, and guard it with a CI check that runs `aspire publish`
and inspects the output.

## Requirements

1. **Package and environment.** Add `Aspire.Hosting.Docker` (13.6.x, the CPM pin moved forward
   with the other Aspire pins) and `builder.AddDockerComposeEnvironment(...)` in the AppHost.
   Confirm the API via the Aspire docs MCP (`deploy-to-docker-compose`, `docker-integration`);
   don't guess.
   - It must be **publish-only**: run mode (`dev run`) is unchanged.
   - Every template flag combination stays valid.
2. **Production-safe output** (the parent's cross-cutting list), verified by a test that runs the
   publish pipeline (or the model) and inspects `compose.yaml`:
   - no mock auth;
   - no browser-log forwarding;
   - no dashboard REPL;
   - secrets (postgres password, Entra) are `.env` parameters, not literals;
   - only the ingress is published to the host.
3. **Postgres.** Use a named volume for data and a secret password parameter, and define how
   migrations run in the Compose target. The options are the published migration bundle or
   script (task 155), run as a one-shot service or by hand; pick one and record it.
4. **Artifacts.** Decide whether generated output is committed or CI-only. The default is
   CI-only: upload it as a workflow artifact. Record the decision in the AppHost Design region. Do
   not create a `devops/` docs tree.
5. **CI regression check** in `.github/workflows/workflow.yml`, following the single-workflow,
   mode-aware `dev workflow` pattern; mirror sibling repos and don't invent a second workflow.
   - Run `aspire publish` for the Compose target.
   - Fail on errors and on any production-safety violation.
   - Upload the artifact.
   - Expose it through `dev`, for example `dev publish compose`, so it runs locally too.
6. **Runtime neutrality.** Nothing hard-codes the `docker` CLI (see task 277); go through the
   Aspire runtime setting.

## Checklist

- [x] `Aspire.Hosting.Docker` + `AddDockerComposeEnvironment` (publish-only; flags valid)
- [x] Production-safety test over the generated compose output
- [x] Postgres volume, secret parameter and migration path decided and implemented
- [x] Artifact policy decided (Design region)
- [x] CI publish check + `dev` entry point
- [x] Gates: `dev build` 0/0, `dev test` (affected suites — see Results), `dev template-smoke`, `ganda repo audit`
- [x] Do **not** start an AppHost or run containers; `aspire publish` (artifact generation) is fine
- [ ] Implementation review; host `open-pr`

## Notes

- After 070-002 (Dockerfiles gone), images come from the .NET SDK container build.
- Maintainer check after merge: on a Linux host or WSL, run `docker compose up` from the
  generated output and confirm the app comes up behind the ingress.

## Results

### What changed

- **Package + environment.** `Aspire.Hosting.Docker` 13.6.0 (CPM, next to the other 13.6.0 hosting
  pins) and `AddDockerComposeEnvironment("compose").WithDashboard(enabled: false)` in the AppHost.
  API confirmed from the aspire.dev docs (`deploy-to-docker-compose`, `docker-integration`) and the
  13.6.0 assembly (`Service.Ports`, `AsEnvironmentPlaceholder`, `PublishAsDockerComposeService`).
- **Publish-only.** Every new branch is gated on `ExecutionContext.IsPublishMode` / `IsRunMode` inside
  the existing flag blocks. Run mode keeps web-server external and gets no publish parameters; a
  run-mode model fact guards this.
- **Production-safe output** (the first raw publish failed 4 of the 6 rules; the guard reports each one):
  - only `ingress` has a host port: `${INGRESS_PORT}:5000` (`ingress-port` parameter). web-server
    is external in run mode only, and the compose dashboard is disabled (it published 18888);
  - no `UseMock`; no service runs as Development/Testing, so browser-log forwarding and the REPL
    stay off;
  - secrets are `.env` parameters: `POSTGRES_PASSWORD`, `ENTRA_CLIENT_SECRET` (secret), plus
    `ENTRA_ENABLED/TENANT_ID/CLIENT_ID/PUBLIC_ORIGIN` (non-secret settings; Entra defaults off).
- **Postgres.**
  - Fixed named volume `postgres-data` when published. The run-mode name hashes the AppHost path,
    so it would differ per checkout.
  - `POSTGRES_DB=postgres-db` creates the database on first initdb.
  - Password comes from the generated `postgres-password` secret parameter.
  - **Migration path (decision):** run the published idempotent SQL script **by hand**:
    `docker compose exec -T postgres sh -c 'PGPASSWORD="$POSTGRES_PASSWORD" psql -U postgres -d postgres-db -v ON_ERROR_STOP=1' < efmigrations/web-migrations.sql`.
    A one-shot service is out because the bundle is a host binary with no image to run in, and
    postgres has no host port. This is recorded in the AppHost Design region.
- **Artifacts (decision): CI-only, never committed.**
  - `dev publish compose` writes `artifacts/aspire-output/compose` (git-ignored). `aspire-output/` is
    also ignored now.
  - workflow.yml uploads `docker-compose.yaml`, `.env` and the SQL script as `aspire-compose-<run>`
    (1-day retention). The ~100 MB migration bundle is excluded.
  - Recorded in the AppHost Design region.
- **CI + dev.**
  - New `dev publish compose` (`tools/dev-cli/endpoints/publish-compose-command.cs` + `publish-group.cs`)
    first checks the Aspire CLI version (≥ 13.6), then runs
    `aspire publish --apphost … --output-path artifacts/aspire-output/compose --non-interactive`.
  - It then runs aspire-tests' `ComposePublish_Given_` with `TIMEWARP_COMPOSE_OUTPUT` pointing at that
    output. One rule set, so CI checks the exact files it uploads.
  - `dev workflow` PR/merge mode is now clean → build → test → publish compose.
  - workflow.yml installs `Aspire.Cli` 13.6.0 as a dotnet tool in the existing `ci` job. There is no
    new workflow.
- **Runtime neutrality.** `aspire publish` only writes files. Nothing added calls `docker`.
  Building and running the stack is `aspire do prepare-compose` / `aspire deploy`, which honour
  `ASPIRE_CONTAINER_RUNTIME`.
- **Guard:** `tests/container-apps/aspire/aspire-tests/compose-publish-tests.cs`.
  - In `dev test` it runs the publish pipeline in-proc: testing builder, `--operation publish --step
    publish-compose`, Production environment. No containers.
  - It parses `docker-compose.yaml` + `.env` with YamlDotNet.
  - Every rule depends on which services exist, so all flag combinations pass.
- Boyscout: `ganda repo audit --fix --checks memsearch-scaffold` removed the `.memsearch.toml`
  leftover and refreshed three `.githooks/*.cs` hooks (advisory warning → clean).

### Gates run (this worktree)

- `dev build`: 0 warnings / 0 errors.
- aspire-tests `ComposePublish` 7/7 and `PostgresVolumeModel` 4/4; dev-cli-tests 102/102.
  - The AppHost-booting `IngressSmoke` class and container-backed suites were **not** run locally
    (the task forbids starting an AppHost or containers); CI's `dev test` runs them.
- Negative control: pointing the guard at the pre-fix publish output fails 4 of 6 rules (ports, dashboard,
  secrets/.env, volume).
- `dev publish compose`: aspire publish 9/9 steps, safety suite 7/7 (CLI-output mode).
- `dev template-smoke`: SmokeDefault / SmokeNoPostgres / SmokeNoApi OK. The compose guard also
  passes 7/7 inside the generated SmokeNoPostgres and SmokeNoApi apps.
- `ganda repo audit`: clean. `dev check-version`: 2.0.0-beta.20 is new vs beta.19.

### How to validate

**Smoke:**

```bash
dev publish compose
cat artifacts/aspire-output/compose/docker-compose.yaml artifacts/aspire-output/compose/.env
cd tests/container-apps/aspire/aspire-tests && dotnet test -c Release -- --filter-class ComposePublish
```

**Expect:**

- `dev publish compose` ends with "Compose publish output is production-safe" and exits 0. The
  output directory holds `docker-compose.yaml`, `.env` and `efmigrations/web-migrations.sql`.
- In `docker-compose.yaml`, only `ingress` has `ports:` (`"${INGRESS_PORT}:5000"`), and there is no
  `compose-dashboard` service.
- postgres has `POSTGRES_PASSWORD: "${POSTGRES_PASSWORD}"`, `POSTGRES_DB: "postgres-db"` and the
  named volume `postgres-data`.
- web-server has `Authentication__Entra__ClientSecret: "${ENTRA_CLIENT_SECRET}"`, and no `UseMock`
  key appears anywhere.
- Every `.env` value is blank.
- The filtered aspire-tests run reports 7/7 passed.
- Maintainer check after merge (Linux/WSL, not workers):
  1. Run `aspire do prepare-compose --environment production`.
  2. Run `docker compose up -d` in the output.
  3. Apply the SQL script with the `docker compose exec … psql` line above.
  4. Browse `http://localhost:${INGRESS_PORT}`.

### Review disposition

- Effort 2, roster: general (subagent a060f10106a6b303a); 2 rounds (round 2 = fix re-verification).
- Final counts: bug 1 fixed; suggestion 2 fixed / 1 wontfix; nit 2 fixed; 0 open.
- Disposition: **accepted-exceptions** — M4 (no host port when the yarp flag is off) is by design:
  only the ingress is host-published; documented in the AppHost Design region.
- Fixes: migration command passes `PGPASSWORD`; ingress port pins and UseMock forward are run-mode
  only; dev-cli Design region corrected; compose tests drop a tautological `.env` loop and add a
  URI-credential placeholder check.
- Post-fix gates: `dev build` 0/0; ComposePublish 7/7; `dev publish compose` (runfile) passed.
- Paths: `review/review-framework.md`, `review/round-1/merged.md`, `review/round-2/general.md`,
  `review/disposition.md`.

## Session

- Created: 2026-10-03 (rewrite of 070)
- 2026-10-06: implemented (implement oracle) — Compose environment, safety guard, `dev publish compose`, CI wiring.
- 2026-10-06: review oracle — effort-2 general review, 5 fixed / 1 wontfix, disposition accepted-exceptions.
- Review oracle: review by implementer-claude (claude, model claude-opus-5-5), session not reported, max-turns 120 — 2026-10-06T12:12:24Z

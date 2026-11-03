# Refactor Aspire AppHost and web-server program.cs over 500 lines

## Parent

469

## Description

Both composition roots have grown past 500 lines. Move resource and service registration into
cohesive extension or builder types grouped by concern, leaving `program.cs` as a short
composition root. Keep every `#if` flag region intact (TWA0008 / TWA0010); extracted code
keeps the same flag guards.

## Files

- source/container-apps/aspire/projects/aspire-app-host/program.cs (942)
- source/container-apps/web/projects/web-server/program.cs (679)

## Requirements

- `program.cs` 500 lines or fewer, ideally far less.
- AppHost resource names stay equal to ServiceNames.
- `dev template-smoke --matrix` passes, since both files sit under `#if` flags.
- Aspire tests and web-server integration tests pass. `dev run` still starts.

## Goal (implementer restatement)

Shrink both composition roots (Aspire AppHost `program.cs`, 942 lines; Web.Server `program.cs`,
679 lines) to 500 lines or fewer by moving registration code, and the Design reasoning that goes
with it, into named types grouped by concern. Behaviour must not change.

Acceptance:

- Each `program.cs` is 500 lines or fewer and reads as a composition root.
- AppHost resource names still equal `ServiceNames` (TWA0007 still passes; names come from `constants.cs`).
- Every template flag guard is kept. Extracted code keeps the same guard, either with the same
  `#if` inside the type or by excluding a whole file in `template.json` for that flag.
- Registration order that matters is kept: web-server environment before `AddEFMigrations`
  (it snapshots env callbacks), the hosted AuthenticationStateProvider after Web.Spa's.
- `dev build` is clean. `dev template-smoke --matrix` passes. Aspire tests, web-server integration
  tests, and the dev-cli tests that scan AppHost source text pass.

## Checklist

- [x] Group AppHost registrations by concern into named types
- [x] Group web-server pipeline setup by concern into named types
- [x] Flag regions preserved
- [x] dev build, template-smoke --matrix verified (dev run not performed: see Results)

## Session

- Created: 66368815-cdd5-46be-bde8-a09d94e1cdc8 (2026-11-03)
- Implement oracle: implemented by implementer-claude (claude, vendor anthropic, model claude-opus-5-5, response model claude-opus-5-5), session e5f800bf-e891-4da5-9b2e-e312f0a76082, max-turns 500, Done — 2026-11-03T21:37:22Z

## Results

AppHost `program.cs` is 942 → 101 lines; Web.Server `program.cs` is 679 → 381 lines.

### AppHost (source/container-apps/aspire/projects/aspire-app-host/)

`program.cs` declares the project resources under their flags, adds the references between them,
and calls each concern in order. The Design notes moved with the code they explain:

- `publish-target.cs`: `PublishTarget` record. It reads Publish:Target and declares the
  Compose / Kubernetes / aca / aca-neon environment. It also holds the per-target Design notes.
- `web-server-resource.cs`: `WebServerResource.Configure`. Covers run-mode external endpoints,
  State DevTools, published Entra parameters, and the mock-auth opt-in.
- `postgres-resources.cs`: `PostgresResources.AddTo`. Declares the database for each target, the
  web-server reference, and web-migrations, including the Neon pooled reference applied after
  AddEFMigrations.
- `ingress-resource.cs`: `IngressResource.Add`. Covers the run-mode yarp project, the publish
  AddYarp image, passkey RP hosts, the health check, and the generated routes.
- `app-host-projects.cs`: `AppHostProjects`. It carries the declared projects to the ingress and
  aca-neon concerns. Each member sits under its own flag.
- `signoz-dashboard-link.cs`: `SigNozDashboardLink.Add`, moved unchanged.
- `aca-neon-compute.cs`: new `ApplyTo` replaces the three per-project aca-neon blocks.

Flag guards: `#if` blocks stay inside the types (ingress routes, ApplyTo, AppHostProjects).
Files that only make sense under one flag are excluded in `.template.config/template.json`:
web-server-resource.cs and postgres-resources.cs under !web, postgres-resources.cs under
!postgres, and ingress-resource.cs under !yarp. The smoke matrix confirmed those files are absent
from the matching flag-off apps. Order is unchanged. web-server's environment and its api/grpc
references are still attached before AddEFMigrations snapshots them.

One dead branch was dropped: the publish-mode `Ingress:Port` / `Ingress:HttpPort` endpoint
overrides. Those pins are computed as null outside run mode, so the branch could never run. The
publish-mode compose branch `else if (IsPublishMode)` is now `else`, because that method only runs
in publish mode.

### Web.Server (source/container-apps/web/projects/web-server/)

- `web-server-http-clients.cs`: `WebServerHttpClients`. Service-discovery named clients and
  ServiceDiscoveryAddress (task 312 / 205-003 Design).
- `web-server-authentication.cs`: `WebServerAuthentication`. Covers policies, schemes
  (cookie events, agent token, mock, Entra), `ConfigurePermissionEvaluation` (147-004 / 182 block,
  still at its original position), and `PreferHostedIdentitySessionState` (task 183, still after
  Web.Spa).
- `web-server-settings.cs`: `WebServerSettings`. Validated options.
- `web-server-payment-services.cs`: `WebServerPaymentServices`. TimeWarp.402 demo services.
- `web-server-state-dev-tools.cs`: `WebServerStateDevTools.AddExport`.
- `brand-stylesheet-endpoint.cs`: `BrandStylesheetEndpoint.Map`.

Registration order in ConfigureServices is unchanged.

### Other edits

- dev-cli-tests (`aspire-deploy-tests.cs`, `deploy-operate-tests.cs`) used to read the AppHost
  `program.cs` as text. They now read every top-level AppHost `.cs` file, and the Kubernetes
  value-less parameter branch is read from `publish-target.cs`.
- Pointers to the "AppHost program.cs Design region" now name the concern files. Updated in
  skills/tw-deploy/SKILL.md, site-settings-seed-hosted-service-server.cs, aspire-tests comments,
  aspire-deploy.cs, and the AppHost csproj.

### Gates

- `dev build`: 0 warnings, 0 errors.
- `dev test --group fast`: 14/14 projects passed (dev-cli-tests 342 tests, 0 failed).
- `dev test --group integration`: 8/8 projects passed. These include aspire-tests (compose,
  kubernetes, aca, and aca-neon publish suites, ingress smoke, postgres volume, SigNoz, and DevTools
  model tests) and web-server-integration-tests.
- `dev template-smoke --matrix`: SmokeDefault, NoPostgres, NoApi, NoWeb, NoGrpc, and NoYarp all
  OK. "Template smoke SUCCEEDED".
- `dev lint-skills`: 9/9 passed.
- `ganda repo audit`: passes. The one advisory is kanban-landed: other tasks' claims have no
  worktree. That is board state outside this task.
- `dev run` was **not performed**. Task-worktree AppHosts share the master UserSecretsId, and
  starting one can rewrite the shared postgres password. The closed-box Aspire.Hosting.Testing
  suites boot the same AppHost model instead. The secrets.json hash was the same before and after
  the test runs.

### How to validate

Smoke:

```bash
wc -l source/container-apps/aspire/projects/aspire-app-host/program.cs source/container-apps/web/projects/web-server/program.cs
./bin/dev build
./bin/dev test --group integration
./bin/dev template-smoke --matrix
```

Expect: both `program.cs` files are 500 lines or fewer (101 and 381). The build reports 0
warnings. All integration projects pass, aspire-tests included. Every smoke variant prints OK and
the run ends with "Template smoke SUCCEEDED". In the SmokeNoYarp app, aspire-app-host has no
ingress-resource.cs. In SmokeNoWeb it has neither web-server-resource.cs nor postgres-resources.cs.
In SmokeNoPostgres it has no postgres-resources.cs.
Optional, operator-run in the master checkout: `dev run` starts with every resource healthy.


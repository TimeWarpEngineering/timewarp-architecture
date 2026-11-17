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
- Review oracle round 1 (2026-11-04): claude-opus-5-5, headless ganda task work, effort 3 (by-diff), roster general
  (two read-only general passes: apphost a9681a5e9a4970e87, web-server ac84f0f4e80731943). Outcome: fix pass needed
  for open M1–M5.
- Review oracle: review by reviewer-claude (claude, vendor anthropic, model claude-opus-5-5, response model claude-opus-5-5), session 4fd94995-2da9-401b-b729-1f6df0bc1893, max-turns 200, artifacts general.md, merged.md — 2026-11-03T21:44:38Z
- Review oracle round 2 (2026-11-04): claude-opus-5-5, headless ganda task work, effort 2 (by-diff, 276 lines),
  roster general (one pass, no subagents), turn cap 120. Disposition: accepted-exceptions, 0 open.

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

### Fix pass (review round 1 → round 2, 2026-11-04)

- M1: eight Purpose/Design regions outside web-server `program.cs` now point at
  `WebServerSettings.ConfigureServices` or `WebServerAuthentication` instead of the removed
  `Program.ConfigureSettings` / `Program.ConfigureAuthentication`. sign-in-options-validator-application.cs
  ("web-server ConfigureSettings") was fixed too. A grep for the old names across source, tests and
  skills is empty.
- M2: web-server-state-dev-tools.cs says `AddExport`. web-server-authentication.cs says
  `AgentTokenOptions (WebServerSettings)`.
- M3: the two postgres-resources.cs "(see the Design region)" pointers now name PublishTarget's
  Design region in publish-target.cs (the aca-neon paragraph and the aca Postgres bullet).
- M4 (wontfix): the RCS1163 finding is real when api, grpc and web are all off. Building the
  AppHost with `-p:DefineConstants=postgres` and with `yarp;postgres` shows it. The yarp-on form of
  that combination already failed on master (IDE0005 on `Aspire.Hosting.Yarp.Transforms`), and no
  smoke variant gates it. Supporting a no-service app is a flag-set decision that needs its own
  matrix variant. Details are in review/round-1/merged.md M4.
- M5: blank line after `namespace …;` in web-server-http-clients.cs, web-server-payment-services.cs,
  and brand-stylesheet-endpoint.cs.
- Gates: `./bin/dev build` 0 warnings / 0 errors. `./bin/dev test --group fast` 14/14 projects.
  `ganda repo audit` exit 0: 35 passed; the one warning is kanban-landed for other tasks' claims
  (board state outside this task). Only comments and whitespace changed, so
  template-smoke and the integration group were not re-run.

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


### Review

- Round 1 (2026-11-04). Effort 3, roster general: two read-only passes (AppHost and Web.Server).
- Counts (round 1): 0 bug; suggestion 3 open; nit 2 open; 0 fixed; 0 wontfix.
- Disposition (fix pass, 2026-11-04): M1, M2, M3 and M5 are fixed. M4 is wontfix, with the reason in merged.md. Counts after the fix pass: suggestion 3 fixed; nit 1 fixed, 1 wontfix; 0 open.
- Round 1 findings were:
  - M1–M3 are stale pointers in comments: `Program.ConfigureSettings` / `Program.ConfigureAuthentication` in 8 files outside web-server program.cs, old member names in two moved files, and postgres-resources.cs "see the Design region" pointing at reasoning that now lives in publish-target.cs.
  - M4: possible RCS1163 when api, grpc and web are all off (unverified).
  - M5: missing blank lines after the namespace in 3 files.
- No behaviour change was found. Order and flag guards were checked statement by statement against the old files. The dead-code claims were confirmed.
- Gates the reviewer re-ran: `dev build` 0/0, and `dev test --group fast` 14/14 (dev-cli-tests 342/342). Not re-run: template-smoke --matrix and the integration group.
- Paths: review/review-framework.md, review/round-1/general.md, review/round-1/merged.md.
- Round 2 (2026-11-04). Effort 2, roster general (one pass). It reviewed the fix delta
  `8edf0ea9a..406fad697`, which is comments and whitespace only. M1, M2, M3 and M5 are confirmed
  fixed: a grep for the old `Program.ConfigureSettings` / `Program.ConfigureAuthentication` names
  is empty, and the remaining "see the Design region" pointers resolve in their own files. The M4
  wontfix rationale is accepted. No new findings. Gate re-run: `./bin/dev build` 0 warnings,
  0 errors.
- Final counts: bug 0; suggestion 3 fixed; nit 1 fixed, 1 wontfix; 0 open.
- **Disposition: accepted-exceptions.** The one exception is M4 (nit): the flag combination with
  api, grpc and web all off is unsupported and ungated, and its yarp-on form already failed on
  master.
- Still open before done: the Requirements item "`dev run` still starts" needs an operator run in
  the master checkout (see Learnings).
- Paths: review/round-2/general.md, review/round-2/merged.md, review/disposition.md.

## Learnings

- Misread (review round 1): the task's Requirements say "`dev run` still starts". The Goal (implementer restatement) left that requirement out. It listed dev build, template-smoke --matrix, Aspire tests, web-server integration tests and dev-cli tests only. Results record that dev run was deliberately not performed, because task-worktree AppHosts share the master UserSecretsId. So the requirement is still open and needs an operator run in the master checkout before done. It was not dropped by the restatement.

## Review

- Round 1: Blocked — open M1, M2, M3, M4, M5
- Fix pass (2026-11-04): M1, M2, M3, M5 fixed; M4 wontfix. Round 2 is next.
- Round 2 (2026-11-04): Done. 0 open. Disposition is accepted-exceptions (M4 wontfix).
- Round 2: Done

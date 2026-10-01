# Update Aspire 13.5.4 to 13.6 and adopt what fits

## Description

Move the template off Aspire 13.5.4 and onto 13.6, the latest 13.6.x on nuget.org. Then adopt
the 13.6 features that improve the generated app's developer loop. Release notes:
https://devblogs.microsoft.com/aspire/whats-new-aspire-13-6/

Current pins, all 13.5.4 unless noted:

- `Aspire.AppHost.Sdk/13.5.4` in `aspire-app-host.csproj`
- `Directory.Packages.props`:
  - `Aspire.Hosting.Yarp`
  - `Aspire.Hosting.PostgreSQL`
  - `Aspire.Hosting.EntityFrameworkCore` (13.5.4-preview.1.26464.4)
  - `Aspire.Hosting.Testing`
  - the other `Aspire.*` entries in the "Aspire Packages" group

Also check the CLI version, the dashboard SDK and the service-defaults packages.

## Requirements

1. **Bump every Aspire pin together** to the latest 13.6.x, or the matching 13.6 preview for
   packages that only ship previews (EntityFrameworkCore). Pins move forward only, never
   backward or split. If a package was renamed or split, migrate to the new package.
2. **`aspire update` gotchas** (these bit us before):
   - It reflows csproj attributes. Revert any formatting-only churn.
   - It skips `Aspire.Hosting.Testing`. Bump that one by hand.
   - `aspire agent init` writes a telemetry hook into `~/.claude/settings.json`. Do not run it.
   - Prefer editing the pins directly and running `aspire update` only to cross-check.
3. **Adopt, in this PR, when low-risk:**
   - **`WithRepl()`** on the Postgres resource, so the dashboard gets an authenticated psql
     REPL command. The REPL is read-only in historical runs. Development only.
   - **Dashboard persistence** (resource snapshots and telemetry kept after the AppHost stops,
     10 runs, pinning). Confirm the default behavior, and note it in the owning skill. It helps
     postmortems such as today's WASM assertion.
   - **`--launch-profile`** support on `aspire run` / `aspire start`. Check whether `dev run`
     should pass it through.
4. **Evaluate and record, don't adopt:**
   - Prerelease `Aspire.Hosting.Dotnet` / `AddDotnetProject()`: coordinated restore, and
     multi-threaded MSBuild on the .NET 11 SDK. This repo is on .NET 10, and the package is
     prerelease.
   - `WithVolume(env:)` path standardization, for the postgres data volume.
   - `aspire stop --force --volumes`: does it fit a `dev db` flow?

   For each item, write an adopt, skip or later recommendation in Notes and leave it as an open
   question for Steve.
5. **Coordinate with task 261** (browser logs to the dashboard, investigating `WithBrowserLogs`).
   Rebase onto it if it merged first, and re-check that its wiring still works on 13.6. Do not
   duplicate its work.
6. **Keep every regression gate green.** That covers the in-proc lane, the closed-box
   `aspire-tests` (Aspire.Hosting.Testing) and `dev template-smoke`. The template ships the
   AppHost. Keep the template flag regions intact.

## Checklist

- [x] All Aspire pins on 13.6.x (SDK, hosting packages, Testing, EF preview, service defaults)
- [x] No csproj churn from `aspire update`; `aspire agent init` not run
- [x] `WithRepl()` on Postgres (Development); dashboard persistence noted in the skill
- [x] `--launch-profile` / `dev run` pass-through evaluated
- [x] Evaluate-only items recorded with recommendations (open questions for Steve)
- [x] Task 261 wiring re-checked on 13.6 (if merged)
- [x] Purpose/Design regions reconciled where AppHost code changes
- [x] Gates: `dev build` 0/0, `dev test` (including aspire-tests), `dev template-smoke`,
      `ganda repo audit`, `dev check-version` if the template version must bump for release
- [x] Do **not** start the AppHost manually (`dev run`, `aspire run`): it shares the
      maintainer's user secrets. The closed-box test suites are fine. Record the dashboard and
      REPL check as not performed.
- [ ] Implementation review; host `open-pr`

## Session

- Created: 585494 (2026-10-01)
- 2026-10-01 implement (ganda task work, Claude Opus 5.5): pins → 13.6.0, WithRepl, skill notes,
  evaluations recorded; permission-evaluator single-flight test made deterministic (template-smoke
  flake). Gates green. AppHost not started (dashboard/REPL check not performed).

## Notes

- Memory discipline: other workers may be building at the same time. Run builds and tests
  serially and call `dotnet build-server shutdown` before finishing.
- Maintainer check after merge:
  1. Run `dev clean`, then `dev run`.
  2. The dashboard shows the Postgres REPL command.
  3. Stop the AppHost, reopen the dashboard, and confirm the previous run is selectable.

### Task 262 findings (implement, 2026-10-01)

- **Versions on nuget.org:** `Aspire.AppHost.Sdk` and `Aspire.Hosting.*` (Yarp, PostgreSQL,
  Testing) are at 13.6.0; `Aspire.Hosting.EntityFrameworkCore` is at 13.6.0-preview.1.26479.8.
  The dashboard SDK (`Aspire.Dashboard.Sdk.linux-x64`) and DCP come from the AppHost SDK and
  restore at 13.6.0. Service defaults reference no `Aspire.*` package (Microsoft.Extensions.* and
  OpenTelemetry only), so nothing to bump there. No package renames or splits on this train.
- **CLI:** the machine-global `aspire` CLI is 13.5.4. It is not repo-pinned (no
  `.config/dotnet-tools.json` entry), and `aspire update --self` changes a shared machine tool
  while other workers run, so it was not updated here. The pins were edited by hand.
  `aspire update` was not run, and neither was `aspire agent init`.
- **Dashboard persistence (on by default; documented):** Aspire.Hosting 13.6
  (`DashboardEventHandlers`) sets `ASPIRE_DASHBOARD_PERSISTENCE_MODE=Run` on the
  AppHost-managed dashboard unless `ASPIRE_DASHBOARD_PERSISTENCE_MODE` or
  `Aspire:Dashboard:PersistenceMode` is set. This also holds with `AspireUseCliBundle=false`
  (the NuGet dashboard), so the AppHost needs no change. Data directory override:
  `Dashboard:Data:Directory` / `ASPIRE_DASHBOARD_DATA_DIRECTORY`. `Aspire.Hosting.Testing` sets
  `DisableDashboard = true`, so test runs never consume the 10-run history. Documented in
  `skills/tw-blazor`, in the browser-logs section (the postmortem use case).
- **WithRepl (adopted):** `postgres.WithRepl()`, gated on `builder.Environment.IsDevelopment()`.
  The API is run-mode only and runs with the server credentials, so it stays off any shared
  dashboard. Mentioned in `skills/tw-aggregate-pattern`.
- **`--launch-profile` / `dev run`: recommend later, not adopted.** The AppHost has two
  profiles: `https`, which is first and so the default, and `http`. `dev run` already forces
  `ASPNETCORE_ENVIRONMENT=Development`, and the installed 13.5.4 CLI rejects `--launch-profile`.
  Revisit if a second meaningful AppHost profile appears, for example a mock-auth profile. At
  that point the pass-through would be an optional `dev run --launch-profile <name>`.
  **Open question for Steve.**
- **`Aspire.Hosting.Dotnet` / `AddDotnetProject()`: recommend later.** It is still
  13.6.0-preview.1, and multi-threaded MSBuild needs the .NET 11 SDK while the repo is on .NET 10.
  Adopting it means rewriting every `AddProject<Projects.*>` call, the `Projects.*` typed
  `ProjectReference` wiring, and the assumptions in the TWA0007 resource-name analyzer.
  Re-evaluate when the repo moves to a stable .NET 11 SDK. **Open question for Steve.**
- **`WithVolume(env:)` for the postgres data volume: recommend skip for now.**
  `WithDataVolume()` is the integration's own API, and postgres reads `PGDATA` from its fixed
  image path, so env-bound path standardization adds nothing. It targets workloads that read
  their storage path from an env var (compute-environment publish). The
  `Postgres:UseDataVolume=false` test gate stays as is. **Open question for Steve.**
- **`aspire stop --force --volumes` in `dev db`: recommend later, as a documented hint rather
  than a `dev db` verb.** It removes every volume Aspire created for the AppHost, which is broader
  than `dev db reset` (drop plus migrate inside the running server). It also needs CLI 13.6 and
  stops the maintainer's running AppHost. A possible later home is a `dev db nuke --yes` that
  needs no running server. **Open question for Steve.**
- **Task 261 (merged first, already on base):** browser logs are forwarded through
  `POST api/browser-logs` into web-server structured logs (category `Web.Spa.Browser`), not
  through `WithBrowserLogs`. The AppHost has no 261 wiring that 13.6 could break. The web-server
  suites that cover it are green on 13.6 (web-server-integration 277/277, web-spa-integration
  124/124).
- **Boyscout:** template-smoke (SmokeNoApi) failed once on
  `PermissionEvaluator_Given_.ConcurrentChecks_Should_SingleFlightStoreExpansion`, with
  TotalGets 2 and MaxConcurrent 1. A `Task.Delay(25)` was the only thing holding the first flight
  open while the later checks were issued. Under load, that flight finished and was evicted before
  the later checks arrived. The counting store now waits on a gate that the test releases after
  issuing every check, which is deterministic and still detects overlap.

## Results

- All Aspire pins moved forward together to 13.6.0: AppHost SDK, Hosting.Yarp, Hosting.PostgreSQL,
  Hosting.Testing, and Hosting.EntityFrameworkCore 13.6.0-preview.1.26479.8. The dashboard SDK
  and DCP follow the SDK to 13.6.0. There was no `aspire update` churn because it was not run.
- `WithRepl()` is on Postgres in Development (`program.cs`, Design region reconciled). The
  `Microsoft.Extensions.Hosting` global using is nested under the web and postgres flags to stay
  IDE0005-clean in every flag combination.
- Dashboard persistence is default-on (`Run`) for the AppHost-managed dashboard. This was verified
  in the Aspire.Hosting 13.6 source and documented in `tw-blazor`. The REPL is noted in
  `tw-aggregate-pattern`.
- `--launch-profile`, `AddDotnetProject`, `WithVolume(env:)` and `aspire stop --volumes` were
  evaluated. Recommendations are in Notes as open questions for Steve.
- Boyscout: the permission-evaluator single-flight test no longer depends on timing.
- Gates: `dev build` passed (warnings as errors). `dev test` passed every suite, including
  aspire-tests 7/7 on Aspire.Hosting.Testing 13.6.0. `dev template-smoke` SUCCEEDED.
  `dev check-version` reports 2.0.0-beta.20 as new, so no bump was needed. `ganda repo audit`
  passes; the `bin-dev` fix rebuilt the untracked `bin/dev`.
- Not performed: starting the AppHost (`dev run`), the dashboard REPL check, and the
  persistence check, because the AppHost shares the maintainer's user secrets.

### How to validate

**Smoke:** `dev build && dev test` (aspire-tests drives the real 13.6 AppHost through
Aspire.Hosting.Testing), then `dev template-smoke`. Maintainer, after merge: `dev clean`,
`dev run`, open the dashboard, find the `postgres` resource and its REPL command, run
`select 1;`. Then stop the AppHost, reopen the dashboard, and select the previous run.

**Expect:** build 0/0; every suite green (aspire-tests 7/7); template smoke SUCCEEDED. In the
dashboard, Postgres shows a REPL command that opens an authenticated psql shell in the
terminal dock, and the previous run (resources, logs and traces) stays selectable after the
AppHost stops.

# Reduce default logging levels so web-server does not emit debug EF SQL in every environment

## Incident (2026-10-14)

On the maintainer's machine `/tmp` filled to 100%. It is a 32 GB tmpfs, so it lives in RAM. Agent
sessions and `ganda` output failed. The cause was **one 26 GB stdout log from the `dev run`
(Aspire) web-server resource**, streaming EF Core debug SQL. The log was truncated in place to
recover, but a long-running `dev run` refills it.

## Root cause (cockpit)

- `source/container-apps/web/projects/web-server/appsettings.json` (the **committed base config**)
  sets `Logging:LogLevel:Default` to **`Debug`**. Only `Microsoft.AspNetCore` is raised to
  Information.
- `appsettings.Development.json` and `appsettings.Production.json` set **no** `LogLevel` override.
- So **every environment, Production included, logs at Debug**: EF Core command text per query
  (`Microsoft.EntityFrameworkCore.Database.Command`), plus everything else at Debug.
- Under Aspire, resource stdout is captured to files (under `/tmp` here) and fed to the dashboard.
  It grows without bound.

## Requirements

1. **Investigate.** Inventory logging config for every host: web-server, api-server, grpc-server,
   yarp, the AppHost, plus any `ILoggingBuilder` / `SetMinimumLevel` / OpenTelemetry log settings
   in code (`aspire-service-defaults`) and EF `LogTo` / `EnableSensitiveDataLogging` /
   `EnableDetailedErrors`. Measure the log volume of a short realistic session before and after
   (for example web-server stdout bytes during the IngressSmoke flow, or a scripted browse), and
   record the numbers.
2. **Sane defaults:**
   - base `appsettings.json` (every environment): `Default: Information`; `Microsoft.AspNetCore:
     Warning`; **`Microsoft.EntityFrameworkCore: Warning`** (and `.Database.Command: Warning`);
     other noisy categories (`System.Net.Http.HttpClient`, `Microsoft.Extensions.Http`, YARP,
     `Grpc`) at Warning unless there's a reason. Record each choice.
   - `appsettings.Development.json`: a little more detail for local work (for example the app's
     own namespaces at Debug), but **EF SQL stays off by default**. A developer opts in by setting
     `Logging:LogLevel:Microsoft.EntityFrameworkCore.Database.Command=Information` in user secrets
     or an environment variable. Document that switch in a Design region and the relevant skill.
   - Production: never Debug. `EnableSensitiveDataLogging` must never be on outside Development.
     Add a publish safety check for it if it's cheap. The 070-series safety suites could assert the
     published env has no `Logging__LogLevel__Default=Debug`.
3. **Consistency:** apply the same baseline to api-server, grpc-server and yarp, and to generated
   apps (it's template content). Prefer one shared place, such as service defaults, if that's
   cleaner than four appsettings files. Record the choice.
4. **Test host / CI:** check that test hosts and CI don't depend on Debug output. If a test
   asserts on a Debug log line, change it to capture at the needed level explicitly. Watch the
   IngressSmoke boot-seed log watch (task 270) and anything grepping web-server console output.
5. **Guard:** a test that the committed `appsettings.json` of each host does not set `Default` below
   Information, and that the EF command category is not below Warning in base config, so this
   can't silently regress.

## Checklist

- [x] Inventory + before/after log-volume numbers
- [x] Base/Development/Production levels set per host (EF SQL off by default; opt-in documented)
- [x] Sensitive-data logging never outside Development (+ publish safety assertion if cheap)
- [x] Tests not dependent on Debug; regression guard on committed levels
- [x] Gates: `dev build` 0/0, `dev test`, `dev template-smoke`, `ganda repo audit`

## Notes

- Workers must not touch the maintainer's running `dev run` or its log files.
- Until this merges, the maintainer can stop the current `dev run`, or set
  `Logging__LogLevel__Default=Information` for web-server, to stop the growth.

## Results

### Inventory (before)

| Host | Base `appsettings.json` | Development | Production | Other |
|------|-------------------------|-------------|------------|-------|
| web-server | **Default Debug**, AspNetCore Information | none | none | `Kubernetes_Docker`: Default Debug |
| api-server | Information / AspNetCore Warning | **Default Debug, AspNetCore Debug** | none | `Kubernetes_Docker`: Debug |
| grpc-server | Information / AspNetCore Warning | **Default, AspNetCore, Grpc Debug** | none | `Kubernetes_Docker`: Debug |
| yarp | Information / AspNetCore Warning | **Default, AspNetCore Debug** | none | `Kubernetes_Docker`: Debug |
| aspire-app-host | Information / AspNetCore + Dcp Warning | same | n/a | — |
| web-spa (client, wwwroot) | **Default Debug**, System/Microsoft Information | none | `{}` | — |
| web-server / web-spa integration-test appsettings | **Default, AspNetCore, Grpc Debug** | | | |

Code: `aspire-service-defaults` only adds the OpenTelemetry logger (no `SetMinimumLevel`, no
filters). EF: `PostgresDbModule` uses `UseNpgsql` only. No `LogTo`, `EnableSensitiveDataLogging` or
`EnableDetailedErrors` anywhere in `source/`. Nothing references the `Kubernetes_Docker` environment.

### Log volume (web-server console during one IngressSmoke run, 10 facts, about 2.5 min)

| | lines | bytes | EF `Database.Command` lines | `dbug:` lines |
|--|------:|------:|------:|------:|
| before | 11,703 | 1,198,203 | 658 | 4,624 |
| after | 876 | 96,199 | 0 | 288 |

About 92% less. The remaining Debug lines (Development only) are the app's own pipeline tracing:
132 `NotificationPreProcessor`, 132 `NotificationPostProcessor`, 24 `MockIdentityPrincipalHandler`.
Measured with a temporary counter in `IngressSmoke_Given_.CleanUpOnce` (removed before commit).

### Changes and choices

- **One baseline, in each server host's base `appsettings.json`** (web-server, api-server,
  grpc-server, yarp): `Default` Information; `Microsoft.Hosting.Lifetime` Information (IngressSmoke
  waits for "Application started"); `Microsoft.AspNetCore`, `Microsoft.EntityFrameworkCore`,
  `Microsoft.EntityFrameworkCore.Database.Command`, `System.Net.Http.HttpClient`,
  `Microsoft.Extensions.Http`, `Polly`, `Yarp`, `Grpc` at Warning. Kept in appsettings rather than
  service-defaults code: config is where an operator looks and overrides, and code-side
  `AddFilter` rules interleave with config rules by registration order. A guard fact keeps the
  four copies identical.
- **Development** adds only `TimeWarp.Architecture: Debug` (renamed by `sourceName` in generated
  apps). web-server also pins `TimeWarp.Architecture.Mail: Information` so
  `DevelopmentEmailSender`'s Debug body line stays out of telemetry, as its Design region intends.
  EF SQL stays off. The opt-in (`Logging:LogLevel:Microsoft.EntityFrameworkCore.Database.Command=Information`
  in user secrets or `Logging__LogLevel__…` in the environment) is documented in the web-server
  `appsettings.Development.json` comment, the `PostgresDbModule` Design region, and `tw-deploy`.
- **Production** files set no levels, so the Information baseline applies.
- **web-spa client**: Default Information, System/Microsoft Warning; Development adds
  `TimeWarp.Architecture: Debug`.
- **Deleted** the four dead `appsettings.Kubernetes_Docker.json` files (Default Debug, no
  references).
- **Test hosts**: the Debug `Logging` blocks are removed from the web-server and web-spa
  integration-test `appsettings.json`, so they inherit the host baseline. No test asserts on a
  Debug line: the seed-on-read capture provider filters at Warning itself, and
  `ef-site-settings-table-probe-tests` sets `SetMinimumLevel(Trace)` on its own factory. The
  IngressSmoke boot log watch keys on "Application started" (Information) and `fail:`/`crit:`, and
  still passes.
- **Guards** (`tests/container-apps/aspire/aspire-tests/logging-baseline-guard-tests.cs`, Unit):
  identical baseline across server hosts; base Default ≥ Information and EF categories ≥ Warning;
  across every committed `source/**/appsettings*.json`, nothing below Information except named
  categories in Development, and EF never below Warning; `EnableSensitiveDataLogging(` only in a
  file that checks `IsDevelopment()`. Verified the guard fails when web-server Default is set
  back to Debug.
- **Publish safety**: compose, kubernetes and aca suites gain `Publish_Should_NotLogBelowInformation`
  (no `Logging__LogLevel__*` at Debug/Trace). `tw-deploy` production-safety table has a row for it.
- **Boyscout (build gate)**: `dev build` failed on master with NU1902/NU1903 for transitive
  `System.Security.Cryptography.Pkcs` 9.0.6 (from `Microsoft.CodeAnalysis.Analyzer.Testing` →
  `NuGet.Packaging` 7.0.3). Pinned 9.0.21 centrally and referenced it from the two analyzer test
  projects.
- **Test race (exposed by the faster logging)**: `AskSurface_Given_Wasm.CtrlK_Should_ShowAsk_And_TheKeyState`
  failed 1/14 in the full Playwright run (base commit passed 14/14). After Escape the picker
  closes in a render, and `FocusComposerAsync` refocuses the textarea afterwards; the test checked
  focus in between. It now waits for focus with `WaitForFunctionAsync` (10 s). 14/14 twice after.

### Gates

- `dev build`: 0 warnings, 0 errors.
- `dev test`: passed (all projects).
- `dev template-smoke`: SUCCEEDED.
- `ganda repo audit`: passes.
- `dev check-version`: 2.0.0-beta.20 is ahead of nuget (beta.19), no bump needed.
- No AppHost was started with `dev run`. Aspire tests use the closed-box helper (ephemeral Postgres, dynamic ports).

### How to validate

**Smoke:**

```bash
dotnet build tests/container-apps/aspire/aspire-tests
dotnet tests/container-apps/aspire/aspire-tests/bin/Debug/net11.0/aspire-tests.dll --filter-class LoggingBaseline
dotnet tests/container-apps/aspire/aspire-tests/bin/Debug/net11.0/aspire-tests.dll --filter-class IngressSmoke
dev publish compose
```

**Expect:**

- LoggingBaseline: 4/4 pass. Changing web-server `appsettings.json` `Default` to `Debug` makes
  `Be_Identical_Across_Server_Hosts` and `Keep_Base_Config_At_Information_With_Ef_At_Warning` fail.
- IngressSmoke: 10/10 pass; web-server console has no `Microsoft.EntityFrameworkCore.Database.Command` lines.
- `dev publish compose` includes `Publish_Should_NotLogBelowInformation` passing.
- Under `dev run`, web-server stdout no longer shows EF SQL. Setting
  `Logging:LogLevel:Microsoft.EntityFrameworkCore.Database.Command=Information` in web-server user
  secrets brings it back.

## Session

- Created: 2026-10-14 (cockpit, per Steve, after /tmp filled)
- 2026-10-14: implemented under `ganda task work` (implement node); results above.

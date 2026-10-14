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

- [ ] Inventory + before/after log-volume numbers
- [ ] Base/Development/Production levels set per host (EF SQL off by default; opt-in documented)
- [ ] Sensitive-data logging never outside Development (+ publish safety assertion if cheap)
- [ ] Tests not dependent on Debug; regression guard on committed levels
- [ ] Gates: `dev build` 0/0, `dev test`, `dev template-smoke`, `ganda repo audit`

## Notes

- Workers must not touch the maintainer's running `dev run` or its log files.
- Until this merges, the maintainer can stop the current `dev run`, or set
  `Logging__LogLevel__Default=Information` for web-server, to stop the growth.

## Session

- Created: 2026-10-14 (cockpit, per Steve, after /tmp filled)

# Drop Serilog from web-server so structured logs reach the Aspire dashboard

## Description

web-server bootstraps Serilog (`UseSerilog(...)` in `program.cs`), which replaces the host's
logging providers and silently drops the OpenTelemetry logging provider that
`AddServiceDefaults()` registers. Result, verified 2026-09-14 against a running AppHost: traces
and metrics from web-server reach the Aspire dashboard, structured logs never do (not even
request logs). api-server has no Serilog and its logs do reach the dashboard.

Decision (Steve, 2026-09-14): drop Serilog from web-server rather than bridge it. One logging
story for the template: `ILogger` + `AddOpenTelemetry` via service defaults, same as api-server.
Considered and rejected: `Serilog.Sinks.OpenTelemetry` (works, env-var driven, but a second
logging stack Aspire itself never documents) and `writeToProviders: true` (known memory leak,
ignores provider log levels, community-only advice).

## Requirements

- `source/container-apps/web/projects/web-server/program.cs`: remove the bootstrap logger,
  `UseSerilog`, `AddSerilog`, `SelfLog.Enable`, and the static `Log.*` calls. Startup crash
  capture: keep a try/catch around host build/run that writes to the host's `ILogger` (or to
  `Console.Error` only in the outermost catch before any logger exists) — do not lose the
  "host terminated unexpectedly" path. Mirror api-server's `program.cs` shape where it already
  does the same job. Reconcile the Purpose/Design regions (the Design currently explains the
  Serilog bootstrap).
- `web-server.csproj`: remove the five Serilog `PackageReference`s. `global-usings.cs`: remove
  `Serilog.Debugging`. `appsettings.json`: remove the `Serilog` section; if it carried a
  meaningful minimum level / category override, express it under `Logging:LogLevel` instead.
- `Directory.Packages.props`: remove the five Serilog pins once no project references them
  (grep first — tests/ and tools/ must be clean too).
- Console output: Aspire's resource console already captures stdout; confirm the default
  console logger (from service defaults / hosting) still prints request logs in the AppHost
  console view. The `Logs/log.txt` file sink is not replaced.
- Gate (the actual point of the task): run the AppHost (`dev run`), POST
  `{"eventName":"Smoke.Serilog","correlationId":"…"}` to web-server `/Analytics/TrackEvent`
  (200 expected), then confirm in the Aspire dashboard **Structured logs** view for `web-server`:
  the `Analytics event Smoke.Serilog` record with `event.name` attribute AND ordinary
  `Request finished` records. Record the dashboard evidence in Results (the aspire MCP
  `list_structured_logs` tool or the UI). Before this task that view is empty for web-server.
- `dev build` 0/0, `dev test`, `dev template-smoke`.

## Checklist

- [ ] program.cs: Serilog bootstrap removed; crash capture preserved; regions reconciled
- [ ] csproj / global-usings / appsettings cleaned
- [ ] CPM pins removed (repo-wide grep for Serilog empty)
- [ ] Dashboard structured logs for web-server show `Request finished` and `Analytics event …`
- [ ] `dev build` 0/0 · `dev test` · `dev template-smoke`

## Notes

- Found during the 216 end-to-end check (task 216 added the `Analytics event` log; the console
  showed it, the dashboard did not).
- Research summary (2026-09-14): `Serilog.Sinks.OpenTelemetry` 4.2.0 reads `OTEL_EXPORTER_OTLP_*`
  and includes trace/span ids by default; Aspire docs never mention Serilog; the fullstackhero
  .NET 10 kit uses the sink; some practitioners have dropped Serilog for OTel entirely. Kept
  here for anyone re-opening the decision.
- Cockpit session: https://claude.ai/code/session_01QYpqCSgnvvLRpXrMKxu5ED

## Session

- Created: https://claude.ai/code/session_01QYpqCSgnvvLRpXrMKxu5ED (2026-09-14)

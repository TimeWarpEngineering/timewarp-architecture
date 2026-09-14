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

- [x] program.cs: Serilog bootstrap removed; crash capture preserved; regions reconciled
- [x] csproj / global-usings / appsettings cleaned
- [x] CPM pins removed (repo-wide grep for Serilog empty)
- [x] Dashboard structured logs for web-server show `Request finished` and `Analytics event …`
- [x] `dev build` 0/0 · `dev test` · `dev template-smoke`
- [x] Implementation review (effort 1, general) — disposition **clean**

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
- Implementer: grok session 01a09f4e-8a91-74c3-9773-709013f710d2 (2026-09-14)
- Review: grok session 01a09f63-6560-7fb0-9269-494c763c729b (2026-09-14)

## Results

Dropped Serilog from web-server so the host `ILogger` + `AddOpenTelemetry` path from `AddServiceDefaults()` is the only logging stack (same as api-server). `UseSerilog` was replacing host providers and silently dropping the OTLP logging exporter; structured logs for web-server were empty in the Aspire dashboard.

### What changed

- `source/container-apps/web/projects/web-server/program.cs`: removed bootstrap logger, `UseSerilog`, `AddSerilog`, `SelfLog.Enable`, and static `Log.*`. Kept try/catch around host build/run: `ILogger` after `Build()`, `Console.Error` before any logger exists. Purpose/Design regions reconciled. Startup lines use `LoggerMessage`.
- `web-server.csproj`: removed the five Serilog `PackageReference`s.
- `global-usings.cs`: removed `Serilog.Debugging`.
- `appsettings.json`: replaced the `Serilog` section with `Logging:LogLevel` (`Default: Debug`, `Microsoft.AspNetCore: Information` so `Request finished` is not filtered). The `Logs/log.txt` file sink is not replaced — Aspire captures stdout.
- `Directory.Packages.props`: removed the five Serilog pins. Repo-wide grep of `*.cs`/`*.csproj`/`*.props`/`*.json` is clean (Design-region mention of the rejected Serilog bootstrap is the only remaining product hit).

### Dashboard evidence (2026-09-14)

Isolated AppHost (`aspire start --isolated`) from this worktree. POST `https://localhost:33235/Analytics/TrackEvent` with `{"eventName":"Smoke.Serilog","correlationId":"02399106-93eb-46f1-9644-1bec31f5bf12"}` returned **HTTP 200** `{}`.

`aspire otel logs web-server --search Smoke.Serilog --format Json`:

- logId 67, resource `web-server`, message **`Analytics event Smoke.Serilog`**, source `TimeWarp.Architecture.Features.Analytics.Application.TrackEvent.Handler`, attribute `correlation.id=02399106-93eb-46f1-9644-1bec31f5bf12`. Dashboard: `https://localhost:45621/structuredlogs?logEntryId=67`.
- `{event.name}` is the LoggerMessage hole, so the event name is in the formatted message (`Analytics event Smoke.Serilog`). The CLI JSON `attributes` bag listed `correlation.id` (not in the message template) and not a duplicate `event.name` key.

`aspire otel logs web-server --search "Request finished" --format Json`:

- logId 68, source `Microsoft.AspNetCore.Hosting.Diagnostics`, message **`Request finished HTTP/2 POST https://localhost:33235/Analytics/TrackEvent - 200 ...`**, same trace as the analytics record.

Console (`aspire logs web-server`) also printed `Starting web host`, `Analytics event Smoke.Serilog`, and `Request finished`. Isolated AppHost was stopped afterward; the master-worktree AppHost was left running.

### Test outcomes

- `dotnet run tools/dev-cli/dev.cs -- build` — **0 Warning(s), 0 Error(s)**
- `dotnet run tools/dev-cli/dev.cs -- test` — **passed** (all suites; 2 skipped as before)
- `dotnet run tools/dev-cli/dev.cs -- template-smoke` — **Template smoke SUCCEEDED**

### How to validate

**Smoke**

```bash
# from this worktree (or after merge, any checkout of the branch)
aspire start --isolated --non-interactive --nologo --format Json \
  --apphost source/container-apps/aspire/projects/aspire-app-host/aspire-app-host.csproj
aspire wait web-server --apphost source/container-apps/aspire/projects/aspire-app-host/aspire-app-host.csproj --non-interactive
# take the https url from: aspire describe web-server --format Json
curl -k -si -X POST "https://localhost:<web-server-https>/Analytics/TrackEvent" \
  -H "Content-Type: application/json" \
  -d '{"eventName":"Smoke.Serilog","correlationId":"11111111-1111-1111-1111-111111111111"}'
aspire otel logs web-server --search "Smoke.Serilog" --format Json --limit 5
aspire otel logs web-server --search "Request finished" --format Json --limit 5
aspire stop --apphost source/container-apps/aspire/projects/aspire-app-host/aspire-app-host.csproj --non-interactive
```

**Expect**

- POST returns HTTP 200 with body `{}`.
- Structured logs for resource `web-server` include a record whose message is `Analytics event Smoke.Serilog` (event name in the formatted message; `correlation.id` as a structured attribute) and a `Request finished ... /Analytics/TrackEvent - 200` record from `Microsoft.AspNetCore.Hosting.Diagnostics`.
- Before this change that Structured logs view for web-server was empty.

**Automated gate**

```bash
dotnet run tools/dev-cli/dev.cs -- build   # expect: 0/0
dotnet run tools/dev-cli/dev.cs -- test    # expect: Tests completed successfully!
dotnet run tools/dev-cli/dev.cs -- template-smoke  # expect: Template smoke SUCCEEDED
```

**Depends on:** Aspire CLI, Docker (postgres), isolated AppHost so it does not collide with another local AppHost.

**Not in scope:** restoring the `Logs/log.txt` Serilog file sink; bridging Serilog via `Serilog.Sinks.OpenTelemetry` or `writeToProviders: true`.

**Review disposition**

- Rounds: 1. Effort 1, roster: general.
- Final counts: bug 0 / suggestion 0 / nit 0 (all open=0, fixed=0, wontfix=0).
- Outcome: **clean** (no findings raised).
- Paths: `review/review-framework.md`, `review/round-1/general.md`, `review/round-1/merged.md`, `review/disposition.md`.

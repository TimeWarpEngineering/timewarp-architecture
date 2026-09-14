# Review framework — task 218

**Date:** 2026-09-14
**Host task:** kanban/in-progress/218-drop-serilog-from-web-server-so-structured-logs-reach-the-aspire-dashboard/
**Diff scope:** branch `task/218-drop-serilog-from-web-server-so-structured-logs-re` vs `origin/master` (product commit `81aba4f5`; Results commit `813687fa`)
**Plan / brief:** Drop Serilog from web-server so host `ILogger` + `AddOpenTelemetry` from `AddServiceDefaults()` is the only logging stack (same as api-server). `UseSerilog` was replacing host providers and dropping the OTLP logging exporter.
**Effort:** 1 (general only)
**Reviewer roster:** general
**Session IDs:** grok review oracle 01a09f63-6560-7fb0-9269-494c763c729b (2026-09-14)

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`

## Brief (what must be true)

- `program.cs`: no bootstrap logger, `UseSerilog`, `AddSerilog`, `SelfLog.Enable`, or static `Log.*`. Crash capture kept: try/catch around host build/run; `ILogger` after `Build()`, `Console.Error` only before any logger exists. Purpose/Design regions reconciled.
- `web-server.csproj`: five Serilog `PackageReference`s gone. `global-usings.cs`: no `Serilog.Debugging`. `appsettings.json`: no `Serilog` section; meaningful min-level / category overrides live under `Logging:LogLevel`.
- `Directory.Packages.props`: five Serilog pins gone once no project references them (tests/ and tools/ clean).
- File sink `Logs/log.txt` is not replaced. Aspire captures stdout.
- Gate: structured logs for `web-server` include `Analytics event …` and `Request finished`. Recorded in Results.
- `dev build` 0/0, `dev test`, `dev template-smoke`.

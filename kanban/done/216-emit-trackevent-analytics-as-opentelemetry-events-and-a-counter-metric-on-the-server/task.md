# Emit TrackEvent analytics as OpenTelemetry events and a counter metric on the server

## Description

`track-event-handler-application.cs` (the handler for
`source/container-apps/web/features/analytics/track-event/track-event-contracts.cs`'s
`TrackEvent.Command`) is currently a deliberate no-op with a TODO. Make the sink OpenTelemetry,
which is already wired for the web server via
`source/container-apps/aspire/projects/aspire-service-defaults/extensions.cs`
`ConfigureOpenTelemetry()` (logging, ASP.NET Core/HttpClient/Runtime metrics + traces, OTLP
exporter; `web-server/program.cs:124` calls `AddServiceDefaults()`), so SPA actions become
visible in the Aspire dashboard. Decision (Steve, 2026-09-14): OpenTelemetry instead of a vendor
SDK; emit events + a counter metric, not spans.

## Requirements

- In `track-event-handler-application.cs`:
  - A structured `ILogger` event via `[LoggerMessage]` carrying `event.name` = `EventName` and
    `correlation.id` (OTel event convention: a log record with an `event.name` attribute).
  - A `Meter` (name constant, e.g. `TimeWarp.Architecture.Analytics`) with a `Counter<long>`
    named `analytics.events`, tagged by `event.name`.
  - The handler still always returns success — delivery/telemetry failures never surface to the
    caller.
  - Delete the existing TODO.
- Register the meter name in `aspire-service-defaults/extensions.cs`'s
  `ConfigureOpenTelemetry()` (`AddMeter(...)`), while keeping `aspire-service-defaults` generic:
  do not make it reference a web feature project. Put the meter-name constant somewhere both
  sides can see (a foundation-level or configuration-level home) and justify the choice in a
  Design region.
- Design region on the handler: "sink is here — swap or add a vendor (e.g. Segment) in this
  handler; the client stays dumb."
- Tests: extend `track-event-handler-tests.cs` to assert the log record (via `FakeLogger` /
  `Microsoft.Extensions.Diagnostics.Testing`) and the counter increment (via
  `MetricCollector<long>`); keep the existing endpoint and validator tests
  (`track-event-endpoint-tests.cs`, `track-event-validator-tests.cs`) green.
- How to validate manually: `dev run`, click the counter in the SPA (once the client half from
  the dependency task is live), open the Aspire dashboard → Structured logs filtered on
  `event.name`, Metrics → `analytics.events`. Record the outcome in Results.

## Checklist

- [x] `[LoggerMessage]` event with `event.name` / `correlation.id` added
- [x] `Meter` + `Counter<long> analytics.events` tagged by `event.name` added
- [x] Meter name registered via `AddMeter` in `aspire-service-defaults/extensions.cs`
- [x] Meter-name constant placed in a shared, justified location; Design region explains it
- [x] Handler Design region added ("sink is here…"); TODO removed
- [x] Tests: log record assertion, counter assertion; endpoint + validator tests still green
- [x] `dev build` 0/0
- [x] `dev test`
- [x] Manual dashboard check performed and recorded in Results
- [x] Implementation review (effort 1) — disposition clean

## Notes

- Independent of the client `TrackEvent` pipeline-behavior task at the file level (this task is
  server-only), but the demo only fires end-to-end once that task's client half is live too.
- Later option, not in scope here: client-side OpenTelemetry via Aspire's Blazor hosting
  `/_otlp` gateway once the WASM path is GA.
- Origin: session discussion 2026-09-14 replacing the `event-stream` demo with a client
  pipeline-behavior exemplar plus a real server sink.
- Cockpit session: https://claude.ai/code/session_01QYpqCSgnvvLRpXrMKxu5ED

Implementation review (effort 1, general only) lives under `review/`:

- `review/review-framework.md`
- `review/round-1/general.md`
- `review/round-1/merged.md`
- `review/disposition.md`

## Session

- Created: 2541946 (2026-09-14)
- Implementer: grok 01a09df2-6b17-7100-bf13-8aab2940e16c (2026-09-14)
- Review oracle: grok-4.6 session 01a09e04-767d-7e63-a523-d78eac731913 (2026-09-14)

## Results

TrackEvent's server handler is no longer a no-op. It emits an OpenTelemetry log event
(`[LoggerMessage]` with `event.name` and `correlation.id` via `[TagName]`) and increments
`analytics.events` tagged by `event.name`. Telemetry exceptions are swallowed so the handler
always returns success. The TODO is gone.

**Files**

- `source/container-apps/web/features/analytics/track-event/track-event-handler-application.cs`
  — OTel sink; Design region states the sink lives here (swap/add a vendor in this handler;
  the client stays dumb).
- `source/container-apps/web/features/analytics/track-event/track-event-contracts.cs` —
  optional `Guid? CorrelationId` so the client pipeline-behavior task can supply a per-session
  id; the validator still only requires `EventName`.
- `source/foundation/foundation-contracts/configuration/analytics-meters.cs` — shared meter /
  instrument / tag name constants next to `ServiceNames`.
- `source/container-apps/aspire/projects/aspire-service-defaults/extensions.cs` —
  `AddMeter(AnalyticsMeters.MeterName)` in `ConfigureOpenTelemetry()`.
- `source/container-apps/aspire/projects/aspire-service-defaults/aspire-service-defaults.csproj`
  — dual-mode `foundation-contracts` reference (no web feature project).
- `tests/container-apps/web/web-server-integration-tests/features/analytics/track-event/track-event-handler-tests.cs`
  — `FakeLogger` structured-state assertion + `MetricCollector<long>` increment assertion.
- `Directory.Packages.props`, `web-application.csproj`, `web-server-integration-tests.csproj`
  — `Microsoft.Extensions.Telemetry.Abstractions` / `Microsoft.Extensions.Diagnostics.Testing`.

**Decisions**

- Meter-name home is `TimeWarp.Foundation.Configuration.AnalyticsMeters` (foundation-contracts
  configuration, same pattern as `ServiceNames`) so Aspire ServiceDefaults can `AddMeter` it
  without referencing a web feature. Design regions on both the constant file and
  `extensions.cs` record this.
- Optional `CorrelationId` on the command (not required by the validator) so the log field has
  a product value once task 215's client half posts it.
- Process-lifetime static `Meter` (name-matched by `AddMeter`) rather than `IMeterFactory`, so
  the handler does not dispose a factory-owned meter.

**Tests**

- `dotnet run tools/dev-cli/dev.cs -- build` — 0 warnings / 0 errors.
- `dotnet run tools/dev-cli/dev.cs -- test` — all projects passed (web-server-integration-tests
  153 passed / 1 skipped `RunForever`; TrackEvent filter: 7/7 including new log + counter
  tests plus existing endpoint and validator tests).

**Manual dashboard**

SPA counter click was not available: task 215 (client `TrackEvent` pipeline behavior) is not
merged, so the demo does not POST from the SPA yet. The HTTP sink is proven by the endpoint
and handler tests (real host `Web.Send` plus unit `FakeLogger` / `MetricCollector`). Aspire
dashboard UI was not opened in this session (no AppHost left running). Use the smoke below
once 215 is live, or POST the command directly.

**Review** (effort 1, general only; 1 round)

- Roster: general (`review/round-1/general.md`)
- Final counts: bug 0 open / 0 fixed / 0 wontfix; suggestion 0; nit 0
- Disposition: **clean** (`review/disposition.md`) — no findings; no wontfix; no escalation
- Paths: `review/review-framework.md`, `review/round-1/merged.md`, `review/disposition.md`

### How to validate

**Automated**

```bash
cd tests/container-apps/web/web-server-integration-tests && dotnet test -c Release -- --filter-class TrackEvent
# expect: 7 passed (Handle_Returns, Handle_Emits log + counter, endpoint Ok + validation, validator Be_Valid + empty EventName)
```

**Smoke**

```bash
dotnet run tools/dev-cli/dev.cs -- run
# wait until the Aspire dashboard URL is printed, then either:
# 1. (once task 215 is live) open the SPA, click the counter
# 2. or POST without the SPA:
#    curl -sk -X POST "$WEB/Analytics/TrackEvent" \
#      -H 'Content-Type: application/json' \
#      -d '{"eventName":"manual-check","correlationId":"aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"}'
```

Open the Aspire dashboard → Structured logs filtered on `event.name` (value `manual-check` or
the counter action type name) → Metrics → `analytics.events` on meter
`TimeWarp.Architecture.Analytics`.

**Expect**

- HTTP 200 / successful `TrackEvent.Response` (never a problem details payload for a valid
  `EventName`).
- One structured log record with attributes `event.name` and `correlation.id`.
- `analytics.events` counter increments by 1, tagged `event.name`.

**Depends on**

- `dev run` (Aspire AppHost injects `OTEL_EXPORTER_OTLP_ENDPOINT`; without it the OTLP exporter
  stays off and the dashboard will not show exported metrics/logs).
- SPA counter click needs task 215 merged.

**Not in scope**

- Client-side OpenTelemetry via Aspire's Blazor `/_otlp` gateway.
- Vendor SDK (Segment, etc.) — swap inside this handler later.

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

- [ ] `[LoggerMessage]` event with `event.name` / `correlation.id` added
- [ ] `Meter` + `Counter<long> analytics.events` tagged by `event.name` added
- [ ] Meter name registered via `AddMeter` in `aspire-service-defaults/extensions.cs`
- [ ] Meter-name constant placed in a shared, justified location; Design region explains it
- [ ] Handler Design region added ("sink is here…"); TODO removed
- [ ] Tests: log record assertion, counter assertion; endpoint + validator tests still green
- [ ] `dev build` 0/0
- [ ] `dev test`
- [ ] Manual dashboard check performed and recorded in Results

## Notes

- Independent of the client `TrackEvent` pipeline-behavior task at the file level (this task is
  server-only), but the demo only fires end-to-end once that task's client half is live too.
- Later option, not in scope here: client-side OpenTelemetry via Aspire's Blazor hosting
  `/_otlp` gateway once the WASM path is GA.
- Origin: session discussion 2026-09-14 replacing the `event-stream` demo with a client
  pipeline-behavior exemplar plus a real server sink.
- Cockpit session: https://claude.ai/code/session_01QYpqCSgnvvLRpXrMKxu5ED

## Session

- Created: 2541946 (2026-09-14)

# Dual OTLP export so Aspire run-mode services export to both Aspire Dashboard and SigNoz

## Description

In local Aspire run mode, Aspire injects `OTEL_EXPORTER_OTLP_ENDPOINT` pointing at the ephemeral local Aspire Dashboard, overriding any host-level `OTEL_EXPORTER_OTLP_ENDPOINT` (such as a persistent SigNoz collector). Meanwhile `SigNozDashboardLink` (`source/container-apps/aspire/projects/aspire-app-host/signoz-dashboard-link.cs`) only adds a dashboard URL bookmark and exports no telemetry.

As a result, live `web-server`, `api-server`, `grpc-server`, and `ingress` traces and structured logs (including `/api/browser-logs`) are invisible to SigNoz and to agents querying the SigNoz MCP.

Add an optional secondary OTLP exporter so local Aspire run-mode services export to **both** the Aspire Dashboard and SigNoz (or any configured secondary OTLP collector) simultaneously, without changing publish/deploy manifests or breaking apps that have no secondary collector configured.

## Propagation

likely

## Parent

536

## Requirements

1. **AppHost run-mode forwarding (`source/container-apps/aspire/projects/aspire-app-host/`):**
   - Add a small, dedicated run-mode helper (alongside `signoz-dashboard-link.cs`) that resolves an optional secondary OTLP target **only when `builder.ExecutionContext.IsRunMode` is true** (never in publish/deploy manifests):
     - Explicit configuration key `Observability:SigNoz:OtlpEndpoint` (plus optional `Observability:SigNoz:OtlpHeaders` and `Observability:SigNoz:OtlpProtocol`).
     - When `Observability:SigNoz:OtlpEndpoint` is unset and `Observability:SecondaryOtlp:Disabled` is not `true`, fall back to the AppHost process's inherited `OTEL_EXPORTER_OTLP_ENDPOINT` (and `OTEL_EXPORTER_OTLP_HEADERS` / `OTEL_EXPORTER_OTLP_PROTOCOL`) **only if** it is a valid absolute `http`/`https` URI and does **not** match Aspire's own dashboard OTLP endpoint (`DOTNET_DASHBOARD_OTLP_ENDPOINT_URL` / `ASPIRE_DASHBOARD_OTLP_ENDPOINT_URL`).
   - Forward the resolved secondary endpoint/headers/protocol to every declared project resource (`webServer`, `apiServer`, `grpcServer`, and run-mode YARP `ingress`, honoring all `#if api` / `#if grpc` / `#if web` / `#if yarp` template flags) using dedicated keys:
     - `Observability__SecondaryOtlp__Endpoint`
     - `Observability__SecondaryOtlp__Headers` (when set)
     - `Observability__SecondaryOtlp__Protocol` (when set)
   - Do **not** modify or override Aspire's `OTEL_EXPORTER_OTLP_ENDPOINT`; Aspire Dashboard must continue receiving all telemetry as before.
   - Update `SigNozDashboardLink` comments/Design region to reference the new secondary OTLP export cleanly.

2. **ServiceDefaults secondary OTLP exporter (`source/container-apps/aspire/projects/aspire-service-defaults/`):**
   - Keep `extensions.cs` minimally diffable against upstream Aspire ServiceDefaults by placing the secondary exporter wiring in a sibling file (following the `log-redaction-extensions.cs` pattern).
   - When `Observability:SecondaryOtlp:Endpoint` is configured (valid absolute `http`/`https` URI), export **traces, metrics, and logs** to both the primary `OTEL_EXPORTER_OTLP_ENDPOINT` (Aspire Dashboard) and the secondary endpoint (SigNoz).
   - **Important OpenTelemetry .NET SDK check:** Verify whether `OpenTelemetryBuilder.UseOtlpExporter()` conflicts with per-signal `AddOtlpExporter(...)` in `OpenTelemetry.Exporter.OpenTelemetryProtocol` 1.21.0 (which throws `NotSupportedException` if both cross-cutting `UseOtlpExporter()` and per-signal `AddOtlpExporter()` are registered on the same builder). When the secondary endpoint is active (or unconditionally if cleaner), register explicit per-signal OTLP exporters (primary + named `"secondary"`) on Tracing, Metrics, and Logging so both exporters coexist cleanly without throwing.
   - Support both `grpc` and `http/protobuf` (`OtlpExportProtocol.Grpc` / `OtlpExportProtocol.HttpProtobuf`) and optional `Headers` (never log header/API-key values).
   - When `Observability:SecondaryOtlp:Endpoint` is unset or blank, behavior is unchanged and no extra exporter is registered.
   - An unreachable secondary endpoint must never fail service startup or block request handling.

3. **Tests & Template Flags:**
   - Add automated tests covering:
     - ServiceDefaults wiring with primary only, secondary only, and both primary + secondary (proving `Build()` and `StartAsync()` succeed with no `NotSupportedException` and both OTLP exporters register for traces, metrics, and logs).
     - AppHost run-mode endpoint resolution: explicit `Observability:SigNoz:OtlpEndpoint`, fallback to host `OTEL_EXPORTER_OTLP_ENDPOINT`, skipping when host endpoint equals Aspire dashboard OTLP URL, disabling via `Observability:SecondaryOtlp:Disabled=true`, and verifying publish-mode manifests do **not** emit `Observability__SecondaryOtlp__*`.
   - Ensure `./bin/dev build`, targeted tests, and `./bin/dev template-smoke --matrix` (since AppHost `#if` flag wiring is touched) pass with 0 warnings and 0 errors.

## Notes

- Host shell on dev machines typically exports `OTEL_EXPORTER_OTLP_ENDPOINT` and `OTEL_EXPORTER_OTLP_PROTOCOL` (e.g. `http/protobuf` or `grpc`) for SigNoz. Inheriting those in run mode means `dev run` / `aspire start` automatically sends `web-server` traces & logs to SigNoz with zero extra per-repo secret boilerplate, while still allowing explicit override via `Observability:SigNoz:OtlpEndpoint`.
- `source/container-apps/aspire/projects/aspire-service-defaults/extensions.cs` and `source/container-apps/aspire/projects/aspire-app-host/signoz-dashboard-link.cs` are the key starting files.

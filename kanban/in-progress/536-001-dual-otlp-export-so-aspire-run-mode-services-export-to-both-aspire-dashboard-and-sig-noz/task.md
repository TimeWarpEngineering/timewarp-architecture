# Dual OTLP export so Aspire run-mode services export to both Aspire Dashboard and SigNoz

## Goal (implementer restatement)

Aspire run mode points each service's `OTEL_EXPORTER_OTLP_ENDPOINT` at the ephemeral dashboard, so a shell-level SigNoz collector never sees `web-server`, `api-server`, `grpc-server`, or `ingress` telemetry. The SigNoz dashboard link is only a bookmark. Add an optional second OTLP exporter that runs beside the dashboard export, and leave publish and deploy manifests unchanged.

Acceptance:

- In run mode only, the AppHost resolves a secondary collector from `Observability:SigNoz:OtlpEndpoint` (optional headers and protocol) or, when that endpoint is unset and `Observability:SecondaryOtlp:Disabled` is not `true`, from the AppHost process environment `OTEL_EXPORTER_OTLP_ENDPOINT` when that value is an absolute http(s) URL and is not the Aspire dashboard OTLP URL. It copies the resolved endpoint, and headers and protocol when that same source set them, onto every declared project resource (`api-server`, `grpc-server`, `web-server`, and run-mode `ingress`) as `Observability__SecondaryOtlp__*`. It does not change `OTEL_EXPORTER_OTLP_ENDPOINT`. Publish and deploy output do not contain those keys. An explicit bad endpoint or protocol fails AppHost startup and names the key. The SigNoz link comments point at this export.
- ServiceDefaults, in a sibling of `extensions.cs`, exports traces, metrics, and logs to that secondary endpoint and to the primary endpoint when both are set. While the secondary endpoint is active it does not call `UseOtlpExporter` (OpenTelemetry 1.21 throws if that is combined with `AddOtlpExporter`); it registers per-signal primary and named `secondary` exporters instead. `grpc` and `http/protobuf` and optional headers work, and header values are not logged. An unset or blank endpoint leaves the existing `UseOtlpExporter` path unchanged. An unreachable collector does not fail startup.
- Tests cover primary only, secondary only, and both (`Build` and `StartAsync` succeed, exporters present on all three signals), plus AppHost resolution: explicit, inherited, dashboard skip, disabled fallback, and publish artifacts that were given an endpoint and still omit `Observability__SecondaryOtlp__*`. `./bin/dev build`, those test files, and `./bin/dev template-smoke --matrix` pass with 0 warnings and 0 errors.

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

## Results

Run mode now forwards an optional secondary OTLP collector onto `api-server`, `grpc-server`, `web-server`, and ingress, and ServiceDefaults exports traces, metrics, and logs to that collector beside the Aspire dashboard. Publish output does not gain `Observability__SecondaryOtlp__*`. OpenTelemetry 1.21 throws when `UseOtlpExporter` and `AddOtlpExporter` share a builder, so an active secondary endpoint registers per-signal exporters instead and restores HTTP signal-path appending. `skills/tw-deploy/SKILL.md` states the same contract in the production-safety table and is 499 lines (the 500-line cap). The first audit failed that cap at 512 lines; the duplicate how-to subsection was removed and the rerun passed.

### How to validate

Smoke: `dev run` with the shell `OTEL_EXPORTER_OTLP_ENDPOINT` pointed at SigNoz (or set `Observability:SigNoz:OtlpEndpoint` to an absolute http(s) URL), then send a web-server request.

Expect: the same traces, metrics, and logs show in the Aspire dashboard and in SigNoz. Aspire's `OTEL_EXPORTER_OTLP_ENDPOINT` stays the dashboard. With the secondary endpoint unset, or with `Observability:SecondaryOtlp:Disabled=true` and no explicit endpoint, only the dashboard receives telemetry. `dev publish` output contains no `Observability__SecondaryOtlp__*`.

These aspire-tests files are project classes, not `#if !JARIBU_MULTI` runfiles. `./bin/dev test <file>` runs the class declared in that file. CI runs the project.

- `./bin/dev build` — exit 0, Build completed successfully (59.8s).
- `./bin/dev test tests/container-apps/aspire/aspire-tests/secondary-otlp-exporter-tests.cs` — `SecondaryOtlpExporter_Given_` 10/10 passed in 10.2s (rerun after the graph reader was fully qualified).
- `./bin/dev test tests/container-apps/aspire/aspire-tests/secondary-otlp-export-model-tests.cs` — `SecondaryOtlpExportModel_Given_` 18/18 passed in 7.0s.
- `./bin/dev test tests/container-apps/aspire/aspire-tests/compose-publish-tests.cs` — `ComposePublish_Given_` 12/12 passed in 7.3s.
- `./bin/dev test tests/container-apps/aspire/aspire-tests/kubernetes-publish-tests.cs` — `KubernetesPublish_Given_` 13/13 passed in 6.7s. `kubernetes-publish-chart-parsing.cs` is that same partial class and was included in this run.
- `./bin/dev test tests/container-apps/aspire/aspire-tests/aca-publish-tests.cs` — `AcaPublish_Given_` 15/15 passed in 4.7s. `aca-publish-bicep-parsing.cs` is that same partial class and was included in this run.
- `./bin/dev test tests/container-apps/aspire/aspire-tests/aca-neon-publish-tests.cs` — `AcaNeonPublish_Given_` 7/7 passed in 6.6s.
- `tests/container-apps/aspire/aspire-tests/secondary-otlp-exporter-graph.cs` — no test class; `SecondaryOtlpExporter_Given_` covers it.
- `dotnet run --file tools/dev-cli/dev.cs -- self-install` — exit 0. `./bin/dev` is gitignored.
- `./bin/dev template-smoke --matrix` — exit 0, Template smoke SUCCEEDED. SmokeDefault, SmokeNoPostgres, SmokeNoApi, SmokeNoWeb, SmokeNoGrpc, SmokeNoYarp, and SmokeWebOnly each OK, with 0 warnings and 0 errors on the generated builds.
- `ganda repo audit` — exit 0, pass: 43 passed, 0 failed, 1 warning (`vscode-window-icon`, pre-existing).

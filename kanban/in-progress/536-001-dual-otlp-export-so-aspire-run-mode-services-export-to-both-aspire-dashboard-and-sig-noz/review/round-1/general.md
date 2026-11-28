# Round 1 — general
**Date:** 2026-11-28
**Scope reviewed:** branch vs merge-base 1d86386de (see review-framework.md). Product files:
`secondary-otlp-export.cs`, `program.cs`, `signoz-dashboard-link.cs` (AppHost);
`secondary-otlp-exporter.cs`, `extensions.cs` (ServiceDefaults); `skills/tw-deploy/SKILL.md`;
`tools/dev-cli/services/template-smoke/template-smoke-dev-cli.cs`; the aspire-tests additions and
the publish-suite edits.

## Summary

The change meets the task. In run mode the AppHost resolves an explicit `Observability:SigNoz:OtlpEndpoint`
or the inherited shell `OTEL_EXPORTER_OTLP_ENDPOINT`. It skips the dashboard URL and honors `Disabled`.
It forwards `Observability__SecondaryOtlp__*` to the four flag-gated project resources and leaves
`OTEL_EXPORTER_OTLP_ENDPOINT` alone. ServiceDefaults swaps `UseOtlpExporter` for per-signal
primary plus named `secondary` exporters only when the secondary endpoint is active. It clears the
inherited dashboard API key from the secondary headers. Log redaction runs at the logger factory,
so the secondary log exporter gets redacted values too. The publish-suite edits only add checks:
each suite is now given an endpoint and asserts it is absent from Compose, Kubernetes, ACA, and
aca-neon output. No check was weakened. The goal restatement matches the task, so there is no
misread. Two issues need work: the http/protobuf signal path is restored by reflection into an
internal OpenTelemetry setter, and an out-of-scope template-smoke parser change is missing from
Results and has no test.

## Issues

### Issue 1 — Severity: suggestion
- File: source/container-apps/aspire/projects/aspire-service-defaults/secondary-otlp-exporter.cs:107
- Description: `RestoreSignalPathAppending` uses reflection to call the non-public setter of
  `OtlpExporterOptions.AppendSignalPathToEndpoint`. That setter is an OpenTelemetry internal, not
  a public API. If a package bump renames or removes it, the method throws inside the exporter
  options callback, and every service fails startup in run mode while a secondary collector is
  set. That breaks the task's own rule that the secondary export never fails service startup.
  Setting a library's private state is the pattern the org avoids ("wrap tools, never reconstruct
  internals"; "no patches of library internals"). OpenTelemetry has a public way to do this: when
  `Endpoint` is set in code for http/protobuf, give it the full signal URL.
- Suggestion: Register three named exporters (for example `secondary-traces`, `secondary-metrics`,
  `secondary-logs`) so each signal gets its own options instance. In each configure callback,
  apply the protocol first. Then set `Endpoint` to the base URI for gRPC, or to the base URI plus
  `v1/traces` / `v1/metrics` / `v1/logs` for `HttpProtobuf`, joined with no duplicate slash. Drop
  the reflection. Update the Design region. The existing `SecondaryOtlpExporterGraph` path
  assertions (`v1/traces` etc.) already cover this.
- Status: open

### Issue 2 — Severity: suggestion
- File: tools/dev-cli/services/template-smoke/template-smoke-dev-cli.cs:192
- Description: The branch rewrites `ListedCommands` to find the capabilities JSON after ANSI
  escape sequences (`IndexOfCapabilitiesJson`, `SkipAnsi`). That is outside the task scope. The
  commit message and `## Results` do not mention it or say why it was needed (presumably the
  `template-smoke --matrix` run failed on a colored warning line). No test covers the new parser:
  `rg ListedCommands` finds only the definition and its one call. A boyscout fix in a feature PR
  is welcome, but it has to be recorded, and a parser in a gate tool needs a case that proves it.
- Suggestion: Add a line to `## Results` naming the change and the failure it fixed. Add a test
  for `ListedCommands` with the dev-cli tests (or a co-located runfile) covering plain JSON at
  offset 0, JSON after warning lines, and JSON after an ANSI color sequence on the same line.
- Status: open

### Issue 3 — Severity: nit
- File: tests/container-apps/aspire/aspire-tests/secondary-otlp-exporter-tests.cs:46
- Description: `SecondaryOtlpExporter_Given_` and `SecondaryOtlpExportModel_Given_`
  (secondary-otlp-export-model-tests.cs:46) each declare their own private `EnvironmentGate`
  semaphore. Both write the same process-wide `OTEL_EXPORTER_OTLP_*` and
  `*_DASHBOARD_OTLP_ENDPOINT_URL` variables. A per-class gate only serializes within one class.
  If the runner ever runs the two classes at once in the shared aspire-tests process, a model fact
  that expects no forwarding (`RunMode_Should_SkipFallbackWhenDisabled`) could read the exporter
  fact's `OTEL_EXPORTER_OTLP_ENDPOINT` and fail at random. The Design comments say the gate guards
  the environment, which suggests more than it guarantees.
- Suggestion: Move the gate and the Capture/Replace/Restore helpers into one shared internal
  test helper (for example `OtlpEnvironmentScope`) that both classes use.
- Status: open

# Round 1 — merged findings
**Date:** 2026-11-28
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 0 | 0 |
| suggestion | 0 | 2 | 0 |
| nit | 0 | 1 | 0 |

## Issues

### M1 — Severity: suggestion — Status: fixed
- File: source/container-apps/aspire/projects/aspire-service-defaults/secondary-otlp-exporter.cs:107
- Description: The http/protobuf signal path is restored by reflection into the non-public setter
  `OtlpExporterOptions.AppendSignalPathToEndpoint`. If an OpenTelemetry bump renames or removes
  it, the callback throws and every run-mode service fails startup while a secondary collector is
  set. Setting a library's private state goes against the org rule "wrap tools, never reconstruct
  internals".
- Suggestion: Use three named exporters, one options instance per signal (`secondary-traces` /
  `-metrics` / `-logs`). Set the full signal URL (`…/v1/traces` etc.) when the protocol is
  `HttpProtobuf`, and the base URI for gRPC. Remove the reflection and update the Design region.
  The existing graph path assertions cover the result.
- Source: general
- Disposition notes: Each signal has its own named exporter. Http/protobuf gets the full signal URL. gRPC gets the base URI. The private setter is gone.

### M2 — Severity: suggestion — Status: fixed
- File: tools/dev-cli/services/template-smoke/template-smoke-dev-cli.cs:192
- Description: The ANSI-aware `ListedCommands` / `IndexOfCapabilitiesJson` rewrite is outside the
  task scope. `## Results` and the commit do not mention it or the failure it fixed, and no test
  covers the new parser.
- Suggestion: Record it in `## Results` with the reason. Add `ListedCommands` cases: JSON at
  offset 0, JSON after warning lines, and JSON after an ANSI sequence on the same line.
- Source: general
- Disposition notes: Results records why the parser skips warnings and ANSI. `ListedCommands_Given_` covers JSON at offset 0, JSON after warning lines, and JSON after an ANSI sequence on the same line.

### M3 — Severity: nit — Status: fixed
- File: tests/container-apps/aspire/aspire-tests/secondary-otlp-exporter-tests.cs:46
- Description: The two new test classes each have a private `EnvironmentGate` but write the same
  process environment variables. That serializes within a class, not across the two.
- Suggestion: Share one gate and the Capture/Replace/Restore helpers in a single internal helper
  that both classes use.
- Source: general
- Disposition notes: `SecondaryOtlpEnvironment` holds the one gate and the capture, replace, and restore helpers. Both test classes call it.

## Duplicates / conflicts

- None. Single reviewer.

## Checked and not raised

- Goal restatement matches the task's Requirements 1–3; no misread.
- Publish-suite edits (compose, kubernetes, aca, aca-neon) only add `SecondaryOtlp` /
  `otlp.example` absence checks and pass the endpoint to each publish. No assertion was loosened.
- Log redaction (`EnableRedaction`) runs at the logger factory, before providers, so the
  secondary log exporter receives redacted values.
- Secondary headers default to empty, so Aspire's dashboard API key (`OTEL_EXPORTER_OTLP_HEADERS`)
  is not sent to the second collector. The tests assert this in both directions.
- Run-mode ingress is an `AddDotnetProject` resource, so `TryCreateResourceBuilder<DotnetProjectResource>`
  finds it. `Forward` runs after Postgres wiring, so `web-migrations` does not inherit the keys,
  and the tests assert that.
- Recorded gates cover the change: build, the six aspire-tests files, `template-smoke --matrix`,
  and the audit. No gate was re-run.

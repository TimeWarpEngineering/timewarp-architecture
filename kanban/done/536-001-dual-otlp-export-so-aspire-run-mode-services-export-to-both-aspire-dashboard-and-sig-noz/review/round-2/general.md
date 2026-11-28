# Round 2 — general
**Date:** 2026-11-28
**Scope reviewed:** fix-pass delta `9674f707a..5ea5b31a7` (commit "fix: give each secondary OTLP signal its own endpoint"), plus the updated `## Goal (implementer restatement)` and `## Results` / `### How to validate (fix pass)` in task.md.

## Summary

The fix pass closes the three round-1 findings without widening scope. The secondary exporter is now
three named exporters, one options object per signal. Http/protobuf gets the base URL plus
`v1/traces`, `v1/metrics`, or `v1/logs`. gRPC gets the base URI. The reflection into the private
`AppendSignalPathToEndpoint` setter is gone. `ListedCommands` has three unit cases, and the two
secondary-OTLP test classes share one environment gate. Risk is low. No new issues.

## Re-verification of round-1 findings

- **M1 — fixed.** `secondary-otlp-exporter.cs`: `using System.Reflection` and
  `RestoreSignalPathAppending` are removed. `ConfigureSecondary(options, signalPath)` applies the
  protocol first, then reads the protocol it ended with (the explicit setting, or the one the options
  constructor read from `OTEL_EXPORTER_OTLP_PROTOCOL`) to choose the full signal URL or the base URI.
  `WithSignalPath` adds one `/` only when needed and leaves an endpoint that already ends in the
  signal path unchanged. The Design regions in `secondary-otlp-exporter.cs` and `extensions.cs` match
  the new behavior. Coverage: `secondary-otlp-exporter-graph.cs:60-61` checks that each exporter's
  `AbsolutePath` contains `v1/<signal>` (http) or `<Signal>Service/Export` (gRPC), and
  `SecondaryOnly_Should_RegisterOneExporterPerSignal` runs that check with `grpc`, `http/protobuf`,
  and `HTTP/Protobuf`.
- **M2 — fixed.** `## Results` → `### Fix pass (review round 1)` says why the parser skips warning
  lines and a leading ANSI sequence. `template-smoke-dev-cli-tests.cs` (`ListedCommands_Given_`) covers
  JSON at offset 0, JSON after warning lines, and JSON after an ANSI sequence on the JSON line.
  `template-smoke-dev-cli.cs` is now compile-included in `dev-cli-tests.csproj`. The csproj comments
  list it and say why it needs Amuru. The CA1307 fix (`IndexOf(..., StringComparison.Ordinal)`) is
  recorded in Results.
- **M3 — fixed.** `secondary-otlp-environment.cs` holds the one `SemaphoreSlim` and the
  capture/replace/restore helpers. Both classes call `IsolateAsync`. The model suite's restore still
  runs after `CreateAsync` returns, as it did before. Its Design region says why that is safe
  (`Forward` copies literal values onto the callbacks inside the gate).

## Checked and not raised

- Goal restatement, updated for the per-signal exporters, still matches Requirements 1–3. No misread.
- No assertion was loosened. The two test files lost only their private gate and helpers, and the
  fact bodies are the same.
- The explicit `using TimeWarp.Amuru; using TimeWarp.Terminal;` in `template-smoke-dev-cli.cs` is
  what the new compile-include needs. In the dev-cli runfile, a repeated global using is only a hidden
  diagnostic and is not an error.
- Recorded gates cover the delta: `./bin/dev build`, the two aspire-tests files (10/10, 18/18),
  `ListedCommands_Given_` (3/3), and `ganda repo audit`. `template-smoke --matrix` was not re-run.
  The fix pass changes no template output or `#if` region, so it does not need a re-run. No gate was
  re-run in this review.

## Issues

None.

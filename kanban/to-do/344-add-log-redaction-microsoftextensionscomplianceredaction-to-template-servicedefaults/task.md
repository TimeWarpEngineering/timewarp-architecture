# Add log redaction (Microsoft.Extensions.Compliance.Redaction) to template ServiceDefaults

## Description

In the Architecture template's ServiceDefaults, wire Microsoft.Extensions.Compliance.Redaction (data classification + redactors, `EnableRedaction` on logging) and `[LogProperties]` / source-generated logging where relevant, so every generated web solution masks PII before logs and OTel export.

Motivation: CrunchIt (financial firm portal) handles client data.

## Checklist

- [x] Define data classification taxonomy (e.g. PII / sensitive) in ServiceDefaults
- [x] Register redactors (`AddRedaction`) and call `EnableRedaction` on logging
- [x] Use `[LogProperties]` / source-generated `[LoggerMessage]` where relevant; watch log volume
- [x] Confirm redaction applies before OTel log export
- [x] Tests proving a classified property is redacted in log output
- [x] Web apps only; do not add to CLIs

## Out of scope

- StaticAnalysis analyzers (KILL)
- Probes / ResourceMonitoring (KILL; ServiceDefaults covers health checks)
- Contextual Options (KILL; preview)
- AI.Evaluation (deferred until skill-evals needs a scoring layer)

## Session

- Created: 1416915 (2026-10-13)
- 2026-10-15 implement (claude, ganda task work): redaction wired in service defaults, mail sender
  classified, TWA0001 generated-partial fix, Nuru 3.0.0 audit boy-scout. Gates green.

## Results

- **Taxonomy** (`source/container-apps/aspire/projects/aspire-service-defaults/`): `DataTaxonomy`
  (`PersonalData`, `SensitiveData`) in `TimeWarp.Architecture.Compliance`, plus
  `PersonalDataAttribute` / `SensitiveDataAttribute`.
- **Redaction**: `log-redaction-extensions.cs` `AddLogRedaction()` calls `builder.Logging.EnableRedaction()`
  and `AddRedaction` mapping both classes (and the fallback) to `ErasingRedactor`. `AddServiceDefaults`
  calls it first; documented as the second deliberate deviation from upstream ServiceDefaults.
  Packages: `Microsoft.Extensions.Compliance.Redaction` + `Microsoft.Extensions.Telemetry` 10.10.0.
- **Before OTel export**: the extended logger factory redacts classified tags once before fan-out, so
  the OTel log processor/exporter receives erased values. Proven by the test's capturing exporter
  attached via `ConfigureOpenTelemetryLoggerProvider` (same point the OTLP exporter sits).
- **Source-generated logging where relevant**: `DevelopmentEmailSender` moved from `LoggerMessage.Define`
  to `[LoggerMessage]` with `[PersonalData] to` and `[SensitiveData] body` (the only server-side log
  of PII today). web-server now references `Microsoft.Extensions.Telemetry.Abstractions` directly so
  the extended generator runs (analyzers do not flow through the service-defaults project reference).
  No `[LogProperties]` added: no server log takes a PII-bearing object today, and logging whole
  objects would raise volume. `LoggerMessage.Define` / `ILogger.LogX` calls carry no classification.
- **Web apps only**: only the server hosts (web-server, api-server, grpc-server, yarp) reference
  service defaults; no CLI does.
- **Analyzer fix (TWA0001)**: `PartialClassDeclarationAnalyzer` now ignores generated declarations
  (`*.g.cs`, `*.g.i.cs`, `*.generated.cs`, `*.designer.cs`, or an auto-generated header), so a
  single authored partial class plus generator output is not "multi-file". Two new analyzer tests.
- **Limit**: the taxonomy lives in service defaults (per spec), so `*-application.cs` handlers
  (web-application) cannot tag parameters yet; moving the taxonomy lower (foundation) would be a
  follow-up if application-layer logs need classification.
- **Boy-scout**: `ganda repo audit` failed on pre-existing `TimeWarp.Nuru` 3.0.0-beta.80; bumped
  `TimeWarp.Nuru` and `TimeWarp.Nuru.DevCli` to 3.0.0 (dev-cli-tests 218/218).
- **Gates**: `dev build` 0/0; `ganda repo audit` exit 0; `dev check-version` OK (2.0.0-beta.20 ahead
  of NuGet beta.19, no bump needed); `dev template-smoke` SUCCEEDED; analyzer tests 224/224;
  web-server-integration-tests 346 passed / 1 skipped.

### How to validate

**Smoke:**

```bash
cd tests/container-apps/web/web-server-integration-tests
dotnet build && dotnet run --no-build -- --filter-class DevelopmentEmailSender_
cd ../../../analyzers/timewarp-architecture-analyzers-tests && dotnet test
```

**Expect:** `DevelopmentEmailSender_.SendAsync_Should.Redact_Classified_Values_Before_Log_Providers_And_Otel_Export`
passes: FakeLogger structured state has `To` and `Body` as empty strings, `Subject` intact, and the
OTel capturing exporter saw the subject but never `client.person@example.com` or the body text.
Commenting out `builder.AddLogRedaction();` in `extensions.cs` makes it fail on `To`. Analyzer
suite passes 224/224, including `Given_SingleAuthoredFileWithGeneratedPartial`.

## Notes

- Busara GREEN 2026-10-13 on Anton Martyniuk's dotnet/extensions post: https://x.com/AntonMartyniuk/status/2109894220601245896
- Akili eval: /workspace/ideas/2026-10-13-martyniuk-post-eval.md (box)

# Add log redaction (Microsoft.Extensions.Compliance.Redaction) to template ServiceDefaults

## Description

In the Architecture template's ServiceDefaults, wire Microsoft.Extensions.Compliance.Redaction (data classification + redactors, `EnableRedaction` on logging) and `[LogProperties]` / source-generated logging where relevant, so every generated web solution masks PII before logs and OTel export.

Motivation: CrunchIt (financial firm portal) handles client data.

## Checklist

- [ ] Define data classification taxonomy (e.g. PII / sensitive) in ServiceDefaults
- [ ] Register redactors (`AddRedaction`) and call `EnableRedaction` on logging
- [ ] Use `[LogProperties]` / source-generated `[LoggerMessage]` where relevant; watch log volume
- [ ] Confirm redaction applies before OTel log export
- [ ] Tests proving a classified property is redacted in log output
- [ ] Web apps only; do not add to CLIs

## Out of scope

- StaticAnalysis analyzers (KILL)
- Probes / ResourceMonitoring (KILL; ServiceDefaults covers health checks)
- Contextual Options (KILL; preview)
- AI.Evaluation (deferred until skill-evals needs a scoring layer)

## Session

- Created: 1416915 (2026-10-13)

## Notes

- Busara GREEN 2026-10-13 on Anton Martyniuk's dotnet/extensions post: https://x.com/AntonMartyniuk/status/2109894220601245896
- Akili eval: /workspace/ideas/2026-10-13-martyniuk-post-eval.md (box)

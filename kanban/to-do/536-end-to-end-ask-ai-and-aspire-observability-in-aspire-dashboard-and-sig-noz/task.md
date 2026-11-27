# End-to-end Ask AI and Aspire observability in Aspire Dashboard and SigNoz

## Description

When debugging live browser + Ask AI flows in local development, traces and logs currently stop at opaque HTTP boundaries and stay trapped in the ephemeral Aspire Dashboard:

1. Aspire overwrites `OTEL_EXPORTER_OTLP_ENDPOINT` for every launched service so telemetry goes only to the local Aspire Dashboard, never to a persistent SigNoz collector even when the operator's environment configures one.
2. `XaiChatUpstream` wraps `OpenAIClient.GetChatClient(model).AsIChatClient()` without `Microsoft.Extensions.AI` OpenTelemetry instrumentation, so neither Aspire nor SigNoz records `gen_ai` prompt messages, tool declarations, tool calls/results, model replies, token counts, or the upstream HTTP error body on failures (such as xAI 400 responses).
3. In WebAssembly mode, Ask AI tool execution (`CatalogAgentFunctions`) and form draft application (`FormPrefillScope`) run in the browser and leave no structured diagnostic trail in server/OTel logs unless an uncaught error occurs.

This parent tracks three focused child tasks that give developers and agents full end-to-end visibility across Aspire Dashboard and persistent SigNoz, without exposing sensitive prompt content or browser diagnostics outside Development/Testing.

## Propagation

likely

## Children

- **536-001** — Dual OTLP export so Aspire run-mode services export to both Aspire Dashboard and SigNoz
- **536-002** — Enable Microsoft.Extensions.AI OpenTelemetry GenAI spans and upstream error body logging on XaiChatUpstream
- **536-003** — Emit development browser diagnostic logs for Ask AI tool invocations and FormPrefillScope outcomes

## Notes

- Children `536-001` and `536-002` both touch `source/container-apps/aspire/projects/aspire-service-defaults/extensions.cs`, so run them sequentially (for example via `ganda task work-set --parent 536 --into feature/ask-observability`).
- Keep `Observability:*` secondary OTLP export and sensitive GenAI payload capture opt-in / Development-safe so generated apps never leak prompts or require SigNoz in Production.

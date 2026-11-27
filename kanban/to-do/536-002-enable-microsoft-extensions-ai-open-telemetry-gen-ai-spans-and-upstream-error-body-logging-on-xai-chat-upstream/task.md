# Enable Microsoft.Extensions.AI OpenTelemetry GenAI spans and upstream error body logging on XaiChatUpstream

## Description

Currently, `OpenAiXaiChatClientFactory` (`source/container-apps/web/features/agent-chat/xai-chat-client-factory-server.cs`) returns `client.GetChatClient(model).AsIChatClient()` directly without `Microsoft.Extensions.AI` OpenTelemetry instrumentation, and `aspire-service-defaults` does not subscribe to the `Microsoft.Extensions.AI` `ActivitySource`.

As a result:
1. Neither Aspire Dashboard nor SigNoz shows `gen_ai` spans with prompt messages, system instructions, tool declarations, tool calls/results, model replies, or token usage for Ask AI turns.
2. When xAI returns HTTP `400 Bad Request` (for example an invalid API key or bad schema), `XaiChatUpstream.CompleteAsync` logs only `ClientResultException.ToString()` (`Status: 400 (Bad Request)`), omitting the actual JSON error body returned by xAI (`GetRawResponse()?.Content`), making root cause invisible in Aspire and SigNoz logs.

## Propagation

likely

## Parent

536

## Requirements

1. **GenAI OpenTelemetry instrumentation on `XaiChatUpstream` / `OpenAiXaiChatClientFactory`:**
   - Wrap the `IChatClient` in `OpenAiXaiChatClientFactory.Create` (or `XaiChatUpstream`) using `Microsoft.Extensions.AI` 11.0.1's `ChatClientBuilder` + `.UseOpenTelemetry(loggerFactory, sourceName: ..., configure: client => { client.EnableSensitiveData = ...; })`.
   - Use a shared constant for the `ActivitySource` name (e.g. `TimeWarp.Foundation.Configuration` or `XaiChatDefaults` / `ServiceDefaults`, following the `AnalyticsMeters.MeterName` pattern in `aspire-service-defaults/extensions.cs`) and register it with `WithTracing(tracing => tracing.AddSource(...))` and `WithMetrics(metrics => metrics.AddMeter(...))` in `aspire-service-defaults` so GenAI spans and token metrics export to Aspire Dashboard and SigNoz.
   - Verify the exact `UseOpenTelemetry` API and default source/meter names in `Microsoft.Extensions.AI` 11.0.1 (using `dotnet-inspect` or compiler inspection — do not guess).

2. **Fail-closed sensitive payload gating:**
   - Add `public bool EnableSensitiveTelemetry { get; set; }` to `XaiChatOptions` (`source/container-apps/web/features/agent-chat/xai-chat-options-application.cs`), defaulting to `false` so Production/deployed apps **never** emit prompt or reply bodies by default.
   - Enable `"XAI": { "EnableSensitiveTelemetry": true }` in `source/container-apps/web/projects/web-server/appsettings.Development.json` (Development only) so local runs capture prompts, tool calls, tool results, and replies in Aspire and SigNoz out of the box.
   - Pass `enableSensitiveData` into `OpenAiXaiChatClientFactory` / `XaiChatClientCache` so changing the option works cleanly and cached clients reflect the setting (and never log API keys).

3. **Upstream HTTP error body logging on `XaiChatUpstream`:**
   - In `XaiChatUpstream.CompleteAsync` (`source/container-apps/web/features/agent-chat/xai-chat-upstream-server.cs`), when catching `System.ClientModel.ClientResultException`, extract `exception.Status` and a bounded snippet (e.g. capped at 512 characters, whitespace-normalized) of `exception.GetRawResponse()?.Content.ToString()` and log them in structured fields alongside the exception (never logging `Authorization` headers or API keys).
   - Keep existing `AgentChatProblems.UpstreamFailed()` client-facing response behavior and existing tests intact (the browser still never receives raw upstream exception text).

4. **Tests:**
   - Add unit/integration tests proving:
     - Calling through the instrumented `IChatClient` emits a GenAI `Activity` span under the registered `ActivitySource` name.
     - With `EnableSensitiveTelemetry = true`, prompt/response details are captured on the telemetry; with `EnableSensitiveTelemetry = false` (default), sensitive prompt/response text is omitted.
     - When `ClientResultException` is thrown with a raw HTTP response body (e.g., `400` `{"code":"invalid-argument","error":"Incorrect API key provided."}`), `XaiChatUpstream` logs the status code and bounded response body snippet while still returning `AgentChatProblems.UpstreamFailed()`.
   - Run `./bin/dev build` and targeted `./bin/dev test` with 0 warnings and 0 errors.

## Notes

- Key files:
  - `source/container-apps/web/features/agent-chat/xai-chat-client-factory-server.cs`
  - `source/container-apps/web/features/agent-chat/xai-chat-client-cache-server.cs`
  - `source/container-apps/web/features/agent-chat/xai-chat-upstream-server.cs`
  - `source/container-apps/web/features/agent-chat/xai-chat-options-application.cs`
  - `source/container-apps/web/projects/web-server/appsettings.Development.json`
  - `source/container-apps/aspire/projects/aspire-service-defaults/extensions.cs`
- Because `536-001` also edits `aspire-service-defaults/extensions.cs`, sequence `536-002` after `536-001` (e.g. via `ganda task work-set --parent 536`).

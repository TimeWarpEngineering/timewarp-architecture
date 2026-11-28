# Round 1 — general
**Date:** 2026-11-28
**Scope reviewed:** commit b2f480d13 (product source and tests), against task.md Requirements 1–4,
`## Goal (implementer restatement)`, and the recorded `## Results` / `### How to validate`.

## Summary

The change wraps the xAI `IChatClient` in Microsoft.Extensions.AI `UseOpenTelemetry` with a shared
`GenAiTelemetry.SourceName` (`TimeWarp.Architecture.GenAI`), which ServiceDefaults registers with
both `AddSource` and `AddMeter`. `EnableSensitiveTelemetry` defaults to false, is set true only in
web-server `appsettings.Development.json`, is passed explicitly to `EnableSensitiveData` (so the
`OTEL_INSTRUMENTATION_GENAI_CAPTURE_MESSAGE_CONTENT` default cannot turn it on), and is part of the
`XaiChatClientCache` key. A `ClientResultException` from either the key-resolution path or
`GetResponseAsync` now logs status and a whitespace-normalized, 512-character-capped content snippet
through `LoggerMessage.Define`, never reads headers, and still returns
`AgentChatProblems.UpstreamFailed()`. Risk is low. No issues found.

Checks made while reading:

- Restatement vs task: matches Requirements 1–4; no misread.
- Snippet loop: leading and trailing whitespace dropped, runs collapsed to one space, cap checked
  before both the space and the character, so the result never exceeds 512. The cap test
  (`ClientResult_Should_NormalizeAndCapTheBody`) exercises the boundary.
- Cache: `ClientCacheKey(ApiKey, EnableSensitiveTelemetry)` record struct, default equality is
  ordinal on the string, same as the old `StringComparer.Ordinal`. Eviction and release paths use
  `CachedChatClient.Key`, which was retyped consistently.
- Factory DI: `OpenAiXaiChatClientFactory` is a `TryAddSingleton` and now takes `ILoggerFactory`,
  a singleton; no lifetime problem.
- Tests: span source name, prompt and reply present on tags when on and absent when off, token
  histogram on the shared meter, flag-change rent, option flip through `CompleteAsync`, status and
  body structured fields, Authorization sentinel absent from message, state, and exception text,
  and appsettings binding per environment.
- Recorded gates: `./bin/dev build`, targeted tests for both touched test files, and
  `ganda repo audit` are recorded with outcomes. Template smoke runs in PR CI.

Observations, not findings:

- `extensions.cs` Design region: the analytics line changed from "the one" to "a deliberate
  deviation" and the GenAI line is unnumbered, while later lines still say "second", "third",
  "fourth". It still reads correctly.
- The error body is logged in every environment, independent of `EnableSensitiveTelemetry`. That is
  what Requirement 3 asks for; a 400 body could echo part of a request, bounded at 512 characters.

## Issues

<!-- None. -->

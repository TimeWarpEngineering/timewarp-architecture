# Round 1 — general
**Date:** 2026-09-14
**Scope reviewed:** branch task/218-drop-serilog-from-web-server-so-structured-logs-re vs origin/master (product commit 81aba4f5)

## Summary

web-server drops Serilog entirely so host `ILogger` + `AddOpenTelemetry` from `AddServiceDefaults()` is the only logging stack, matching api-server’s OTel path. Crash capture is preserved with try/catch (`ILogger` Critical after `Build()`, `Console.Error` before any logger exists); Purpose/Design regions match the code. Repo-wide product grep of `*.cs`/`*.csproj`/`*.props`/`*.json` is clean aside from the intentional Design-region warning; CPM pins and PackageReferences are gone; `Logging:LogLevel` maps the former Serilog MinimumLevel/category filters so `Request finished` stays visible. Low risk; no product defects found.

## Issues


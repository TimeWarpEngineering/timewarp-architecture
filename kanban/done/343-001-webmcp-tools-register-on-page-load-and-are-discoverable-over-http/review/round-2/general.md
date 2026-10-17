# Round 2 — general
**Date:** 2026-10-17
**Scope reviewed:** `1cfb5960a..94133f633` (fix commit `94133f633`: `web-mcp-discovery-catalog.cs`, `web-mcp-discovery-endpoint.cs`, `navigation-destinations-tests.cs`, `markdown-content-negotiation-tests.cs`, and task.md Results)

## Summary

The fix pass resolves both round-1 findings. `DocumentJsonAsync` now takes the `IActionCatalog` entries. It keeps an action only when the entry's visibility includes `Agent` and publishes the catalog entry name. That is the same filter `CatalogAgentToolSet` uses. A route row with no agent-visible action is dropped. A page action with no catalog entry throws. The server endpoint resolves `IActionCatalog` from the SPA services that `Web.Spa.Program.ConfigureServices` registers on the web-server host. The HTTP smoke proves this works. The registry test builds its expectation from the filtered list. It asserts that `Credentials.AddPasskey` is present and that the two human-only `/Settings` actions are absent. A new test reads the `## WebMCP` sections of `llms.txt`, `auth.md`, and the discovery skill. It requires every backticked catalog tool, every short tool id, and every Pascal route cited there to be in the generated document. Unknown backticked words fail by default, and three prose words are allowlisted. The test also requires at least one citation per file, so an empty or renamed section cannot pass silently.

Gates were re-run by this reviewer in the claim worktree:

- `./bin/dev build`: 0 warnings, 0 errors.
- `dotnet run source/container-apps/web/platform/agent-discovery/markdown-content-negotiation-tests.cs`: 6/6 passed, including `WebMcpDiscovery_Should_BeReadableWithoutJavaScript`.
- `dotnet run --project tests/container-apps/web/web-spa-integration-tests/web-spa-integration-tests.csproj`: 301/301 passed, 0 failed. This includes `Destinations_Should_` with the new `WebMcp_Sections_Should_Cite_Only_Published_Tools_And_Routes`.

The WebMCP sections were checked by hand against the regexes. `navigate`, `site_map`, `page_context`, `Credentials.AddPasskey`, `/Feedback`, and `/Settings` are the cited names. `/mcp`, `/.well-known/...`, and `#enable-webmcp-testing` do not match the patterns. `tools`, `status`, and `stub` are the allowlisted prose words.

## Prior findings

| ID | Severity | Round-2 status | Evidence |
|----|----------|----------------|----------|
| M1 | bug | fixed | `web-mcp-discovery-catalog.cs:83-122` filters by `ActionVisibility.Agent` and uses `entry.Name`. Asserted in `navigation-destinations-tests.cs:204-210` and `markdown-content-negotiation-tests.cs:176-178`. |
| M2 | suggestion | fixed | `navigation-destinations-tests.cs:227-303` checks the cited names in the WebMCP markdown against the generated document. |

## Issues

None new on the fix delta.

## Restatement check

The fix-pass Goal restatement adds two bullets for M1 and M2 and keeps the original "Done means" list. It matches the task. No misread found.

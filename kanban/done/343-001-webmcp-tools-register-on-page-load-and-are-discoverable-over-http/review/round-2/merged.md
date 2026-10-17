# Round 2 — merged findings
**Date:** 2026-10-17
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 1 | 0 |
| suggestion | 0 | 1 | 0 |
| nit | 0 | 0 | 0 |

## Issues

### M1 — Severity: bug — Status: fixed
- File: source/container-apps/web/projects/web-spa/features/application/agent/web-mcp-discovery-catalog.cs:79
- Description: `routeScopedTools` listed human-only actions as WebMCP tools.
- Source: general (round 1)
- Disposition notes: Fixed in `94133f633`. Route rows keep only `IActionCatalog` entries whose visibility includes `Agent`, under `entry.Name`. A route with no agent-visible action is dropped. An action missing from the catalog throws. The registry test and the HTTP smoke assert that `Credentials.AddPasskey` is present and that `Credentials.AddExistingPasskey` and `Credentials.LinkMicrosoft365` are absent. Re-verified in round 2: build 0/0, smoke 6/6, web-spa integration 301/301.

### M2 — Severity: suggestion — Status: fixed
- File: source/container-apps/web/projects/web-spa/wwwroot/llms.txt:124
- Description: Tool and route names in the WebMCP prose were not checked against the generated document.
- Source: general (round 1)
- Disposition notes: Fixed in `94133f633`. `WebMcp_Sections_Should_Cite_Only_Published_Tools_And_Routes` covers `llms.txt`, `auth.md`, and the discovery skill. Each backticked catalog tool, short tool id, and Pascal route must appear in `webmcp.json`. Unknown backticked words fail unless allowlisted, and each file must cite at least one name. Passed in round 2.

## Duplicates / conflicts

- None. No new findings on the fix delta.

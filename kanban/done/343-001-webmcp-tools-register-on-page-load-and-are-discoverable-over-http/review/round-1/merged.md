# Round 1 — merged findings
**Date:** 2026-10-17
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 1 | 0 | 0 |
| suggestion | 1 | 0 | 0 |
| nit | 0 | 0 | 0 |

## Issues

### M1 — Severity: bug — Status: open
- File: source/container-apps/web/projects/web-spa/features/application/agent/web-mcp-discovery-catalog.cs:79
- Description: `/.well-known/webmcp.json` `routeScopedTools` lists human-only actions as WebMCP tools. On `/Settings` these are `Credentials.AddExistingPasskey` and `Credentials.LinkMicrosoft365` (`ActionVisibility.Human`). `CatalogAgentToolSet` never registers them. The registry test (`navigation-destinations-tests.cs:480`) builds its expectation from the same unfiltered list, so it locks the wrong output in.
- Suggestion: Keep only catalog entries whose visibility includes `Agent`, using `IActionCatalog` names. Update the test to expect the filtered list, and assert that the two human-only names are absent and `Credentials.AddPasskey` is present.
- Source: general
- Disposition notes:

### M2 — Severity: suggestion — Status: open
- File: source/container-apps/web/projects/web-spa/wwwroot/llms.txt:124
- Description: The hand-written docs name `Credentials.AddPasskey` and the `/Feedback` and `/Settings` routes. Nothing checks those names against the generated document, so prose can drift from the registry. The task asks for generated docs or a build check.
- Suggestion: Have the HTTP or registry test assert that each tool and route named in the markdown appears in the generated `webmcp.json`.
- Source: general
- Disposition notes:

## Duplicates / conflicts

- None.

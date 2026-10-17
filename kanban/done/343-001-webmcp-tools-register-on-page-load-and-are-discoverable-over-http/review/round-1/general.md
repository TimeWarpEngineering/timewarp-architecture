# Round 1 — general
**Date:** 2026-10-17
**Scope reviewed:** `ffc54646f..b3a18de22` (product files plus task.md Results)

## Summary

The change adds a classic script that registers the real `navigate` tool during HTML parse, before Blazor starts. Blazor's later publish aborts that registration and replaces it. The change also adds an anonymous `/.well-known/webmcp.json` built from `PageRegistry.ActionScopes` and `AgentSiteMap`, plus WebMCP and agent-registration sections in `llms.txt`, `auth.md`, and the discovery skill. The page-load path looks right: it checks the url against the enum, asks with `window.confirm`, allows only same-origin moves, and escapes `<` in the inline JSON. It does nothing when the browser has no model context. The server card stays a stub. The endpoints that `auth.md` names exist (`api/identity/agent/register/options`, `agent/token/options`, `agent/me`, `identity/session`). The main defect is that the generated discovery document lists human-only actions as WebMCP tools.

Gates were not re-run in this round. The implementer reports `dev build` 0/0, `dev test` 23/23, `template-smoke`, and `ganda repo audit`. Round 2 should re-verify them after the fix pass.

## Issues

### Issue 1 — Severity: bug
- File: source/container-apps/web/projects/web-spa/features/application/agent/web-mcp-discovery-catalog.cs:79
- Description: `routeScopedTools` lists every `scope.ActionTypes` entry. `PageRegistry.ActionScopes` also holds human-only actions. The `PageAgentScope` Design region says "Human-only actions may be listed; the visibility filter, not this type, drops them". `CatalogAgentToolSet` keeps only entries where `Visibility.HasFlag(ActionVisibility.Agent)` (`catalog-agent-tool-set.cs:95`, `:124`). Two `/Settings` actions are `ActionVisibility.Human` and never become WebMCP tools: `CredentialsState.AddExistingPasskeyActionSet` (`credentials-state.add-existing-passkey.cs:30`) and `LinkMicrosoft365ActionSet` (`credentials-state.link-microsoft-365.cs:33`). The document still advertises `Credentials.AddExistingPasskey` and `Credentials.LinkMicrosoft365` as `/Settings` tools. An agent that reads `webmcp.json` will look for tools that are never registered. That breaks the task's "keep the card honest" and "no fake tool" intent. The drift test in `navigation-destinations-tests.cs:480` builds its expected list from the same unfiltered `ActionTypes`, so it locks the wrong output in.
- Suggestion: Filter each scope's actions to catalog entries whose visibility includes `Agent`. Take the names from `IActionCatalog` (`entry.Name`) and pass the catalog into `DocumentJsonAsync`, or resolve it in the endpoint. A route row with no agent-visible action can be dropped or left with an empty list. Pick one and document it. Update the test to expect only agent-visible names. Also assert that `Credentials.AddExistingPasskey` and `Credentials.LinkMicrosoft365` are absent and that `Credentials.AddPasskey` is present.
- Status: open

### Issue 2 — Severity: suggestion
- File: source/container-apps/web/projects/web-spa/wwwroot/llms.txt:124
- Description: The task asks that docs which name tools or routes be generated or build-checked. `llms.txt` names the tool `Credentials.AddPasskey` in hand-written prose, and nothing checks it. If someone renames that action or removes it from `/Settings`, the generated document changes but `llms.txt` keeps the stale name. The `/Feedback` and `/Settings` routes are only loosely covered: the HTTP test requires `webmcp.json` to contain those substrings.
- Suggestion: In `WebMcpDiscovery_Should_BeReadableWithoutJavaScript` (or the registry test), assert that each tool and route the markdown names (`Credentials.AddPasskey`, `/Feedback`, `/Settings`, `navigate`, `site_map`, `page_context`) also appears in the generated `webmcp.json`. Then prose drift fails the build.
- Status: open

## Restatement check

The Goal restatement matches the task. It keeps the card a stub, puts honest page-load registration on `/`, adds a WebMCP section to all three docs with inspector steps, describes auth.md public-key registration without inventing OAuth, adds an HTTP smoke, and defers the live re-scan. No misread found.

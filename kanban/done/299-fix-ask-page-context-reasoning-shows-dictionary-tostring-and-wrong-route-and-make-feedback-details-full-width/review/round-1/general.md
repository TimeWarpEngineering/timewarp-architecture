# Round 1 — general
**Date:** 2026-10-10
**Scope reviewed:** same as framework (source/ and tests/ diff, plus call sites of PageAgentRoute, WebMcpDispatcher, CatalogAgentFunctions, TimeWarpPage)

## Summary

The change formats tool arguments as JSON (empty arguments hidden), adds a scoped `PageAgentRoute`
that the shell fills from `window.timeWarpPagePath` on WebAssembly, adds Feedback facts to
`page_context`, and widens the Details textarea. The argument rendering, the Feedback facts
(own filings only, no body text, cleared on sign-out), and the CSS look right. The risk is in
`PageAgentRoute`: once a path is observed it wins over the live `NavigationManager` everywhere,
and the browser path is not made base-relative.

## Issues

### Issue 1 — Severity: suggestion
- File: source/container-apps/web/projects/web-spa/features/application/agent/page-agent-route.cs:52
- Description: After the first `Observe`, `PathOr` and `Current` return the observed `Path` and never go back to the live `NavigationManager`. Observation only happens in `WebMcpAgentSurface` (renders and LocationChanged) and in `AgentAsk` renders. `TimeWarpFocusedPage` hosts neither, so after one focused page hops to another the route stays on the first. On InteractiveServer the observed path is just the same circuit manager's path read earlier, so it can only be equal or older than the live one. `WebMcpDispatcher.InvokeTool` and `IsOnPath` used the live manager before this change. The dispatcher's design says it re-selects for the current route because the browser agent is not trusted, so a stale route weakens that check.
- Suggestion: Re-read the route when it is asked for. Keep the in-process runtime from `Observe` and read `timeWarpPagePath` again in `PathOr`. When the path came from a `NavigationManager`, return that manager's live path if the caller passes the same instance. Keep the observed path only for a different manager, which is the stuck-provider case the integration test covers.
- Status: open

### Issue 2 — Severity: suggestion
- File: source/container-apps/web/projects/web-server/components/App.razor:69
- Description: `timeWarpPagePath` returns `location.pathname`, which is absolute. `PageAgentScope.FromNavigation` returns the base-relative path (`ToBaseRelativePath`). `<base href="/">` makes them equal today, but an app generated from this template and hosted under a path base would see `/app/Feedback` instead of `/Feedback`, so no page tools and no page facts.
- Suggestion: Remove the `document.baseURI` path prefix in the script so both sources agree.
- Status: open

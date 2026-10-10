# Round 1 — merged findings
**Date:** 2026-10-10
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 0 | 0 |
| suggestion | 0 | 2 | 0 |
| nit | 0 | 0 | 0 |

## Issues

### M1 — Severity: suggestion — Status: fixed
- File: source/container-apps/web/projects/web-spa/features/application/agent/page-agent-route.cs:52
- Description: An observed route always beats the live NavigationManager, so it goes stale on focused-page hops and on InteractiveServer. The WebMCP dispatcher re-selection and IsOnPath checks used the live route before this change.
- Suggestion: Re-read the browser path when asked. Use the live manager when it is the one that was observed. Keep the observed path only for a different (stuck) manager.
- Source: general
- Disposition notes: fixed on this task. PageAgentRoute keeps the in-process runtime and re-reads timeWarpPagePath in PathOr. It returns the live path when the caller holds the observed NavigationManager and keeps the stored path only for a different manager. Current delegates to PathOr. Regression test: Route_Follows_The_Observed_Manager_And_Keeps_The_Path_For_A_Stuck_One.

### M2 — Severity: suggestion — Status: fixed
- File: source/container-apps/web/projects/web-server/components/App.razor:69
- Description: timeWarpPagePath is absolute (`location.pathname`), but FromNavigation is base-relative. Under a non-root base href they disagree.
- Suggestion: Remove the document.baseURI path prefix in the script.
- Source: general
- Disposition notes: fixed on this task. App.razor timeWarpPagePath removes the document.baseURI pathname prefix, so it matches ToBaseRelativePath.

## Duplicates / conflicts

- None.

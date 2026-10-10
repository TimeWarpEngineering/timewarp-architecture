# Round 2 — general
**Date:** 2026-10-10
**Scope reviewed:** fix delta for M1 and M2 (`page-agent-route.cs`, `App.razor`, new test in `catalog-agent-tests.cs`), re-verified against the round-1 findings

## Summary

M1: `PathOr` re-reads the browser path when `Observe` found one. It returns the live path when
the caller holds the manager that was observed, and keeps the stored path only for a different
manager. `Current` delegates to `PathOr`, so the WebMCP dispatcher, the publisher, Ask, and the
catalog functions read the route the same way. The stuck-manager integration test still passes.
The new test proves a hop with no observer follows the observed manager. M2: the script now
removes the `document.baseURI` path, so it matches `ToBaseRelativePath`. With `<base href="/">`
the output is the same. No new issues.

Evidence: `./bin/dev build` 0 warnings 0 errors; `web-spa-integration-tests` 184 passed, 0 failed;
`web-spa-playwright-tests` 6 passed, 0 failed (includes the Ask `/Feedback` WASM trip and the
Details width check).

## Issues

<!-- none -->

# Round 2 — general
**Date:** 2026-11-04
**Scope reviewed:** fix delta `7d1df50ac..10ec4bcb6` (round-1 fix pass, 25 product test files plus the
deleted `host-graph-lease.cs`). M1–M9 were re-checked against the post-fix tree.

## Summary

The fix pass resolves all nine round-1 findings. `HostGraphLease` is gone (`git grep` finds nothing).
Each of the six split web-server classes has a `static HostGraph?` that SetupOnce creates and
CleanUpOnce disposes, the same as the master originals. The fixtures hold no host state, and their
helpers take `WebTestServerApplication web`. The Playwright helpers are generic on `TTest`, so each
class writes screenshots to its own folder. The partial-aware filter in `aspire-publish-targets-tests`
is correct for `public partial class` and `public static partial class`. The reviewer re-ran the
gates in this worktree: `dev build` had 0 warnings and 0 errors, fast passed 14 of 14 projects, and
integration passed 8 of 8 (web-server-integration-tests passed). No file is over 500 lines. Playwright
was not re-run; the implementer reports 33 of 33.

One new nit: the M4 trim left the two protected-page-deep-link halves with their Design prose outside
a `#region Design`. The master original had that region.

## Issues

### Issue 1 — Severity: nit
- File: tests/container-apps/web/web-server-integration-tests/features/identity/protected-page-deep-link-pages-tests.cs:6
- Description: The master `protected-page-deep-link-tests.cs` wrapped its design prose in
  `#region Design … #endregion`. Both split halves (`protected-page-deep-link-pages-tests.cs:6-19` and
  `protected-page-deep-link-settings-tests.cs:6-21`) keep that prose as loose `//` lines after the
  Purpose region, with no Design region. Tooling and agents that read Design regions
  (tw-agent-context-regions) do not see it. The Goal says context regions are kept. Each half's
  fixture and the other split files in the delta all have a Design region.
- Suggestion: Wrap the loose comment block in each file in `#region Design` / `#endregion`.
- Status: open

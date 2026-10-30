# Round 2 — general
**Date:** 2026-10-29
**Scope reviewed:** delta since round-1 base `3f596ae40` (HEAD is still `3f596ae40`; no product commits, no product working-tree changes). Re-verified round-1 M1 and M2 against the unchanged tree.

## Summary

The fix pass (`implement-fix-2`, implementer-grok, 1 turn, 11s) reported Done but changed nothing. `git diff 3f596ae40 HEAD` touches only kitchen files (`review/review-framework.md`, `task.md`). The ingress routes behind M1 and M2 are the same as in round 1. Both findings stay open. No new findings, because the delta has no product code.

## Issues

### Issue 1 (carries M1) — Severity: bug
- File: source/container-apps/yarp/program.cs; source/container-apps/aspire/projects/aspire-app-host/program.cs:860-861
- Description: Still no exact `/api/health` → web-server ingress route. Public `GET /api/health`, which the OpenAPI document advertises, still falls through to Api.Server's catch-all.
- Suggestion: Same as round-1 M1.
- Status: open

### Issue 2 (carries M2) — Severity: bug
- File: source/container-apps/yarp/program.cs:108,115; source/container-apps/aspire/projects/aspire-app-host/program.cs:860-861
- Description: The duplicate `"/api"` and `"/api/"` ingress routes are still there. The documented `GET|POST /api` alias still hits the AmbiguousMatch 500 at the public origin.
- Suggestion: Same as round-1 M2.
- Status: open

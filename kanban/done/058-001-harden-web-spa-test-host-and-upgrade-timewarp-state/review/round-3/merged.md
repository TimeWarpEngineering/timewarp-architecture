# Round 3 — merged findings
**Date:** 2026-09-23
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 0 | 0 |
| suggestion | 0 | 0 | 0 |
| nit | 0 | 0 | 0 |

## Issues

No findings. Scope was the product delta since round 2 (`b494fcf4..HEAD`): commit `4a34830b` (yarp `WithHttpHealthCheck` readiness gate, Design regions in `base-test.cs` / `ingress-smoke-tests.cs`) and commit `c0b72263` (`#if(api)` gating of the SPA test-host mock-auth block and the Authentication / Configuration global usings). Reviewer re-verified against the repo and the decompiled Aspire 13.5.4 packages: the health check sits in the same web-gated block as the `/` catch-all route; `AddYarp` always creates the `http` endpoint and registers no health check of its own; no builder-graph `WaitFor` edge targets the ingress, so no ordering change or deadlock; every other test file that uses the now-gated symbols is already excluded by `template.json` when `api` is off; no template-conditional token leaked into comment prose (TWA0008); the test csproj defines `api` (TWA0010).

Orchestrator gate re-check on the clean worktree at `10ad134c`: `dotnet run tools/dev-cli/dev.cs -- build` 0 warnings / 0 errors, exit 0.

## Duplicates / conflicts

- Single reviewer. Rounds 1 and 2 raised no `M#` ids to carry. Nothing to collapse.

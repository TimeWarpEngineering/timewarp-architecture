# Round 3 — fix verification (commit 285559de)
**Date:** 2026-07-31
**Sources:** fix agent (killed mid-gates; work taken over by orchestrator) + orchestrator gate runs

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 3 | 0 |
| suggestion | 0 | 2 | 0 |
| nit | 0 | 1 | 0 |

## Issues

### R2-1 — bug — fixed
- TRUE root cause (deeper than round-2's hypothesis): same-TargetPath appsettings.json
  content-flattening collision — Api.Server's file (no SampleOptions) shadows Web.Server's in
  any consumer referencing both transitively; invisible to web-server-integration-tests
  (references Web.Server alone — why round-1's env passed). Fix:
  msbuild/project-directory-metadata.props bakes each server's $(MSBuildProjectDirectory)
  into AssemblyMetadata; ProjectContentRoot resolves ContentRootPath from it (safe fallback).
  Product csproj touch (web-server/api-server/yarp) justified: metadata is evaluated at the
  generated app's own build time; template-smoke proves it.
### R2-2 — bug — fixed structurally by R2-3 (suite excluded under (!api) AND (!web)).
### R2-3 — suggestion — fixed: file relocated to suite-shaped tests/common/timewarp-testing-tests
  (Jaribu MTP, global.json sdk-pin mirrored); api-jaribu-tests expected back to 2;
  CoLocatedTestFiles restored.
### R2-4 — suggestion — fixed: behavioral authenticated-request assertion (suite now 3 tests).
### R2-5 — nit — fixed: count comments reconciled (command, harness, skill).
### R3-1 — bug (NEW, found during fix verification) — fixed
- Template stripping of adjacent family-conditional regions stacked separator blank lines →
  IDE2000 errors in generated apps (SmokeNoApi caught it on the first green-path run).
  Seams moved inside regions in host-graph-factory.cs AND host-graph.cs (incl. the latent
  --web false seam no smoke matrix exercises yet — noted for a future SmokeNoWeb entry).

## Gate results (orchestrator-run, clean fix worktree)

dev build 0/0 · FULL dev test green (19 projects incl. api-jaribu 2/2, web-jaribu 5/5,
timewarp-testing-tests 3/3, all Fixie suites) · template-smoke SUCCEEDED ×3 matrices
(api-dependent suites correctly excluded under --api false) · ganda repo audit 23/23.

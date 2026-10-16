# Round 1 — general
**Date:** 2026-10-16
**Scope reviewed:** branch `task/373-fix-web-off-generated-app-test-projects-and-add-a` vs `master` (19119532f), product files only

## Summary

The change excludes the web-only `ingress-smoke-tests.cs` under `!web`, moves the usings only
that file needs into `#if(web)` in `aspire-tests/global-usings.cs`, and narrows
`EnsureYarpPortsAreFree` to `web && yarp`. It also moves the YARP config using into `#if(web)`
next to its only consumer, and adds SmokeNoWeb, SmokeNoGrpc and SmokeNoYarp to `SmokeMatrix`.
The fixes use regions and excludes, not suppressions. Risk is low.

Checks:
- The `DefineConstants web` added to `aspire-tests.csproj` follows the precedent in
  `timewarp-testing.csproj` (`api;yarp;web`).
- `AssertMockAuthFailClosedSurfaces` checks only web-server and web-spa paths, so skipping it
  for `web` excluded loses no api coverage.
- `ExcludedFamilies ["web"]` lines up with the harness's existing `"web"` family entries.

Re-verified in this review:
- The SmokeNoWeb artifact has no `ingress-smoke-tests.cs`, and its generated `global-usings.cs`
  is stripped.
- `dotnet build tests/container-apps/aspire/aspire-tests -c Release` in that artifact: 0 Warning(s), 0 Error(s).
- `ganda repo audit` passes.

Goal restatement vs task: consistent, no misread.

## Issues

None.

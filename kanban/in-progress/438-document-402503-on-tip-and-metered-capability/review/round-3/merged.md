# Round 3 — merged findings
**Date:** 2026-10-29
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 1 | 0 | 0 |
| suggestion | 0 | 2 | 0 |
| nit | 1 | 1 | 0 |

## Issues

### M1 — Severity: suggestion — Status: fixed
- File: source/container-apps/web/platform/payment/open-api-payment-responses-server.cs:85
- Description: The documented challenge `resource.url` was a fixed route, not the configured
  `TipOptions.Resource` / `MeteredCapabilityOptions.Resource`.
- Source: general (round 1)
- Disposition notes: Fixed in `0264769f3`; unchanged since. Re-verified round 3.

### M2 — Severity: suggestion — Status: fixed
- File: tests/container-apps/web/web-server-integration-tests/open-api-payment-responses-tests.cs:240
- Description: The test stubs hard-coded the routes instead of using the contract `RouteTemplate`
  constants.
- Source: general (round 1)
- Disposition notes: Fixed in `0264769f3`; unchanged since. Re-verified round 3. Keep this when fixing
  M4: the stubs must stay on the real routes because the transformer matches on `RelativePath`.

### M3 — Severity: nit — Status: fixed
- File: source/container-apps/web/platform/payment/open-api-payment-responses-server.cs:36
- Description: The dead `?? "/api/tip"` fallback.
- Source: general (round 1)
- Disposition notes: Removed in `0264769f3`; unchanged since.

### M4 — Severity: bug — Status: open
- File: tests/container-apps/web/web-server-integration-tests/open-api-payment-responses-tests.cs:236
- Description: The four public namespace-scope stub endpoints in the shared integration-test assembly
  are picked up by api-server's FastEndpoints auto-discovery (api-server/program.cs:101 sets
  `Assemblies` without `DisableAutoDiscovery`). Every in-process api-server host in the test process
  then fails at startup with `Duplicate routes detected!` for `GET:api/tip`, `POST:api/tip`,
  `GET:api/demo/metered-capability`, and `GET:/api/hello`. CI `ci (integration)` on PR #600 failed
  twice (199 of 403), and `--filter-class AgentProtectedEndpoint_` reproduces it locally (5 of 5 fail).
- Suggestion: Mirror web-server and set `DisableAutoDiscovery = true` in api-server's `AddFastEndpoints`
  with its explicit `Assemblies`, after confirming api-server needs no auto-discovered endpoints; or
  otherwise make the stubs invisible to other hosts' discovery. Keep the stubs on the contract
  `RouteTemplate` constants. Prove with the whole integration group
  (`dotnet run tools/dev-cli/dev.cs -- test --group integration`), at minimum
  `--filter-class AgentProtectedEndpoint_` and `--filter-class OpenApi` both green.
- Source: general (round 3, Issue 1)
- Disposition notes:

### M5 — Severity: nit — Status: open
- File: source/container-apps/web/platform/payment/open-api-payment-responses-server.cs:41
- Description: `FreeRoutesNever402` repeats the two routes as literals next to `TipRoute` /
  `MeteredRoute`. The info-description test pins the constant-derived paths, so drift is caught, but
  the copies agree by hand.
- Suggestion: Build the sentence from the route constants, or mark wontfix with the test as the guard.
  Optional; must not delay M4.
- Source: general (round 3, Issue 2)
- Disposition notes:

## Duplicates / conflicts

- None. M4 is the CI failure already recorded on `task.md` (`### CI failure (open, must fix)`); it is
  now a tracked finding so the fix pass has an id.

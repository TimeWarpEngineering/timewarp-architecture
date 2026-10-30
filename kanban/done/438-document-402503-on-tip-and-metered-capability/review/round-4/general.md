# Round 4 — general
**Date:** 2026-10-29
**Scope reviewed:** fix delta since round 3 base `e9f8e11bc` (round-3/base.sha) to `866e57a34`, outside `kanban/`.

## Summary

The fix pass requested in round 3 produced no product change. `git diff e9f8e11bc..HEAD -- . ':!kanban/'`
is empty (0 lines). The only commit since round 3 is `866e57a34`, which edits `task.md` (two Session and
Results lines). The implement-fix-2 oracle returned Done in 26 ms (`task-work.progress.log`), the same
duration and the same session id (`01a18528-…`) as the resumed implement oracle before round 3, so the
host appears to have replayed a journaled result instead of running an implementer. Because the product
tree is identical to the tree round 3 reproduced the failure on, the round-3 evidence stands without a
re-run: M4 and M5 remain open as written. No new findings; nothing was changed that could introduce one.

Static re-verification on this tree:

- `tests/container-apps/web/web-server-integration-tests/open-api-payment-responses-tests.cs:236,251,266,282`
  still declare `public sealed class TipGetEndpoint / TipPostEndpoint / MeteredCapabilityEndpoint /
  HelloEndpoint : EndpointWithoutRequest` at namespace scope.
- `source/container-apps/api/projects/api-server/program.cs:104` still sets `options.Assemblies` with no
  `DisableAutoDiscovery`; only `web-server/program.cs:348` has it.
- `source/container-apps/web/platform/payment/open-api-payment-responses-server.cs:39` still defines
  `FreeRoutesNever402` with route literals.

## Issues

### Issue 1 — Severity: bug (carries M4)
- File: tests/container-apps/web/web-server-integration-tests/open-api-payment-responses-tests.cs:236
- Description: Unchanged since round 3. The four public stub endpoints are auto-discovered by every
  in-process api-server host and register duplicate routes (`GET:api/tip`, `POST:api/tip`,
  `GET:api/demo/metered-capability`, `GET:/api/hello`), so `ci (integration)` fails deterministically
  (PR #600, run 40803144337, 199 of 403 twice; locally `--filter-class AgentProtectedEndpoint_` 5 of 5).
- Suggestion: As in round 3: set `DisableAutoDiscovery = true` with explicit `Assemblies` in api-server
  (mirroring web-server) after confirming api-server needs no auto-discovered endpoints, or otherwise keep
  the stubs out of other hosts' discovery. Keep the stubs on the contract `RouteTemplate` constants (M2).
  Prove with `dotnet run tools/dev-cli/dev.cs -- test --group integration`; at minimum
  `--filter-class AgentProtectedEndpoint_` and `--filter-class OpenApi` both green.
- Status: open

### Issue 2 — Severity: nit (carries M5)
- File: source/container-apps/web/platform/payment/open-api-payment-responses-server.cs:39
- Description: Unchanged since round 3. `FreeRoutesNever402` repeats the tip and metered routes as literals
  next to `TipRoute` / `MeteredRoute`.
- Suggestion: Build the sentence from the route constants, or mark wontfix with the info-description test as
  the guard. Must not delay M4.
- Status: open

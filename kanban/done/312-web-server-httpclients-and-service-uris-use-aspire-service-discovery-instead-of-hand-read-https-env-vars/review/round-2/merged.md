# Round 2 — merged findings
**Date:** 2026-10-12
**Sources:** general (round 2 re-review of fix commit 140ab824f)

## Counts (final)

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 1 | 0 |
| suggestion | 0 | 2 | 0 |
| nit | 0 | 3 | 1 |

## Prior findings (carried from round-1/merged.md)

| ID | Severity | Status | Disposition notes |
|----|----------|--------|-------------------|
| M1 | bug | fixed | 140ab824f. The self-call forwards `X-Forwarded-Host` (port kept) and `X-Forwarded-Proto` from `HttpAppBaseUrlAccessor.GetRequestOrigin`, each only when absent. RP-ID still strips the port. 3 new handler tests, 2 rewritten. Design regions corrected. |
| M2 | suggestion | fixed | 140ab824f. `web-spa-integration-tests/features/superhero/superhero-grpc-service-provider-tests.cs` (3/3): WASM `/grpc/` prefix, server shape rebased before discovery with HTTP/2 exact, unregistered client throws. Excluded under `!grpc`. |
| M3 | suggestion | fixed | 140ab824f. `yarp-integration-tests/grpc-route-ingress-path-tests.cs` ties the yarp Development `GrpcRoute` to `IngressPaths.GrpcPathPrefix`. Excluded under `!grpc`. |
| M4 | nit | wontfix | The test host cannot run `Main`, so it mirrors the two discovery calls `AddServiceDefaults` makes. The comment says so. Production wiring is covered by the publish output check (`services__*__http__0`) and the Aspire suites. A shared helper would add a test-only seam to service defaults for little gain. Decided by: review orchestrator. |
| M5 | nit | fixed | 140ab824f. "loopback" → "self-call" across the listed Design regions plus 3 more sites. |
| M6 | nit | fixed | 140ab824f (web-server). Boyscout follow-up: the review commit also makes `web-spa.csproj` and `timewarp-testing.csproj` append `$(DefineConstants)`. |
| M7 | nit | fixed | 140ab824f. The program.cs Design note records the pass-through DNS fallback and the TLS name-validation guard. |

## New issues

None (round-2 general.md).

## Additional verification (orchestrator)

- `dev build` after the boyscout csproj edits: 0 warnings, 0 errors.
- `dev template-smoke`: SUCCEEDED (web-jaribu 270/270).
- Manual `--grpc false` generation (smoke packages): both new test files are excluded and the Release build has 0 errors. The only warnings are git-metadata ones, because /tmp is not a git repo.

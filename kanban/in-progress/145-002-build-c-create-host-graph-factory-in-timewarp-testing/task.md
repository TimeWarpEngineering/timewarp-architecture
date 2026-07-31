# Build C-create host graph factory in timewarp-testing

## Description

The lifetime primitive for zero-Fixie (parent 145;
`kanban/done/143-research-aspire-and-jaribu-assembly-fixture-strategy-for-zero-fixie/findings.md` §3).
A factory module in `tests/common/timewarp-testing` that CREATES per-class-owned, correctly-ordered
host graphs — replacing what Fixie's DI graph did implicitly.

## Requirements

1. `HostGraphFactory`: async creation for Api-only; Web+Api (Api first); Web+Api+Yarp (Yarp last).
   Explicit ordering; `HostGraph` IAsyncDisposes reverse order. NO process statics / refcounting.
2. Per-graph override hook via `Action<IServiceCollection>?` per host.
3. Consumption: SetupOnce stores HostGraph; CleanUpOnce disposes — weather exemplar converted.
4. Fixed ports; port-free teaching error.
5. Documented in tw-feature-placement.

## Checklist

- [x] Factory + owner disposal (Api / Web+Api / Web+Api+Yarp)
- [x] Per-host override + MockAccessTokenProvider proven (`host-graph-factory-tests.cs`)
- [x] Exemplar converted; standalone + aggregator green
- [x] dev build 0/0; docs updated; review clean; kanban committed

## Session

- Orchestration 2026-07-31: implement + verify + review disposition clean

## Results

### Summary

`HostGraphFactory` + `HostGraph` land C-create in timewarp-testing. Weather uses `CreateApiAsync`;
factory smoke uses `CreateWebWithApiAsync` with MockAccessTokenProvider + override hook. Host
`ContentRootPath` set to server assembly output so Jaribu runfiles load appsettings (SampleOptions).
WebApplicationHost startup races RunAsync faults vs ApplicationStarted for clearer errors.

### Files

| Path | Role |
|------|------|
| `tests/common/timewarp-testing/host-graph-factory.cs` | Create* + EnsurePortIsFree |
| `tests/common/timewarp-testing/host-graph.cs` | Owner + reverse DisposeAsync |
| `applications/*-test-server-application.cs` | Optional configureServices; ContentRootPath |
| `web-application-host.cs` | Startup race / real exception surfacing |
| `get-weather-forecasts-tests.cs` | CreateApiAsync exemplar |
| `api/features/host-graph/host-graph-factory-tests.cs` | Web+Api + Mock + override |
| `template-smoke-harness.cs` | api expected 4; co-located list |
| `skills/tw-feature-placement/SKILL.md` | HostGraphFactory note |

### Verification

| Gate | Result |
|------|--------|
| weather standalone | 2/2 |
| host-graph-factory standalone | 2/2 |
| api-jaribu-tests `dotnet test` | 4/4 |
| solution build | 0/0 |
| web-server-integration-tests | 97 passed, 1 skipped |

### Review

Effort 1, round 1, **clean** — `review/`

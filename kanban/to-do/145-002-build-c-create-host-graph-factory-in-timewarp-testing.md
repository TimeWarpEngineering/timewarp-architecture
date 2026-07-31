# Build C-create host graph factory in timewarp-testing

## Description

The lifetime primitive for zero-Fixie (parent 145; kanban/done/143-research-aspire-and-jaribu-assembly-fixture-strategy-for-zero-fixie/findings.md §3). A factory module in
tests/common/timewarp-testing that CREATES per-class-owned, correctly-ordered host graphs —
replacing what Fixie's DI graph did implicitly (IWebApiTestService force-resolve;
YarpTestServerApplication(web, api) ctor).

## Requirements

1. `HostGraphFactory` (name negotiable): async creation methods for the real graphs —
   Api-only; Web+Api (Api boots first — BFF base addresses); Web+Api+Yarp (Yarp last).
   Explicit code ordering; returns an owner object that IAsyncDisposables the whole graph in
   reverse order. NO process statics, NO refcounting (C-create semantics — see findings M1).
2. Per-graph override hook: caller passes the configureServicesDelegate-style overrides per
   host (MockAccessTokenProvider etc.) so "mock only externalities" carries over unchanged.
3. Consumption shape: Jaribu class `SetupOnce` creates + stores in static field;
   `CleanUpOnce` disposes + nulls — same discipline as get-weather-forecasts-tests.cs.
   Convert that exemplar to the factory (Api-only case) as the worked example.
4. Fixed ports unchanged (7000/7255/8443); factory asserts port free with a clear teaching
   error if not (serialized execution contract).
5. Document in tw-feature-placement (with 145-001's note; whichever lands second reconciles).

## Checklist

- [ ] Factory + owner disposal implemented (Api / Web+Api / Web+Api+Yarp)
- [ ] Per-host override hook proven with MockAccessTokenProvider in a test
- [ ] Exemplar runfile converted; standalone + aggregator + template-smoke green
- [ ] dev build 0/0; full dev test green; docs updated; kanban committed

# Co-located Jaribu runfile preamble (the `tests` layer)


This is the canonical in-repo home for the co-located Jaribu runfile authoring convention
(task 135) — the cross-repo `tw-jaribu` skill covers Jaribu itself (test attributes, naming,
assertions) but not this repo-specific preamble; updating it there is tracked as a follow-up, not
duplicated here. Reference implementations:
`source/container-apps/web/features/admin/roles/create-role/create-role-tests.cs` (host-free
contract round-trip) and
`source/container-apps/api/features/weather-forecast/get-weather-forecasts/get-weather-forecasts-tests.cs`
(real host, fixed port 7255, class-scoped `SetupOnce`/`CleanUpOnce` for host dispose —
requires `TimeWarp.Jaribu` ≥ 1.0.0-beta.14) — read one before writing a new co-located test.

**C-create host lifetime (epic 145 / task 143 §6):** when a test class needs ASP.NET hosts
(in-proc lane), **each class owns its own graph**:

- `SetupOnce` creates hosts via **`HostGraphFactory`** (`tests/common/timewarp-testing`,
  task **145-002**): `CreateApiAsync` / `CreateWebWithApiAsync` / `CreateWebApiYarpAsync` —
  explicit Api→Web→Yarp order; returns a **`HostGraph`** (`IAsyncDisposable`).
- `CleanUpOnce` **must** `await graph.DisposeAsync()` (reverse order) and null the static.
  Never leave fixed-port hosts to process exit.
- **Never share** ASP.NET hosts across classes via process-static / `Lazy` / assembly singletons
  (that was the Fixie mental model; TimeWarp.Fixie actually rebuilt the provider **per class**
  anyway — see task 143). Ad hoc process-static sharing is never the answer — use **C-share**
  (below) when sharing is actually warranted.
- **Documented exception (no dispose required):** Testcontainers postgres process-static
  `Lazy` in foundation/infra tests — Ryuk reaps containers at process exit; do **not** cite that
  as precedent for sharing Kestrel hosts.

**C-share host lifetime (task 145-008 — Jaribu session-scoped fixtures, `TimeWarp.Jaribu` ≥
1.0.0-beta.15):** C-create (above) is still the **default** for any test class — one owned host
graph per class, disposed in `CleanUpOnce`. Reach for C-share only when a suite is genuinely
**expensive AND multi-class closed-box** (e.g. a full `Aspire.Hosting.Testing`
`DistributedApplication` boot shared by several test classes in the same suite — the SPA suite's
`SpaSessionFixture` is the shipped exemplar, `tests/container-apps/web/web-spa-integration-tests/infrastructure/spa-session-fixture.cs`).
Do not reach for it just because a fixture is mildly annoying to construct — C-create's per-class
in-proc `HostGraphFactory` boots are already cheap; session sharing is for the closed-box tax.

- **Base type:** `SessionHostFixture<TInner>` (`tests/common/timewarp-testing/session-host-fixture.cs`)
  — a suite declares ONE sealed subclass whose `CreateAsync()` delegates to the EXACT SAME
  factory function its C-create callers already use (no duplicated boot logic), so the
  per-class and session-scoped shapes never drift apart.
- **Explicit registration, once per suite:** call `RegisterSessionFixture<TFixture>()` from a
  single dedicated `[ModuleInitializer]` (its own file, separate from each class's own
  `RegisterTests<TClass>()` initializer — Jaribu's `Register` throws on double-registration).
- **Consuming classes:** `SetupOnce` calls `await SessionFixture.GetAsync<TFixture>()` instead of
  the per-class factory. `CleanUpOnce` must **not** dispose the fixture — the Jaribu session hook
  (MTP `CloseTestSessionAsync`, or the standalone/session-of-one wrap) disposes it exactly once
  when the outermost session ends; null the local static reference only.
- **Anti-pattern warning:** never reach for a process-static `Lazy<T>` or a bare static field to
  "share" a host across classes — that reintroduces the undisposed/refcounted bug class this
  design exists to avoid. If a fixture needs sharing, it goes through `RegisterSessionFixture` /
  `SessionFixture.GetAsync`, never ad hoc statics.
- **Lazy + skip-aware:** a class with no `SetupOnce` (e.g. an all-`[Skip]` class) never calls
  `GetAsync` and therefore never triggers the shared fixture's boot — preserve this when adding
  or un-quarantining tests in a session-shared suite (see the SPA suite's quarantined
  weather-forecast class).
- **Suite-shaped vs co-located coverage:** this composes cleanly for suite-shaped `tests/`
  projects (the whole assembly's module initializers always run together, so registration is
  never partial). For co-located single-file runfiles that might want session sharing while
  still working under bare `dotnet run <file>.cs`, the registering file and the consuming file(s)
  would need to be the same file (or the type simply isn't shared when run alone) — no co-located
  exemplar exists yet; treat this as an open follow-up if a co-located suite ever needs it.

```csharp
#!/usr/bin/env -S dotnet --
#:project <path-to-the-layer-project-this-test-needs, e.g. $(SourceDirectory)container-apps/web/projects/web-contracts/web-contracts.csproj>
#:package TimeWarp.Jaribu
#:package Shouldly
#:property PublishAot=false
#:property NoWarn=$(NoWarn);CA1707;CA1849;IDE0161;IDE0021;IDE0058

#region Purpose
// One honest line: what this runfile proves.
#endregion

//-:cnd:noEmit
#if !JARIBU_MULTI
return await TimeWarp.Jaribu.TestRunner.RunAllTests();
#endif
//+:cnd:noEmit

namespace Your.Slice.Namespace
{
  // Jaribu test classes — see the two exemplars above.
}
```

- `#:property PublishAot=false` — .NET 10 file-based apps default `PublishAot=true`, which bakes
  in reflection-disabling runtime feature switches and breaks
  `ContractSerializationDefaults`-style reflection-based JSON.
- `#:property NoWarn=$(NoWarn);…` — **the `$(NoWarn);` prefix is required.** A bare
  `NoWarn=CA1707;…` literal *replaces* the property rather than appending to it, silently
  un-suppressing everything `Directory.Build.props` already accumulated (CA1052/CA1515/RCS1102
  are already ambient from `source/container-apps/Directory.Build.props` and don't need
  re-listing here; CA1707/CA1849/IDE0161/IDE0021/IDE0058 are the ones this runfile shape needs on
  top of that).
- The `#if !JARIBU_MULTI` / `return` / `#endif` block MUST stay wrapped in the `//-:cnd:noEmit` /
  `//+:cnd:noEmit` escape (TWA0008) — without it, `dotnet new`'s conditional processor strips the
  `#if`/`#endif` directive lines from the generated app's copy while keeping the `return`
  unconditional (task 134 finding M1), breaking the family `JARIBU_MULTI` aggregator build.
  `dev template-smoke` tier 1 regression-tests this for the two exemplars; tier 3 runs the
  generated aggregators via MTP.
- New runfiles that introduce additional `#:project` dependencies must extend the matching
  family aggregator's `ProjectReference` list (`web-jaribu-tests` / `api-jaribu-tests`).
- When co-located **test method totals** change for an exemplar family (or a new family gains
  runfiles), also bump `TemplateSmokeHarness.JaribuFamilyAggregators` expected counts in
  `tools/dev-cli/services/template-smoke-harness.cs` (tier 3 hardcodes succeeded counts —
  web 5 / api 2 today). A green `dev test` alone is not enough if smoke still expects the old
  total.
- `#region Purpose` is never suppressed (TWA0004) — write the real one-line reason, not a
  placeholder.

Adding or changing a function or layer means editing only the JSON — the change applies to every
family:

1. Add the entry (e.g. a new `"validator": "application"` pair, or a new `unroutedLayers` entry).
2. Build the analyzers project (or a full solution build) so both generated files regenerate.
3. **Do a full rebuild, not an incremental one.** Analyzer DLLs can go stale under incremental
   MSBuild — a registry change that doesn't get picked up will silently keep enforcing the old
   pairing. Treat every registry edit as `dev build --clean`-worthy.
4. A layer-suffix that would nest inside another registered suffix (dual-glob-match risk) is
   rejected at generation time — the generator fails the build rather than shipping an ambiguous
   registry. The nesting check covers `layers` **and** `unroutedLayers` together.

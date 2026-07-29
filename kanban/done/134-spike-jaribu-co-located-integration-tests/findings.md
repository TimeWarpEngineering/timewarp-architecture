# Task 134 — Spike findings: Jaribu co-located integration tests

**Date:** 2026-07-29
**Spike branch:** `spike/134-jaribu-co-located-integration-tests` (off dev; evidence artifact —
not intended to merge as-is)
**Verdict up front:** the spike SUCCEEDS. Co-located Jaribu runfiles work for both the
contracts case and the primary real-host integration case, the existing test infrastructure
carries over nearly unchanged, and `dotnet test`/IDE discovery works. Two confirmed adoption
blockers (template safety, `dev test` MTP invocation) and three strategic decisions stand
between this proof and repo-wide adoption — all enumerated below with evidence.

## 1. What was proven (all independently re-verified in review round 1)

| Proof | Artifact | Result |
|-------|----------|--------|
| Contracts round-trip, host-free, co-located | `source/container-apps/web/features/admin/roles/create-role/create-role-tests.cs` | 5/5 standalone via `dotnet run`; JSON round-trip via `ContractSerializationDefaults`; Validator Name rejection |
| Integration test, real host (PRIMARY) | `source/container-apps/api/features/weather-forecast/get-weather-forecasts/get-weather-forecasts-tests.cs` | 2/2 standalone; real FastEndpoints + mediator pipeline over HTTP on :7255 (200 + 400 observed); port confirmed free before / released after |
| Membership-guard carve-out (spike form) | `Exclude="**/*-tests.cs"` in web+api `feature-membership.targets` | `dev build` (solution) 0 warnings / 0 errors with both `-tests.cs` files present |
| `dotnet test` / IDE discovery | `tests/container-apps/jaribu-spike-tests/` (JARIBU_MULTI aggregator, `TimeWarp.Jaribu.TestingPlatform`) | 7/7 discovered and passed in 605ms (bare `dotnet test` from project dir) |

"Don't mock your friends" is satisfied structurally: the integration runfile references only
`api-contracts` + `timewarp-testing` via `#:project`, spins the real Program pipeline, and
mocks nothing.

## 2. Step-0 evidence: file-based apps vs the repo build chain

- **Directory.Build.props/targets DO apply** to `dotnet run <file>.cs`: the SDK synthesizes a
  virtual csproj in the entry file's directory and MSBuild's upward walk imports the full repo
  chain (confirmed via `dotnet build -v:diag`: root props, `TreatWarningsAsErrors=true`,
  `GenerateAssemblyMarker` firing). This is why the task-046 precedent
  (`tests/foundation/foundation-domain-jaribu-tests/`) carries its own local props suppressing
  TWA0004/CA1707 — the rules genuinely apply.
- **Analyzers fire on co-located runfiles** with no ambient NoWarn relief under `source/` (the
  relief exists only under `tests/`): TWA0004, CA1707, CA1052, CA1515, CA1849, RCS1102,
  IDE0161, IDE0021, IDE0058 all observed. Spike handled via file-scoped
  `#pragma warning disable/restore` (folder-wide NoWarn is not an option — production files
  share the slice folders). Note: the IDE0161 disable must precede the `namespace` line.
- **Membership guard**: a `-tests.cs` file under `features/` without the carve-out is a hard
  `dev build` error (`ValidateFeatureFileMembership`, exact message reproduced) — confirmed,
  then carved out.
- **Surprise — .NET 10 file-based apps default `PublishAot=true`**, baking runtime feature
  switches (`JsonSerializer.IsReflectionEnabledByDefault=false`, …) into every runfile.
  This broke `ContractSerializationDefaults.Options` (reflection-based) with
  `InvalidOperationException: Reflection-based serialization has been disabled`. Per-file fix:
  `#:property PublishAot=false`. Adoption implication: this belongs in whatever shared
  convention/template the co-located runfile pattern standardizes on, or runfile authors will
  hit it one by one.

## 3. Requirement-2 verdict: what carries over from timewarp-testing

- **Carries unchanged:** `WebApplicationHost<TProgram>` (real `WebApplication.CreateBuilder` +
  `RunAsync`, fixed URLs, `IAspNetProgram` wiring, `configureServicesDelegate` override hook),
  `TestServerApplication<TProgram>` helpers (`GetResponse<T>`, `ConfirmEndpointValidationError<T>`),
  `ApiTestServerApplication` (public parameterless ctor — `new` it directly).
- **Replaced:** only Fixie's DI-convention role (constructor-injected singletons) → manual
  instantiation in the runfile.
- **Jaribu gap:** no class/assembly-scoped fixture lifetime — worked around with a
  `Lazy<ApiTestServerApplication>` static shared across the file's tests, never disposed
  (acceptable for a short-lived `dotnet run` process; wrong pattern for anything longer-lived).
  Filed upstream: https://github.com/TimeWarpEngineering/timewarp-jaribu/issues/19
- Cost note: ~1.5s first-request host spin-up per standalone runfile run.

## 4. Confirmed adoption blockers (review round 1, both empirically verified)

1. **Template safety (M1, bug):** `#if !JARIBU_MULTI` in template content is NOT survivable —
   dotnet-new's conditional processor treats the unrecognized symbol as unset, strips the
   directive lines, and keeps the guarded top-level `return` unconditionally; a generated
   app's aggregator build fails with CS8802. (Adjacent to the TWA0008/TWA0010 problem family:
   non-template `#if` symbols in template content are hazardous.) Fix candidates for the
   adoption task: `cnd:noEmit`-style escape around the pair, a template-recognized symbol, or
   excluding `-tests.cs` from conditional processing in template.json; extend
   `dev template-smoke` as the regression gate.
2. **`dev test` MTP invocation (M2, bug):** `dev test` globs `tests/**/*.csproj` (independent
   of `.slnx`) and invokes `dotnet test <csproj-path> -c Release`, which fails for
   Microsoft.Testing.Platform projects on .NET 10 ("Testing with VSTest target is no longer
   supported"). Bare `dotnet test` from the project directory works. Any committed Jaribu
   aggregator breaks `dev test` until its invocation gains MTP support.
3. Additional MTP friction (docs-level): the aggregator needed BOTH
   `TestingPlatformDotnetTestSupport=true` AND a `global.json` `test.runner` opt-in; a local
   `global.json` must mirror the root sdk pin or the SDK silently switches. Filed upstream:
   https://github.com/TimeWarpEngineering/timewarp-jaribu/issues/20

## 5. Carve-out: spike form vs permanent mechanism

Spike form: `Exclude="$(…TreeRoot)/**/*-tests.cs"` on the `FeatureTreeFile` glob in web+api
`feature-membership.targets`. Proven to keep `dev build` 0/0, but it has a validation blind
spot (M3): ANY `-tests.cs` file — orphaned, misnamed, not a valid runfile — now silently
passes the guard. The alternative, registering `tests` in `feature-filename-grammar.json` as a
recognized-but-unrouted suffix, keeps such files matched-and-validated (and greppable in the
grammar SSOT) while still compiling into no layer project. The blind spot is the concrete
evidence for that strategic choice (see §8 Q1).

## 6. Aspire testing (requirement 3 — full survey in `aspire-testing-survey.md`)

**Complements, does not supersede.** Decisive: Aspire testing runs every resource as a
separate process and its docs explicitly exclude DI mocking/substitution — the
`configureServicesDelegate` externality-override hook has no Aspire equivalent, and "mock only
externalities" depends on it. Keep the hand-rolled host for single-service endpoint tests;
consider `Aspire.Hosting.Testing` (already pinned, 13.4.6, unused) only for a new
multi-resource/postgres/ingress-topology tier — first candidate to evaluate:
`aspire-tests/ingress-smoke-tests.cs`. Fixed ports need no migration either way.

## 7. Proposed test-tier map

| Tier | Framework/host | Lives | Notes |
|------|----------------|-------|-------|
| Co-located contract + slice integration tests | Jaribu runfiles; timewarp-testing host classes | inside `features/<slice>/` as `<name>-tests.cs` | The new default; per-file `#:project` isolation; slice deletion deletes its tests |
| Host-level / cross-service integration | Fixie today (migrate opportunistically or never) | `tests/` | Fixed ports, serialized; `WebApplicationHost` stays |
| AppHost topology (optional future) | `Aspire.Hosting.Testing` | `tests/` | Only if/when a multi-resource tier is wanted (§8 Q3) |
| E2E | Playwright | unchanged | unchanged |

## 8. Strategic decisions for Steve (in dependency order)

1. **Carve-out mechanism:** exclude-glob (simple, but validation blind spot — M3) vs
   registered-unrouted `tests` suffix in the grammar SSOT (files stay validated; grammar
   remains the single vocabulary; registry edit ⇒ full-rebuild caveat). Evidence favors the
   grammar registration; decide before the adoption task starts.
2. **`dev test` discovery shape:** teach `dev test` MTP invocation + extend discovery to
   co-located runfiles (making co-location first-class in tooling) vs aggregator projects
   under `tests/` as the only `dev test` entry (runfiles then are dev-loop-only artifacts).
   Blocker M2 must be fixed under either choice.
3. **Aspire tier:** adopt `Aspire.Hosting.Testing` for a new multi-resource tier now (evaluate
   `ingress-smoke-tests` as first candidate) or defer until postgres-backed scenarios demand it.

## 9. Follow-up task list (create after §8 decisions)

1. **Adopt co-located Jaribu convention** (the big one): permanent carve-out per Q1; template
   safety fix for the JARIBU_MULTI switch (M1) + `dev template-smoke` regression coverage;
   shared runfile preamble convention (`PublishAot=false`, pragma set or an ambient NoWarn
   story for `-tests.cs` under `source/`, Purpose-region placement); grammar/skill/AGENTS.md
   documentation; TWA0015/0016 awareness of the `tests` suffix if Q1 chooses registration.
2. **`dev test` MTP support** (M2): fix invocation form; per Q2, optionally extend discovery
   to co-located runfiles/aggregators; keep serialized execution for fixed-port tests.
3. **Jaribu upstream follow-through:** class-scoped lifetime (#19) — adopt in the integration
   runfile pattern when shipped, replacing the undisposed-Lazy workaround; README docs (#20).
4. **Migration policy task:** new tests are co-located Jaribu from adoption day; existing
   Fixie projects migrate opportunistically slice-by-slice; `tests/` host-level integration
   suites migrate last or never.
5. **(Conditional on Q3)** Aspire testing tier task starting from `ingress-smoke-tests`.

## 10. Upstream issues filed

- https://github.com/TimeWarpEngineering/timewarp-jaribu/issues/19 — class/assembly-scoped
  fixture lifetime hooks
- https://github.com/TimeWarpEngineering/timewarp-jaribu/issues/20 — .NET 10 `dotnet test`
  MTP opt-ins + sdk-pin + invocation-form docs

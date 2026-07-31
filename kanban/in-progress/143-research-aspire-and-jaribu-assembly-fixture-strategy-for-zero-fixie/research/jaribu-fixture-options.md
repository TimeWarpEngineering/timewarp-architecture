# Stream 3 — Jaribu fixture-level comparison (requirement 3)

Research agent output, 2026-07-31. Read-only; grounded in timewarp-jaribu/dev source
(test-runner.cs, jaribu-test-framework.cs, mtp-sink.cs, task 029) and the TWA consumer tree.

## Key mechanical fact

Two dispatch paths; aggregators use the harder one:
- `TestRunner.RunAllTests()` (test-runner.cs:728-805) — standalone `dotnet run`; new
  TerminalSink per class.
- `JaribuTestFramework.ExecuteRequestAsync` (jaribu-test-framework.cs:42-84) — `dotnet test`
  MTP (what `dev test` invokes); one shared MtpSink across classes but nothing hooks
  before/after the loop.
SetupOnce/CleanUpOnce resolve/invoke strictly per-class inside `RunTestsAsyncCore`
(test-runner.cs:521-563). NO multi-class hook exists in either path today.
`MtpSink.OnRunStarted/CompletedAsync` are no-ops. Discovery = per-class
`[ModuleInitializer]` self-registration; class order is CLR module-initializer order
(implementation-defined, not author-controlled).

## Options

**A — per-file SetupOnce (status quo):** ships now; strictly per-class both paths; cannot
share a multi-host graph across classes (N× startup under JARIBU_MULTI); sequential-class
behavior is current-behavior, not guaranteed. Upstream cost: none.

**B — aggregator/run-scoped hooks:** needs new API around the RegisteredTestClasses loop in
BOTH paths; run-scoped under MTP; degrades to class-scope standalone. Gets "once per process"
but NOT ordering. Hidden-coupling failure mode (class silently depends on aggregator wiring).
Current family aggregators are per-family processes — a Web+Api+Yarp graph needs a new
combined aggregator csproj. CRITICAL: the MTP seam lives in timewarp-jaribu-testing-platform,
so B is unreachable under `dev test` without changing Jaribu — B collapses into D.

**C — explicit shared fixture module (timewarp-testing, plain static async-lazy factory):**
the ONLY option that naturally expresses ordering (`StartWeb → StartApi → StartYarp(web,api)`
is just code). Zero Jaribu changes. But solves composition, not teardown — alone it
reintroduces the undisposed-static class of bugs; needs pairing with A (per-class CleanUpOnce
+ refcounting risk) or B/D/E run-scope teardown.

**D — assembly SetupOnce/CleanUpOnce upstream:** task 029 explicitly deferred this
("Assembly-scoped hooks: new task when needed"); no API shape designed. Mechanically
converges with B (same seam, same no-Type-to-hang-a-hook-off problem); difference is
ownership — first-class, versioned, fail-fast-validated Jaribu API vs TWA hand-roll (which
isn't possible anyway per B). 029's rejected-alternatives list still applies (no
IClassFixture DI, no magic field scanning).

**E — MTP session hooks (NEW, source-suggested):** `JaribuTestFramework` already implements
`CreateTestSessionAsync`/`CloseTestSessionAsync` (jaribu-test-framework.cs:32-40) as unused
no-ops — a genuine per-MTP-session start/end seam. Wiring user-supplied setup/teardown here
is a narrower, more idiomatic diff than loop-wrapping ExecuteRequestAsync, though it must
guard against firing on discovery-only sessions. Doesn't fire under bare `dotnet run`.

## Facts table

| | A | B | C | D | E |
|---|---|---|---|---|---|
| Ships today | Yes | No | No (new TWA file only) | No | No |
| New Jaribu API | None | Yes ×2 places | None | Yes (designed) | Reuses empty methods |
| Spans classes in one process | No | Yes | N/A (module) | Yes | Yes |
| Expresses dependency ordering | No | No | **Yes** | No | No |
| Deterministic teardown | Per-class | If built | Not alone | If built | If built |
| Standalone `dotnet run` story | Native | Degrades | Works via class SetupOnce calling it | Same as B | MTP-only |
| Reachable under `dev test` w/o Jaribu change | Yes | **No** | Yes | N/A | N/A |

## OPEN judgment points (for synthesis/decision)

1. B ≡ D under `dev test` — is B even a distinct option?
2. No hook option expresses ordering; only C does. Any design likely = C (composition) + one
   of {A, D, E} (disposal scope). Two-part model: acceptable or a smell?
3. Standalone-single-file-first pulls toward C+A regardless: classes must work under bare
   `dotnet run`, so they must call C's idempotent factory from their own SetupOnce; run-scope
   hooks become an optimization, never the authoring primitive.
4. Today's real multi-host answer is Fixie assembly-DI constructor-graph resolution
   (YarpTestServerApplication(web, api) — yarp-test-server-application.cs:15-25). No Jaribu
   option has a "resolve my dependencies" primitive; C makes ordering author-explicit instead
   of container-derived — judgment call.
5. Fixed-port serialization stays regardless (web 7000 / api 7255 / yarp 8443; 7255 shared by
   web+api suites today per test-command.cs comment). A combined aggregator moves the
   discipline into one process; doesn't remove it.

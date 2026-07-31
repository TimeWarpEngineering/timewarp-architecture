# Stream 1 — Lifetime/composition inventory (requirement 1)

Research agent output, 2026-07-31. Read-only; grounded in DECOMPILED TimeWarp.Fixie 3.1.0 +
TimeWarp.Jaribu[.TestingPlatform] beta.14 and every call site in tests/.

## Headline correction to the task's framing

**"Fixie assembly DI singletons" is a misnomer.** `TestExecution.Run` creates a NEW
ServiceCollection/Provider PER TEST CLASS (ConfigureServices callback re-invoked each time;
`classServiceProvider.DisposeAsync()` between classes). `.AddSingleton<WebTestServerApplication>()`
= singleton-per-class-provider. Consequences:
- web-server-integration-tests boots/tears down Web.Server (:7000, 30s wait budgets) ~14× per
  run (one per consuming class).
- The Aspire-backed "shared" singletons (api-server-integration-tests, web-spa-integration-tests
  conventions) are DECLARED assembly-shaped but REBUILT per consuming class — the full Aspire
  app graph (postgres+web+api+yarp) boots once per class (2× in api suite today). The intended
  sharing never actually happens.
- Classes run strictly sequentially; per-method scopes inside a class; lazily-built singletons
  mean non-consuming classes pay nothing.

## DI graph + ordering (testing-convention.cs:16-46)

- `IWebApiTestService` factory force-resolves ApiTestServerApplication BEFORE returning
  WebTestServerApplication → Api (:7255) boots before Web (:7000) whenever both flags on.
- `YarpTestServerApplication(web, api)` ctor-injects both → DI graph forces order, no author code.

## Consumers table (condensed; full detail in session transcript)

| Project | Framework | Hosts | Overrides |
|---|---|---|---|
| web-server-integration-tests | Fixie/WebServerTestConvention | Web in-proc :7000 ×~14 classes; Api :7255 DI-forced for BFF | MockAccessTokenProvider; PostConfigure HttpClientFactoryOptions (→:7000/:7255) |
| api-server-integration-tests | Fixie/ApiServerTestConvention | MIXED per class: 2 classes in-proc Api :7255; 2 classes FULL Aspire DistributedApplication (OpenAPI class documents why: in-proc pollutes FastEndpoints discovery across AppDomain) | Aspire singleton factory; TestApiService |
| web-spa-integration-tests | Fixie/SpaTestConvention | Full Aspire app per class; AspireSpaTestApplication wraps ingress HttpClient + builds its OWN non-Aspire ServiceCollection for SPA (TimeWarp.State, fake IJSRuntime); BaseTest = ctor-injected ISpaTestApplication + per-class-instance scope | fake IJSRuntime, remove FluentUI toast handler, fake IAccessTokenProvider. NOTE: dead competing path `SpaTestApplication<,>` registered but unused |
| aspire-tests | **xunit** (3rd framework!) | IClassFixture/IAsyncLifetime; one Aspire app per class; health-gates web→api→ingress then reachability-polls | none (real everything) |
| web-contracts/domain/infrastructure, foundation-*, identity, analyzers | Fixie bare | none (infra: Testcontainers postgres via process-static Lazy, Docker-skippable) | n/a |
| api/web-jaribu-tests | Jaribu MTP | api: exactly 1 host spin per run (one SetupOnce class exists); web: 0 | n/a |

## Fixed ports

:7000 Web (web-test-server-application.cs:13) · :7255 Api (api-test-server-application.cs:9;
also Web's BFF base + ApiServiceUriHelper fallback) · :8443 Yarp (yarp-test-server-application.cs:33)
· Aspire-backed suites: dynamic ports (Aspire allocates). Identity ceremony tests reference
7000/7255 as origin STRINGS only.

## Jaribu modes (beta.14, decompiled)

- SetupOnce/CleanUpOnce: exactly one public static hook pair per class (multiple = hard error);
  Setup lazy on first non-skipped test; CleanUp in finally; state scoped per RunTestsAsyncCore
  call → genuinely class-scoped.
- BOTH dispatch paths (RunAllTests standalone; ExecuteRequestAsync MTP) = same sequential
  foreach over process-wide RegisteredTestClasses; no parallelism; no run-scope hooks.
- Aggregators glob-compile the same source files (no copies); api aggregator ProjectReferences
  timewarp-testing.

## Gap statement — what Fixie gives that Jaribu beta.14 cannot express (facts)

1. Declarative ctor injection of a shared host into a test class (Jaribu: static field + explicit reference).
2. Automatic transitive resolution/ordering from the DI graph (Jaribu: manual construct/order/dispose in hooks).
3. One assembly-declared override point (ConfigureAdditionalServicesCallback) auto-applied to
   every class (Jaribu: none — inline per runfile, or a not-yet-existing shared mechanism).
4. Structural disposal via container lifetime (Jaribu: hand-written CleanUpOnce; omission = leaked port).
5. Scrutor assembly-scan discovery (Jaribu: per-class [ModuleInitializer] boilerplate).
6. **Anti-gap:** Fixie does NOT actually deliver assembly-lifetime sharing (see headline) — the
   intended Aspire-app reuse across classes doesn't happen today. Jaribu class-scope is
   lifetime-EQUIVALENT to Fixie's real behavior.

Key refs: testing-convention.cs:16-46; test-server-application.cs:19-39;
web-application-host.cs:44-103; applications/*.cs; api-server-test-convention.cs:20-55;
web-spa-integration-tests/infrastructure/{spa-test-convention,aspire-spa-test-application,base-test}.cs;
aspire-tests/ingress-smoke-tests.cs:13-92; get-weather-forecasts-tests.cs:60-104.

# Stream 2 — Aspire fit (requirement 2)

Research agent output, 2026-07-31. Measurements live this session; docs re-pulled at 13.4.6.

## aspire-tests current state (drift alert)

- **xUnit**, not Fixie/Jaribu — a THIRD test framework in the repo, contradicting AGENTS.md's
  "host-level suites stay Fixie+Shouldly".
- integration-test1.cs: template boilerplate, per-test AppHost boot, 26s for ONE test, whole
  5-resource graph started to touch api-server only.
- ingress-smoke-tests.cs: IClassFixture/IAsyncLifetime shared app; health-gates then
  reachability-polls the ingress (DCP proxy race documented); real auth/host-header/cert
  assertions; zero mocks. 7 passed, 42.99s test / 1:46 wall.

## Per-category fit (measured)

- **api-server-integration-tests**: ALREADY hybrid — in-proc ApiTestServerApplication for
  handler tests AND full Aspire app for endpoint + OpenAPI tests (OpenAPI class NEEDS process
  isolation: in-proc web+api in one AppDomain pollutes FastEndpoints discovery — a bug class
  only real process boundaries fix). Cost: pays BOTH boot paths in one assembly (32.69s test /
  52.65s wall for 8 tests).
- **web-server-integration-tests**: zero Aspire; hand-rolled Web+Api fixed ports; depends on
  in-proc `MockAccessTokenProvider` DI substitution.
- **web-spa-integration-tests**: full Aspire app per class + SPA's OWN ServiceCollection built
  in test code (SPA is NOT an Aspire resource — its fakes never cross the process wall, so
  Aspire's DI limitation doesn't apply to the SPA layer). Dead competing path
  `SpaTestApplication<,>` still registered, unused via DI (one file uses it directly).
  Heaviest suite: 11 passed/3 skipped, 77.57s test / 2:13 wall.
- **aspire-tests**: pure closed-box by design.

## DI-substitution boundary (re-confirmed verbatim from 13.4.6 docs)

"Aspire testing doesn't enable scenarios for mocking, substituting, or replacing services in
dependency injection—as the tests run in a separate process." All advanced levers
(WithEnvironment, AppHost args, WithExplicitStart, WaitAnnotation removal) are AppHost/config
level. UNCHANGED from 134 survey — decisive, still true.

Mock-by-mock check:
- BaseAddress wiring → config-only already; Aspire service discovery replaces it cleanly.
- `MockAccessTokenProvider` → **compile-time `#if MOCK_AUTHENTICATION`** (web-spa
  program.cs:56-58): env/config cannot flip a baked preprocessor branch. CONFIRMED blocker to
  wholesale Aspire migration of web-server-integration-tests; unlock requires product-code
  change (runtime-config-gated registration).
- SPA fakes (IJSRuntime etc.) → unblocked; SPA never hosted as Aspire resource.

## Facts for synthesis

- Aspire is load-bearing in 3 of 4 categories today (all but web BFF tier).
- Boot cost dominates: 20–30s per fresh full-graph AppHost fixture; no partial-graph
  (WithExplicitStart) usage in-repo yet; hand-rolled WebApplicationHost amortizes near-zero
  across 97 tests (28s whole suite).
- OPEN: SPA "Aspire-but-DI-unconstrained" pattern — template or accident?
- OPEN: is 20–30s/fixture acceptable as closed-box tax, or does it demand session/shared
  AppHost lifetime (ties into stream 1/3 lifetime question)?
- OPEN: does process-isolation-fixes-discovery-pollution generalize into a stated REASON for
  the end-state architecture?

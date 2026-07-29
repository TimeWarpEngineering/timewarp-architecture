# Aspire testing survey (task 134, requirement 3)

Research agent output, 2026-07-29. Repo context: Aspire 13.4 (AppHost SDK 13.4.3);
Aspire.Hosting.Testing 13.4.6 already pinned in Directory.Packages.props but unused.

## Verdict

Aspire's `Aspire.Hosting.Testing` (DistributedApplicationTestingBuilder) **complements, not
supersedes**, the hand-rolled `WebApplicationHost<TProgram>` host. Keep the hand-rolled host for
single-endpoint/mediator-pipeline tests with externality mocking; adopt Aspire testing only for
a new, separate class of multi-resource/postgres/e2e-adjacent scenarios if/when wanted. No
migration of the existing suite is warranted.

## What current Aspire testing offers

- `DistributedApplicationTestingBuilder.CreateAsync<TAppHost>()` launches the AppHost project
  itself, in a background thread, as a real distributed run — every resource starts as its own
  separate process/container, not in-proc. Docs position it as "closed-box integration testing
  of your entire distributed application," explicitly contrasted with unit/open-box tests.
  (https://aspire.dev/testing/overview/)
- `BuildAsync()`/`StartAsync()` lifecycle; `app.CreateHttpClient("resourceName")` resolves
  BaseAddress via service discovery; `app.ResourceNotifications.WaitForResourceHealthyAsync`
  for readiness. (https://aspire.dev/testing/write-your-first-test/)
- Advanced: `WithExplicitStart` to disable resources, conditional resources via AppHost config
  args, removing WaitAnnotations, env-var overrides via resource builders, and a
  `DistributedApplicationFactory` base class with OnBuilderCreating/Created/OnBuilding/OnBuilt
  hooks — all AppHost-level, not app-DI-level.
  (https://aspire.dev/testing/advanced-scenarios/)
- CI: container runtime required for container resources; explicit timeouts advised; random
  port assignment is default and the recommended CI posture (`DcpPublisher:RandomizePorts=false`
  exists only for narrow local cases).
  (https://aspire.dev/testing/testing-in-cicd-pipelines/)
- 13.4/13.5 changes were reliability fixes (dashboard/dynamic-ports fix; log flush before
  terminal resource state), no new capability changing the closed-box model.

## Why it cannot supersede (repo-specific)

1. **No in-process DI overrides — decisive.** Docs: "Aspire testing doesn't enable scenarios
   for mocking, substituting, or replacing services in dependency injection—as the tests run in
   a separate process." The repo's `configureServicesDelegate` pattern (e.g.
   `AddSingleton<IAccessTokenProvider, MockAccessTokenProvider>()` into the real
   IServiceCollection before Build()) has no Aspire equivalent; env/config levers cannot
   substitute a C# type in DI. "Don't mock your friends, only mock externalities" depends on
   exactly this hook.
2. **Granularity/cost.** Boots AppHost + at least one project-resource process (plus
   containers) per fixture — right for suite-level closed-box fixtures, wrong for tight
   per-endpoint mediator-pipeline loops against a warm in-proc host.
3. **Fixed ports.** Hand-rolled host uses deterministic 7000/7255 for cross-service
   BaseAddress wiring; Aspire uses random/proxied ports resolved via CreateHttpClient. Two
   different resolution models that can coexist; neither obsoletes the other. Retiring fixed
   ports is NOT a prerequisite for adopting Aspire testing.
4. **Jaribu/MTP composition: no blocker.** DistributedApplicationTestingBuilder is a plain
   async C# API — callable from a Jaribu runfile the same as from xUnit; framework templates
   are conveniences, not coupling.

## Recommendation

- Keep `WebApplicationHost<TProgram>` / `TestServerApplication<TProgram>` for single-service
  endpoint tests and anything needing DI-level externality mocks.
- Introduce Aspire.Hosting.Testing only for a genuinely new tier: multi-resource /
  postgres-backed / cross-service (web-api-grpc via YARP ingress) tests validating the actual
  AppHost topology. First candidate to evaluate: `aspire-tests/ingress-smoke-tests.cs`, which
  already tests route composition by other means.
- Package pin already in place; no version work needed to start.

## Open questions (human)

1. Does the repo want a multi-resource/e2e-adjacent test tier now, or does
   ingress-smoke-tests' current approach suffice (avoid second pattern's maintenance surface)?
2. If adopted, placement: new `tests/aspire-integration-tests` sibling vs folded into existing
   `aspire-tests` project (axis-1 placement decision).
3. CI container-runtime (Docker for postgres-backed Aspire tests): acceptable in the existing
   serialized `dev test` model, or its own CI lane?

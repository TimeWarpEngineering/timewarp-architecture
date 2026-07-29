# Task 134 — Spike implementation plan (Phase 2 output)

Produced by plan agent, 2026-07-29. Investigated actual code, not guessed.

## Key facts established

- **Jaribu already pinned**: `Directory.Packages.props:170` — `TimeWarp.Jaribu` `1.0.0-beta.13`. No bump needed.
- **House runfile precedent exists**: `tests/foundation/foundation-domain-jaribu-tests/enumeration.cs`
  (task 046) — Jaribu duplicate of an existing Fixie suite. Shape: `#!/usr/bin/env -S dotnet --`,
  `#:project $(SourceDirectory)...`, `#:package TimeWarp.Jaribu`, `#:package Shouldly`,
  `#if !JARIBU_MULTI` inline `return await TestRunner.RunAllTests();`. Replicate this pattern:
  duplicate existing proven Fixie tests, don't invent new tests.
- **Existing test infra carries over** (`tests/common/timewarp-testing/`) — plain C#, not
  Fixie-coupled at class level:
  - `WebApplicationHost<TProgram>` wraps `WebApplication.CreateBuilder`, fixed URLs via `UseUrls`,
    drives `IAspNetProgram` members, then applies `Action<IServiceCollection>?
    configureServicesDelegate` — THE service-override hook (mock externalities only).
  - `TestServerApplication<TProgram>` exposes `HttpClient`, `Send<TResponse>()`,
    `GetResponse<TResponse>()`, `ConfirmEndpointValidationError<TResponse>()`, `IAsyncDisposable`.
  - Fixed ports hardcoded in subclasses: `WebTestServerApplication` → :7000,
    `ApiTestServerApplication` → :7255.
  - Only the Fixie-DI-convention layer (`TimeWarpTestingConvention` singleton registration) is
    Fixie-specific. Jaribu tests `new` the host manually. Verify ctor accessibility.
  - Watch: Fixie gives class-scoped singleton lifetime; Jaribu `Setup`/`CleanUp` are per-test.
    Try lazy static shared host first; file upstream only if structurally impossible.

## Spike targets (corrects task.md's stale example)

- **Req 1 (contracts round-trip)**: task.md's "`features/counter/`" example is WRONG — counter is
  client-only SPA state (web-spa tree), no wire contract, and web-spa isn't governed by
  `feature-membership.targets`. Use
  `source/container-apps/web/features/admin/roles/create-role/create-role-tests.cs`:
  round-trip `CreateRole.Command`/`Response`, exercise `Validator` Name rejection,
  `#:project` → web-contracts.
- **Req 2 (integration, primary)**: api family —
  `source/container-apps/api/features/weather-forecast/get-weather-forecasts/get-weather-forecasts-tests.cs`.
  `[EndpointAllowAnonymous]` (no auth plumbing diluting the spike), real validator rule
  (`Days >= 1`), and an existing Fixie suite to duplicate:
  `tests/container-apps/api/api-server-integration-tests/.../get-weather-forecasts-endpoint-tests.cs`
  (happy path `_10WeatherForecasts_Given_10DaysRequested` + `ValidationError`).

## CRITICAL RISK (confirmed)

- Root `Directory.Build.props` sets `TreatWarningsAsErrors` + attaches TW0001/TWA analyzers
  repo-wide; root targets add `GenerateAssemblyMarker`.
- `ValidateFeatureFileMembership` globs EVERY `.cs` under the feature/platform trees against
  the layer-suffix regex — a `-tests.cs` file in `features/` is a hard MSBuild error on
  `dev build` today. No silent-exclusion path.
- TWA0004 fires per syntax tree — runfile needs `#region Purpose` after shebang/`#:` lines.
- UNKNOWN empirically: whether standalone `dotnet run <file>.cs` walks up and applies
  Directory.Build.props/targets. `enumeration.cs` (no Purpose region, outside guard trees)
  suggests maybe not, but weak evidence. **Spike step 0: test this first.**

## Ordered steps

0. **De-risk first**: run `enumeration.cs` + a throwaway runfile dropped under
   `api/features/weather-forecast/` via `dotnet run`; observe whether props/analyzers fire.
   Determines scope of steps 3–5.
1. Spike branch off dev (naming per tw-git skill).
2. Req 1 runfile (create-role, standalone `./create-role-tests.cs`).
3. Req 5 alongside (not after): minimal real carve-out in `feature-membership.targets`
   (+ grammar JSON if chosen) so step 2's file coexists with `dev build` 0/0 — the "sketch," proven.
4. Req 2 runfile (weather-forecast, `#:project` api-contracts + timewarp-testing.csproj,
   manual `ApiTestServerApplication`, happy path + validation rejection on :7255).
5. Req 3: Aspire testing survey — repo is on Aspire 13.4 (`Aspire.AppHost.Sdk/13.4.3`);
   `Aspire.Hosting.Testing` already pinned at 13.4.6 but UNUSED. Answer supersede-vs-complement
   for `WebApplicationHost<TProgram>`; prior: complement (testing builder matters for
   multi-resource/Postgres scenarios) — confirm against current docs.
6. Req 4: `JARIBU_MULTI` aggregator project wrapping both runfiles; `dotnet test` discovery;
   file Jaribu MTP gaps upstream, never work around.
7. findings.md + task.md checklist + kanban commits.

`dev test` today globs `tests/**/*.csproj` serially (task 083) — co-located runfiles invisible
to it; follow-up list only, out of scope.

## Open questions

Strategic (Steve decides, post-spike via findings): carve-out mechanism (glob-exclude vs
registered-unrouted `tests` suffix — different long-term compile-visibility implications);
whether `dev test` should discover co-located tests directly vs via aggregator under `tests/`;
whether Aspire testing builder replaces `WebApplicationHost` once Postgres-backed tests exist.

Tactical (implementer): branch name; shared-host lifetime strategy; carve-out edit form
(targets Exclude vs grammar JSON) — either is fine as spike evidence.

# 058-001: Harden web-spa test host + upgrade TimeWarp.State (remove 058 workarounds)

## Parent

058-get-fixie-tests-running-at-root-and-add-dev-test-command

## Why

Task 058 got the suite green, but three web-spa items landed as **workarounds** that should be cleaned
up properly:

1. **TimeWarp.State case-sensitive test guard.** `State.Initialize()` guards with
   `if (!assembly.FullName.Contains("Test"))` (capital "Test", case-sensitive). The kebab-cased
   assembly name `web-spa-integration-tests` (lowercase "tests") fails it → `FieldAccessException`.
   **Workaround:** `<AssemblyName>web-spa-integration-Tests</AssemblyName>` in the csproj.
   **Proper fix:** upgrade TimeWarp.State past `12.0.0-beta.1` (e.g. `12.0.0-beta.3`, which fixes the
   guard) and remove the AssemblyName override. NOTE: beta.3 has breaking API changes
   (`ActionHandler.Handle` → `ValueTask<Unit>`, `INotification`/`StateTransactions` changes) — the
   web-spa state handlers must be migrated. Sizeable; treat like a package-migration task.

2. **FluentUI toast in headless tests.** `ToastNotificationState.ExceptionNotificationHandler` shows a
   FluentUI toast (`IToastService`), which needs a rendered `<FluentToastProvider>` not present in the
   integration tests → `FluentServiceProviderException`. `IToastService` can't be faked/stubbed
   (`IFluentServiceBase<T>` has internal interface members). **Workaround:** the SPA test host
   (`spa-test-application.cs`) removes the `INotificationHandler<ExceptionNotification>`. Revisit when
   FluentUI test support improves, or render a toast provider in the harness.
   **Proper fix (FluentUI v5 migration debt):** `FluentToast`/`FluentToastProvider`/`IToastService` are
   **removed in FluentUI v5** — replaced by **`FluentMessageBar`** (per the v5 migration guide,
   `/Migration/Toast`). Our `ExceptionNotificationHandler` still uses the removed `IToastService`. Migrate
   it to `FluentMessageBar`; that likely also resolves the headless-test problem (and the test-host
   handler-removal workaround can then go away). Belongs with the broader epic-059 FluentUI v5 cleanup.

3. **Quarantined test.** `WeatherForecastsState_.FetchWeatherForecasts_Action_Should
   .Update_WeatherForecastState_With_WeatherForecasts_From_Server` is `[Skip]`-ped — the SPA's weather
   fetch throws in the headless `SpaTestApplication` host (the toast surfaced it). The SPA→server fetch
   needs wiring in the test host (HttpClient/base address / auth) so the action actually returns 5
   forecasts. **Un-skip once the fetch works.**

## Rescope (2026-09-22)

Board audit against master. Item 1's package half is **done**: task 237 moved the repo to
`TimeWarp.State` / `.Plus` 12.0.0-beta.3 and `TimeWarp.Mediator` 14.0.0-beta.1 and migrated the
handlers. Section 4 (Aspire testing migration) is **moot**: epic 145 settled the two-lane model —
in-proc `HostGraphFactory` / `SessionHostFixture` C-create for DI-substitution suites, Aspire
closed-box only for topology — and `web-spa-integration-tests` already runs on it. Do not migrate
the SPA suite to Aspire.

What is still open, and this task's whole scope:

1. **Remove the `<AssemblyName>web-spa-integration-Tests</AssemblyName>` override** in
   `tests/container-apps/web/web-spa-integration-tests/*.csproj`. beta.3 fixed the case-sensitive
   test guard; verify by deleting the override and running the suite. If State still trips on the
   kebab name, that is a State bug — file it on timewarp-state, do not keep the override.
2. **Migrate `ExceptionNotificationHandler` (and the sibling `ProblemDetailsNotificationHandler`)
   off `IToastService` / `FluentToastProvider`** to the FluentUI v5 `FluentMessageBar` pattern the
   rest of web-spa uses (tw-blazor-css-strategy skill; look at how Section / Card surface
   errors today). Then delete the SPA test host's `INotificationHandler<ExceptionNotification>`
   removal workaround in `spa-test-application.cs`; the handler must run headless without a
   rendered provider. Handler rule applies: never `Send` an action from a handler (TWS0002);
   publish a notification or mutate state.
3. **Un-quarantine** `WeatherForecastsState_.FetchWeatherForecasts_Action_Should
   .Update_WeatherForecastState_With_WeatherForecasts_From_Server`: wire the SPA→server fetch in
   the test host (base address / auth via `MockAccessTokenProvider`) so the action returns the
   5 forecasts, remove the `[Skip]`.

Gates: `dev build` 0/0; `dev test` with the web-spa suite green and the skip count down by one;
`ganda repo audit` exit 0.

## Checklist

- [x] Upgrade TimeWarp.State (+ .Plus) past beta.1; migrate web-spa state handlers — done by 237
- [x] Remove the `<AssemblyName>` override on web-spa-integration-tests; suite still green
- [x] Exception / problem-details notification handlers on `FluentMessageBar`; test-host handler
      removal workaround deleted
- [x] SPA→server weather fetch wired in the test host; quarantined test un-skipped and passing
- [x] `dev build` 0/0, `dev test` green, `ganda repo audit` 0
- [x] Section 4 (Aspire migration) — closed as moot by epic 145; no work

## Notes

Historical: before this cleanup, `dev test` was green with the workarounds (72 passed / 6 skipped / 0 failed). That skip count is not comparable to the suite shape after epic 145.

Review trail: `review/review-framework.md`, `review/round-1/`, `review/round-2/merged.md`, `review/disposition.md`. Effort 1 (general). Disposition clean; 0 findings. Round 2 re-verified the same product diff.

## Results

Removed the three web-spa workarounds that task 058 left behind. TimeWarp.State / .Plus 12.0.0-beta.3 was already on the branch (task 237).

1. **Assembly name.** Deleted `<AssemblyName>web-spa-integration-Tests</AssemblyName>` and the matching `InternalsVisibleTo` entries (`web-spa.csproj`, `web-contracts.csproj`). The kebab assembly `web-spa-integration-tests` loads. `State.Initialize()` no longer calls the case-sensitive guard, but `ThrowIfNotTestAssembly` still does (`assembly.FullName.Contains("Test")`). Debug seeders (`CounterState`, `ApplicationState`, `AnalyticsState`, `WeatherForecastsState`) now call `TestCaller.Ensure`, which accepts `test` ordinal-ignore-case and still throws `FieldAccessException` for any other caller. Filed as [timewarp-state#607](https://github.com/TimeWarpEngineering/timewarp-state/issues/607). The AssemblyName override stays deleted.

2. **Message bars.** `ExceptionNotificationHandler` and `ProblemDetailsNotificationHandler` append to `ToastNotificationState` and re-render subscribers. They do not call `INotificationService` and do not `Send` (TWS0002). `AddNotification` / `DismissMessage` mutate the same list through the action pipeline. `MessageBars` paints `FluentMessageBar` rows at the top of `TimeWarpPage` and `TimeWarpFocusedPage` (`AllowDismiss=false`; dismiss dispatches `DismissMessage` so a re-render does not bring the row back). The SPA test host no longer mentions removing `INotificationHandler<ExceptionNotification>`. Headless proof: `CloneStateBehavior.Should.RollBackState_When_Exception` sees the error bar, and `AddNotification_Records_MessageBar_Without_Provider` records a success bar with no FluentUI provider in the tree.

3. **Weather fetch.** `Update_WeatherForecastState_With_WeatherForecasts_From_Server` is not skipped. `AspireSpaTestApplication` points the `api-server` `HttpClient` at the Aspire ingress HTTP endpoint and registers `MockAccessTokenProvider` through `MockAuthenticationRegistration` (Testing + `Authentication:UseMock`). The action returns 5 forecasts (`PassedTestNodeStateProperty`, about 372 ms inside the suite).

**Intermittent full-suite failure (follow-up, same branch).** `Update_WeatherForecastState_With_WeatherForecasts_From_Server` passed alone but failed about one run in four with the whole suite: `forecasts should not be null`, `Error:An error occurred while sending the request.`, in 142 ms. Root cause was NOT the SPA host, the session fixture, or a pinned ingress port (ingress host ports are dynamic under `Aspire.Hosting.Testing`; the suite boots exactly one `DistributedApplication`). `Aspire.Hosting.Yarp`'s `AddYarp` registers no health check, so `WaitForResourceHealthyAsync("ingress")` returned the moment DCP reported the YARP container **Running** — while the DCP host-side proxy accepted connections and closed them with an immediate EOF because Kestrel inside the container was not listening yet. Instrumenting `SpaIntegrationHost.StartAsync` with an immediate no-delay probe measured that gap at up to **810 ms / 26 failed attempts** (`HttpIOException: The response ended prematurely` and `Connection reset by peer`) right after the health wait returned. The weather fact is the only one that sends a live request through the ingress, so it was the only one exposed; whether it landed inside the gap depended on machine load.

Fix: the AppHost gives the yarp resource `WithHttpHealthCheck(endpointName: "http")` (path `/` -> web catch-all -> SPA shell 200, so it is environment-independent; web-gated because without the catch-all `/` would 404 forever; AppHost health checks execute only in run mode, never in publish). `Healthy` now means "the host proxy is wired and YARP routed a request to a live web-server", so the gate lives in the app model where every waiting suite inherits it — no retry, sleep, or widened timeout in test code. With the probe still instrumented, the first request succeeded on attempt 1 in every run (133-233 ms) and the ingress health wait began taking the real 9-14 s it had been skipping. Design regions reconciled in `program.cs`, `base-test.cs`, and `ingress-smoke-tests.cs` (whose hand-rolled `WaitForIngressReachableAsync` is now a documented backstop that returns on its first attempt). Gates: `dev build` 0/0, web-spa suite 40/40 three consecutive runs, `aspire-tests` 7/7, `ganda repo audit` 29 passed / 0 failed.

`dotnet run tools/dev-cli/dev.cs -- build`: 0 warnings, 0 errors. `dotnet run tools/dev-cli/dev.cs -- test`: exit 0. web-spa suite 40 passed / 0 skipped / 0 failed (the quarantined weather fact is one of the 40). The only remaining skip in `dev test` is web-server `RunForever` (manual, unrelated). Shell message-bar pixels were not checked in a browser; the handler path was proven headless.

### Review

Effort 1, roster `general`, two rounds (round 2 re-verified the same product diff). Counts: bug 0/0/0, suggestion 0/0/0, nit 0/0/0 (open/fixed/wontfix). Disposition **clean**. No wontfix and no escalation.

- Framework: `kanban/done/058-001-harden-web-spa-test-host-and-upgrade-timewarp-state/review/review-framework.md`
- Last merged: `kanban/done/058-001-harden-web-spa-test-host-and-upgrade-timewarp-state/review/round-2/merged.md`
- Disposition: `kanban/done/058-001-harden-web-spa-test-host-and-upgrade-timewarp-state/review/disposition.md`

### How to validate

**Smoke**

```bash
cd tests/container-apps/web/web-spa-integration-tests
dotnet test -c Release -- --filter-class FetchWeatherForecasts_Action_Should
dotnet test -c Release -- --filter-class CloneStateBehavior
```

Style Guide (running app): open the notifications card, click Error, then Throw exception. A `FluentMessageBar` appears at the top of the page. Dismiss removes it and it stays gone.

**Expect**

- Weather fact passes and `WeatherForecasts.Count` is 5. No `[Skip]`.
- Clone fact passes: after the thrown action, `ToastNotificationState.Messages` has one error bar titled `Test Rollback of State`, and the counter `Guid` is unchanged.
- `web-spa-integration-tests.csproj` has no `<AssemblyName>` override. Assembly name is `web-spa-integration-tests`.
- No `IToastService` / `FluentToastProvider` usage under `web-spa`.

**Automated gate**

```bash
dotnet run tools/dev-cli/dev.cs -- build
dotnet run tools/dev-cli/dev.cs -- test
ganda repo audit
```

**Not in scope**

- Aspire migration of the SPA suite (epic 145 two-lane model).
- Broader FluentUI v5 visual cleanup beyond this notification path.
- Fixing `ThrowIfNotTestAssembly` inside TimeWarp.State (timewarp-state#607).

## Session

- Implementation: ganda task-work implementer (2026-09-22)
- Review: grok session `01a0c8ad-9504-7392-896d-5f8b9eff692b` (2026-09-22)
- Review re-verify: grok session `01a0c8c8-5d89-7543-89ce-9db5ac7c3f37` (2026-09-22)

## 4. Modernize integration tests to Aspire testing (the bigger one)

The api/web/web-spa integration tests use a hand-rolled `WebApplicationHost` with FIXED ports
(web=7000, api=7255, yarp=8443) and manual `HttpClient`s — the pre-Aspire pattern. That's what forced
`dev test` to run sequentially (the suites collide on those ports). The modern approach is Aspire's
`DistributedApplicationTestingBuilder` (already used by `aspire-tests`): dynamic ports + service
discovery, no fixed-port management. Migrating would delete the sequential hack and the manual host.
Note: the HTTPS dev-cert (`UntrustedRoot`) issue is orthogonal — even the Aspire test hit it; CI now
runs `dotnet dev-certs https --trust` (the standard fix) rather than per-client handler bypass.


### Evidence addendum (2026-07-22, from PR 286 CI hang investigation)

Aspire test suites LEAK their AppHost containers on teardown: after local `dev test`, three
ingress containers + tunnelproxies were still Up 5 hours later, and CI's cancellation cleanup
listed multiple generations of orphaned web-server/api-server/grpc-server/dcp/docker processes.
The leak amplified the shared-postgres-volume WAL corruption (fixed separately, `943945a4` —
test AppHosts now run ephemeral postgres). Hardening scope for this task: deterministic
DistributedApplication disposal in SpaTestConvention/ApiServerTestConvention (await DisposeAsync
on convention completion), and consider a WaitFor/startup timeout so a wedged resource fails a
suite loudly instead of hanging it forever (Fixie has no per-test timeout).

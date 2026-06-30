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

## Checklist

- [ ] Upgrade TimeWarp.State (+ .Plus) past beta.1; migrate web-spa state handlers to the new API.
- [ ] Remove the `<AssemblyName>` override on web-spa-integration-tests.
- [ ] Fix the SPA→server weather fetch in `SpaTestApplication`; un-skip the quarantined test.
- [ ] Reconsider the toast-handler removal once FluentUI test support allows a real/stub provider.

## Notes

Not blocking — `dev test` is green with these workarounds (72 passed / 6 skipped / 0 failed).

## 4. Modernize integration tests to Aspire testing (the bigger one)

The api/web/web-spa integration tests use a hand-rolled `WebApplicationHost` with FIXED ports
(web=7000, api=7255, yarp=8443) and manual `HttpClient`s — the pre-Aspire pattern. That's what forced
`dev test` to run sequentially (the suites collide on those ports). The modern approach is Aspire's
`DistributedApplicationTestingBuilder` (already used by `aspire-tests`): dynamic ports + service
discovery, no fixed-port management. Migrating would delete the sequential hack and the manual host.
Note: the HTTPS dev-cert (`UntrustedRoot`) issue is orthogonal — even the Aspire test hit it; CI now
runs `dotnet dev-certs https --trust` (the standard fix) rather than per-client handler bypass.

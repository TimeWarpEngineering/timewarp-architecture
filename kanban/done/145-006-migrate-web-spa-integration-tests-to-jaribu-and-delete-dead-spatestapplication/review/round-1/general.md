# Round 1 — general
**Date:** 2026-08-02
**Scope reviewed:** commit 121b2c4b SPA Jaribu migration

## Summary

Reviewed the SPA suite migration to Jaribu MTP and the SpaTestApplication deletion against the current tree for task 145-006 (web-spa-integration-tests, timewarp-testing ISpaTestApplication / TestingConvention, kanban wall-clock + partial-graph notes).

**Claims check (read-only):**

| Claim | Verdict |
|-------|---------|
| Dead `SpaTestApplication` class gone | **Confirmed** — no `spa-test-application.cs` under `tests/common/timewarp-testing/applications/`; remaining mentions are comments, interface name `ISpaTestApplication`, or historical kanban |
| Clone-state on AspireSpaTestApplication | **Confirmed** — `pipeline/clone-state-behavior-tests.cs` uses SetupOnce + SpaTestScope |
| SpaTestScope + SpaIntegrationHost; re-fetch after Send | **Confirmed** — Design regions and fact bodies re-`GetState` after dispatch; generic `Send<TRequest>` avoids boxing |
| Partial-graph not adopted | **Confirmed** — documented in `base-test.cs` Design region and task.md |
| Wall-clock before/after recorded | **Confirmed** — task.md table (95.15s Fixie → 118.63s Jaribu; clone-state now full Aspire) |
| Jaribu shape vs peers | **Consistent** with aspire-tests / web-server-integration-tests: ModuleInitializer + `RegisterTests<T>`, static SetupOnce/CleanUpOnce, MTP + project-local `global.json`, Shouldly, no Fixie packages in csproj |

Migration shape is sound: host-level suite stays under `tests/`, pure serialization facts stay host-free, closed-box Aspire graph matches pre-migration SpaTestConvention (`Postgres:UseDataVolume=false`, wait healthy web/api/ingress). Toast ExceptionNotification removal landed on AspireSpaTestApplication as claimed.

No blocking bugs found. A few cleanups and a wall-clock footgun are worth follow-up.

## Issues

### Issue 1 — Severity: suggestion
- File: `tests/container-apps/web/web-spa-integration-tests/infrastructure/aspire-spa-test-application.cs`
- Description: After the BaseTest → SpaTestScope move, all facts dispatch via `SpaTestScope.Send` / `Store`. `AspireSpaTestApplication` still constructs a root-level `ScopedSender` and exposes `Send` overloads that no suite call site uses. Dead API surface leftover from the Fixie host-as-ISender shape; `ISpaTestApplication` correctly only exposes `ServiceProvider`.
- Suggestion: Drop `ScopedSender` field and both `Send` methods so the host is “compose ServiceProvider + ingress base URL” only. Optionally assert nothing still depends on them via a quick suite compile after removal.
- Status: open

### Issue 2 — Severity: suggestion
- File: `tests/container-apps/web/web-spa-integration-tests/features/weather-forecast/weather-forecast-state-fetch-weather-forecasts-action-tests.cs`
- Description: The class’s only fact is `[Skip]` (quarantine 058), but SetupOnce still boots a full Aspire AppHost and builds AspireSpaTestApplication. On a multi-class suite that already pays full-graph SetupOnce per host class, this is pure wall-clock cost for a class that never runs a fact — material for the 145-008 gate numbers (~118s after).
- Suggestion: Either (a) remove SetupOnce/CleanUpOnce/App/Spa from this class while the fact stays skipped, or (b) move the quarantined fact into a host-free placeholder file until 058 un-skips it. Re-add host lifecycle when the test is live again.
- Status: open

### Issue 3 — Severity: nit
- File: `tests/container-apps/web/web-spa-integration-tests/features/weather-forecast/weather-forecast-state-fetch-weather-forecasts-action-tests.cs` (Skip message)
- Description: Skip reason still attributes failure to ExceptionNotification / FluentToastProvider. Task 145-006 removed that notification handler on AspireSpaTestApplication, so the toast path should no longer be the primary failure mode. The skip text can mislead 058 work.
- Suggestion: Rewrite Skip (and task 058 notes when next touched) to the remaining root cause: SPA→server weather fetch wiring under the headless AspireSpaTestApplication host — without the toast narrative unless re-verified.
- Status: open

### Issue 4 — Severity: nit
- File: `tests/container-apps/web/web-spa-integration-tests/infrastructure/base-test.cs` (`SpaIntegrationHost.StartAsync`)
- Description: aspire-tests ingress smoke waits for resource Healthy **and** polls until the DCP proxy actually answers (connection-EOF race after Healthy). SPA suite stops at `WaitForResourceHealthyAsync` for web/api/ingress, then creates the ingress HttpClient inside AspireSpaTestApplication. Pre-migration likely same; suite is reported green so this is residual flake risk, not a proven regression.
- Suggestion: If flaky EOFs appear under load/CI, lift or share the reachability poll from aspire-tests before first SPA HTTP use (only matters for facts that hit the wire — e.g. weather when un-quarantined).
- Status: open

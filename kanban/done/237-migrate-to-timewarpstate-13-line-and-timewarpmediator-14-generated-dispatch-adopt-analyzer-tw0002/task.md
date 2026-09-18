# Migrate to TimeWarp.State 13 line and TimeWarp.Mediator 14 generated dispatch; adopt analyzer TWS0002

## Description

**Version correction (2026-09-17):** the TimeWarp.State release is **12.0.0-beta.3** (12.0.0 never had a stable release, so the beta line carries the breaks; there is no 13 line). Wherever this brief says "13 line" or "13.0.0-beta.x", read **12.0.0-beta.3**. Both upstream releases now exist on nuget.org: TimeWarp.State / .Plus / .Policies 12.0.0-beta.3 and TimeWarp.Mediator 14.0.0-beta.1 — this task is unblocked.

Queued behind two upstream releases (both now published):

1. **timewarp-state** next release from master — carries task 080 (Mediator 14 beta, breaking
   public API) and task 087 (analyzer **TWS0002**: a handler never sends an action, may publish a
   notification). Task 080 recorded "TimeWarp.State needs a major bump before release", so expect
   **13.0.0-beta.x**, not 12.0.0-beta.3.
2. **timewarp-mediator 14.0.0-beta.1** (already on nuget.org; "not a drop-in for 13.0.0").

Architecture today: `TimeWarp.State` / `.Plus` **12.0.0-beta.1**, `TimeWarp.Mediator` **13.0.0**
(`Directory.Packages.props` L111, L142-143). Mediator is used by **all three planes** (web-spa
state, web-server and api-server request handlers, and `TimeWarp.Foundation.Contracts`'s
`FluentValidationBehavior`), so this is a monorepo-wide migration, not a SPA-only bump.

Decision (Steve, 2026-09-17): a State is a boundary; handlers publish notifications and never
send actions. TWS0002 makes that declarative; this task is where architecture adopts it and gets
the errors "when we upgrade".

## Requirements

### A. Package bumps (one train)
- `TimeWarp.State`, `TimeWarp.State.Plus` (and `.Policies` if referenced) → **12.0.0-beta.3**.
- `TimeWarp.Mediator` 13.0.0 → `TimeWarp.Mediator.Contracts` + `.Generators` + `.Analyzers`
  14.0.0-beta.1 per the mediator readme (`Generators` on every host: web-server, api-server,
  web-spa, grpc-server if it uses Mediator; `Contracts` on libraries such as Foundation.*).
- Foundation packages are released from this repo at the same `<Version>`; bump per task 124
  rules (pins equal `<Version>`).

### B. Mediator 14 migration (per timewarp-state task 080 Results and mediator docs)
- Hosts call `AddGeneratedMediator()` (or `<TScope>` if named pipelines are wanted — default no)
  instead of any `AddMediator(...)`/manual registration. Architecture has **zero** literal
  `AddMediator(` calls today — find where the mediator is actually registered (`AddTimeWarpState`
  path in the SPA; explicit `AddScoped(typeof(IPipelineBehavior<,>), …)` in `web-server/program.cs`
  ~L309, `api-server/program.cs` ~L120-121) and replace with the generated registration.
- Pipeline behaviors become compile-time: `[assembly: MediatorBehavior(typeof(X<,>), order: N)]`
  in a `mediator-behaviors.cs` per host (state's own behaviors are 100-400; app behaviors 500+):
  `FluentValidationBehavior` (Foundation.Contracts), `GenericPipelineBehavior` (api-server),
  `TrackEventBehavior` (web-spa analytics), and any others found by grep.
- Every request/action type the generator must see is **public** (internal → CS0051). Architecture
  has ~41 `internal sealed class Action : IBaseAction` files under `web-spa/features/**/*-state/`
  plus server-side commands/queries — make them public (and their containing partial state
  classes where required, as the state samples did). Check the TWA analyzers / policies that
  currently *require* internal action sets and update them in the same PR (`tw-web-api-contracts`
  / `timewarp-state-policies` expectations).
- `TimeWarp.State.IAction` is gone → `TimeWarp.Mediator.IAction`; confirm what `IBaseAction`
  maps to in the 13 line and update usings/markers. `ActionHandler<T>` → `StateActionHandler<T>`
  rename where architecture derives from it (`features/base/*handler*.cs`).
- `[assembly: MediatorAssembly]` where the host generator must link handlers from a library
  assembly (Foundation.Contracts behaviors; web-contracts if handlers live there).

### C. Adopt TWS0002
- Build with TWS0002 on. Expected hits: `DefaultApiHandler.HandleError → ToastNotificationState.AddProblemDetails`
  and `FileResponseApiHandler` (cross-state), `ApplicationState.ResetStore → RouteState.ChangeRoute`.
  Convert the toast pattern to a **notification** (`ProblemDetailsNotification` published from the
  base; `ToastNotificationState` gets an `INotificationHandler` that adds the toast). ResetStore:
  sequence the route change from the caller (Counter page already does after 236). Do not use
  the `[AllowActionSend]` escape hatch except with a written reason in Results.
- Retire the 236 source-scan guard `handler-nested-dispatch-guard-tests.cs` in favour of TWS0002
  (keep the loading-literal and raw-button guards).
- Promote TWS0002 to **error** in `.editorconfig` for this repo once clean.

### D. Gates
- `dev build` 0/0 across all planes; `dev test`; SPA + prerender suites; Aspire ingress tests;
  `dev template-smoke` (generated apps must also register the generated mediator — update the
  template output, `.template.config`, and `timewarp-templates` tree accordingly).
- `ganda repo audit` clean. Docs: `auth.md`/developer guides that mention `AddMediator`.

## Depends on

- timewarp-state 12.0.0-beta.3 (published 2026-09-17; contains tasks 080 + 087). Satisfied.

## Checklist

- [x] A: State 12.0.0-beta.3 + Mediator 14.0.0-beta.1 packages on one train; Foundation pins aligned at 2.0.0-beta.20
- [x] B: generated mediator registration on every host; behaviors compile-time; public actions; marker/handler renames
- [x] C: TWS0002 clean (toast → notification; reset-store sequenced); 236 scan guard retired; TWS0002 error in editorconfig
- [x] D: `dev build` 0/0; `dev test` green; SPA + prerender + Aspire ingress; `ganda repo audit` clean (2 advisory); docs (no product `AddMediator` mentions); `dev template-smoke` SUCCEEDED (SmokeDefault + SmokeNoApi)
- [x] Results and How to validate (list every TWS0002 hit and its resolution)
- [x] Implementation review disposition (clean; M1 nit fixed on this id)

## Session

- Created: cockpit (2026-09-17)
- Claude Code cockpit session: https://claude.ai/code/session_01KPZXyAmA6Vk99W1yUQUn1N
- Implementer: Grok 4.6 ganda task-work (2026-09-18)
- Review oracle: Grok 4.6 ganda task-work (2026-09-18); effort 1, roster `general`, 2 rounds

## Notes

- Upstream references: timewarp-state `kanban/done/080-001-packages-and-addgeneratedmediator-from-origindev/task.md`
  (Results section is the consumer migration list), timewarp-state task 087 (TWS0002 spec),
  timewarp-mediator `readme.md` L36-50 and `documentation/generated-vs-legacy.md`,
  `documentation/m1-generated-mediator.md`, `m2-named-pipelines.md`.
- Architecture inventory (2026-09-17): `AddMediator(` 0 hits; `IBaseAction` in 41 files;
  `IPipelineBehavior<,>` in 6 files (api-server generic + validation, foundation-contracts
  validation, web-server validation, web-spa track-event); actions are `internal sealed`.
- Size: large, multi-plane. Expect the implementer to hit the turn budget; re-dispatch resumes.
  Consider splitting into 237-001 (packages + Mediator 14 hosts/behaviors), 237-002 (public
  actions + State renames), 237-003 (TWS0002 adoption + guard retirement + template) if the
  first pass shows the split is cleaner — decide in the planning step, not mid-implementation.

## Results

Migrated the monorepo (and therefore the `dotnet new timewarp-architecture` template) to **TimeWarp.State / .Plus 12.0.0-beta.3** and **TimeWarp.Mediator 14.0.0-beta.1** generated dispatch, and adopted **TWS0002** as error.

There is no State 13 line. Pins: State/Plus `12.0.0-beta.3`; Mediator split into `TimeWarp.Mediator.Contracts` + `.Generators` + `.Analyzers` `14.0.0-beta.1`. Foundation / analyzer / Identity / 402 CPM pins bumped with `<Version>` to **2.0.0-beta.20** (task 124). `.Policies` is not referenced.

### A. Packages

- `Directory.Packages.props`: dropped `TimeWarp.Mediator` 13.0.0; added Contracts/Generators/Analyzers 14.0.0-beta.1; State/Plus 12.0.0-beta.3.
- Hosts (Generators): web-spa, web-application (web-server's generator lives here to avoid CS0121 / Mediator issue 63), api-server.
- Libraries (Contracts + Analyzers): foundation-contracts, foundation-server, web-contracts, api-contracts, api-application, timewarp-testing.
- grpc-server does not host Mediator handlers; Contracts flow transitively from Foundation.Server.

### B. Generated mediator

- **api-server:** `AddGeneratedMediator()`; `[assembly: MediatorBehavior]` GenericPipelineBehavior 500, FluentValidationBehavior 510; `[assembly: MediatorAssembly]` on api-application.
- **web-server:** cannot call `AddGeneratedMediator` (references Web.Spa). Unique wrapper `AddWebApplicationGeneratedMediator()` compiled in web-application; FluentValidationBehavior 500 there; `[assembly: MediatorAssembly]` on web-application.
- **web-spa:** `AddWebSpaGeneratedMediator()` → `AddGeneratedMediator<ClientPipeline>()`; `[assembly: MediatorScope(typeof(ClientPipeline))]`; host behaviors 500–530 (pre/post pipeline notifications, ActiveActionBehavior, TrackEventBehavior). State library behaviors stay 100–400.
- **IBaseAction** = Foundation `IBaseRequest` + `TimeWarp.Mediator.IAction`. `BaseHandler<T>` derives from `StateActionHandler<T>`.
- Nested Action types under web-spa features are **public** (generator CS0051). No TWA analyzer required internal Action sets; timewarp-state-policies is not referenced.
- TWA0022 also matches `ISender<TScope>`.

### C. TWS0002 hits and resolutions

| Hit | Resolution |
|-----|------------|
| `DefaultApiHandler.HandleError` → `ToastNotificationState.AddProblemDetails` | Deleted `AddProblemDetails` ActionSet. Bases publish `ProblemDetailsNotification`; `ToastNotificationState.ProblemDetailsNotificationHandler` shows the toast. |
| `FileResponseApiHandler.HandleError` (cross-state) | Same notification. |
| `ApplicationState.ResetStore` → `RouteState.ChangeRoute` | Handler only `Store.Reset()`. Counter page already sequences `ResetStore()` then `ChangeRoute` (task 236). |
| `[AllowActionSend]` | **One use**, test-only: `NestedDispatchProbeState.UpdateActionSet.Handler` — probe proves ApiHandler releases the per-state semaphore before HandleSuccess. Product handlers must not nest. |

Retired `handler-nested-dispatch-guard-tests.cs`. Kept `razor-loading-literal-guard-tests.cs` and `razor-raw-button-guard-tests.cs`. `.editorconfig`: `dotnet_diagnostic.TWS0002.severity = error`.

### Consumer follow-ons (State 12.0.0-beta.3 generated graph)

- Always call `UseReduxDevTools` (CommitHandler needs Interop/Store/Options). `<ReduxDevTools/>` / `InitAsync` stay `ReduxDevToolsEnabled` (Debug); without Init, dispatch is a no-op.
- Register `IPersistenceService` / `PersistenceService` (Plus `LoadPersistentStateRequestHandler`).
- Generated `Publisher_ClientPipeline` resolves notification handlers by **concrete type**; `RemoveAll<INotificationHandler<T>>` is a no-op. Toast handlers swallow `FluentServiceProviderException<FluentToastProvider>` for headless tests.
- `TrackEventBehavior` is always in the generated pipeline (was Program.cs DI only). Telemetry failures (including missing `IWebServerApiService` in headless hosts) must not roll back the traced action.
- TimeWarp.State 12.0.0-beta.3 packs only `tsconfig.json` as contentFiles (tsc 6 options). TypeScript.MSBuild 7 compiles every Content tsconfig. **Do not exclude it with concatenated MSBuild percent-metadata in the csproj** — `dotnet new` treats percent-delimited spans as replacements and strips the Include line (SmokeDefault then fails TS18003/TS5108). The durable fix is `ExcludeAssets="contentFiles"` on the `TimeWarp.State` PackageReference (static web assets stay).
- Local `~/.nuget/packages/timewarp.state/12.0.0-beta.3` can be a stale 080 intermediate without TWS0002; delete and restore the nuget.org nupkg.

### Docs

No product `auth.md` / developer guide mentioned `AddMediator`. Kanban surveys that mention it are historical. `wwwroot/index.md` already points at TimeWarp.Mediator.

### Gates (this session)

- `./bin/dev build` — 0/0
- `./bin/dev test` — all suites passed (web-server-integration 239 passed / 1 skipped RunForever; web-spa-integration 38 passed / 1 skipped task 058 weather quarantine; aspire-tests 7/7)
- `ganda repo audit` — pass; 2 advisory (memsearch-scaffold, vscode-window-icon)
- `./bin/dev check-version` — 2.0.0-beta.20 > nuget 2.0.0-beta.19
- `./bin/dev template-smoke` — **SUCCEEDED** (SmokeDefault + SmokeNoApi; generated apps restore/build with generated mediator)

### How to validate

**Smoke**

```bash
cd /path/to/timewarp-architecture   # this task worktree
./bin/dev build
# expect: Build succeeded. 0 Warning(s) 0 Error(s)

./bin/dev test
# expect: Tests completed successfully!

# TWS0002 is error; product handlers must not Send
rg -n 'dotnet_diagnostic.TWS0002.severity' .editorconfig
# expect: error

# Generated registration on hosts
rg -n 'AddGeneratedMediator|AddWebSpaGeneratedMediator|AddWebApplicationGeneratedMediator' \
  source/container-apps --glob '*.cs'
# expect: api-server AddGeneratedMediator(); web-spa AddWebSpaGeneratedMediator();
#         web-application AddGeneratedMediator() behind AddWebApplicationGeneratedMediator

rg -n 'ExcludeAssets="contentFiles"' source/container-apps/web/projects/web-spa/web-spa.csproj
# expect: TimeWarp.State PackageReference has ExcludeAssets=contentFiles
```

**Expect**

- No `TimeWarp.Mediator` 13.0.0 pin; Contracts/Generators/Analyzers are 14.0.0-beta.1; State/Plus 12.0.0-beta.3.
- `handler-nested-dispatch-guard-tests.cs` absent; loading-literal and raw-button guards present.
- `ProblemDetailsNotification` published from `DefaultApiHandler` / `FileResponseApiHandler`; no `AddProblemDetails` ActionSet.
- `AllowActionSend` only on the nested-dispatch probe handler, with a reason string.
- Generated apps from `dotnet new timewarp-architecture` keep `ExcludeAssets="contentFiles"` on TimeWarp.State and call the generated mediator wrappers.

**Automated gate**

```bash
./bin/dev build          # 0/0
./bin/dev test           # all passed
ganda repo audit         # blocking checks pass (2 advisory allowed)
./bin/dev check-version  # 2.0.0-beta.20 is new vs last release
./bin/dev template-smoke # SUCCEEDED; SmokeDefault + SmokeNoApi 0/0
```

**Not in scope:** live browser Redux DevTools (Debug-only component); `[PersistentState]` (none in this template).

### Review

**Disposition:** clean (0 open). Effort 1, roster `general`, 2 rounds.

**Final counts** (round 2):

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 0 | 0 |
| suggestion | 0 | 0 | 0 |
| nit | 0 | 1 | 0 |

Round 1 raised M1 (nit: `AspireSpaTestApplication` Purpose/Design still claimed toast-handler removal after generated Publisher made `RemoveAll` a no-op). Fixed on this task id (Purpose/Design now describe the FluentServiceProviderException swallow). Round 2 re-verified M1 and raised nothing new.

**Paths:** `review/review-framework.md`, `review/round-2/merged.md`, `review/disposition.md`.


# Migrate to TimeWarp.State 13 line and TimeWarp.Mediator 14 generated dispatch; adopt analyzer TW0002

## Description

Queued behind two upstream releases (do not start until both exist on nuget.org):

1. **timewarp-state** next release from master — carries task 080 (Mediator 14 beta, breaking
   public API) and task 087 (analyzer **TW0002**: a handler never sends an action, may publish a
   notification). Task 080 recorded "TimeWarp.State needs a major bump before release", so expect
   **13.0.0-beta.x**, not 12.0.0-beta.3.
2. **timewarp-mediator 14.0.0-beta.1** (already on nuget.org; "not a drop-in for 13.0.0").

Architecture today: `TimeWarp.State` / `.Plus` **12.0.0-beta.1**, `TimeWarp.Mediator` **13.0.0**
(`Directory.Packages.props` L111, L142-143). Mediator is used by **all three planes** (web-spa
state, web-server and api-server request handlers, and `TimeWarp.Foundation.Contracts`'s
`FluentValidationBehavior`), so this is a monorepo-wide migration, not a SPA-only bump.

Decision (Steve, 2026-09-17): a State is a boundary; handlers publish notifications and never
send actions. TW0002 makes that declarative; this task is where architecture adopts it and gets
the errors "when we upgrade".

## Requirements

### A. Package bumps (one train)
- `TimeWarp.State`, `TimeWarp.State.Plus` (and `.Policies` if referenced) → the released 13 line.
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

### C. Adopt TW0002
- Build with TW0002 on. Expected hits: `DefaultApiHandler.HandleError → ToastNotificationState.AddProblemDetails`
  and `FileResponseApiHandler` (cross-state), `ApplicationState.ResetStore → RouteState.ChangeRoute`.
  Convert the toast pattern to a **notification** (`ProblemDetailsNotification` published from the
  base; `ToastNotificationState` gets an `INotificationHandler` that adds the toast). ResetStore:
  sequence the route change from the caller (Counter page already does after 236). Do not use
  the `[AllowActionSend]` escape hatch except with a written reason in Results.
- Retire the 236 source-scan guard `handler-nested-dispatch-guard-tests.cs` in favour of TW0002
  (keep the loading-literal and raw-button guards).
- Promote TW0002 to **error** in `.editorconfig` for this repo once clean.

### D. Gates
- `dev build` 0/0 across all planes; `dev test`; SPA + prerender suites; Aspire ingress tests;
  `dev template-smoke` (generated apps must also register the generated mediator — update the
  template output, `.template.config`, and `timewarp-templates` tree accordingly).
- `ganda repo audit` clean. Docs: `auth.md`/developer guides that mention `AddMediator`.

## Depends on

- timewarp-state release containing tasks 080 + 087 (external; check `dotnet package search TimeWarp.State --prerelease`).

## Checklist

- [ ] A: State 13 line + Mediator 14 packages on one train; Foundation pins aligned
- [ ] B: generated mediator registration on every host; behaviors compile-time; public actions; marker/handler renames
- [ ] C: TW0002 clean (toast → notification; reset-store sequenced); 236 scan guard retired; TW0002 error in editorconfig
- [ ] D: all gates incl. template-smoke; docs
- [ ] Results and How to validate (list every TW0002 hit and its resolution)

## Session

- Created: cockpit (2026-09-17)
- Claude Code cockpit session: https://claude.ai/code/session_01KPZXyAmA6Vk99W1yUQUn1N

## Notes

- Upstream references: timewarp-state `kanban/done/080-001-packages-and-addgeneratedmediator-from-origindev/task.md`
  (Results section is the consumer migration list), timewarp-state task 087 (TW0002 spec),
  timewarp-mediator `readme.md` L36-50 and `documentation/generated-vs-legacy.md`,
  `documentation/m1-generated-mediator.md`, `m2-named-pipelines.md`.
- Architecture inventory (2026-09-17): `AddMediator(` 0 hits; `IBaseAction` in 41 files;
  `IPipelineBehavior<,>` in 6 files (api-server generic + validation, foundation-contracts
  validation, web-server validation, web-spa track-event); actions are `internal sealed`.
- Size: large, multi-plane. Expect the implementer to hit the turn budget; re-dispatch resumes.
  Consider splitting into 237-001 (packages + Mediator 14 hosts/behaviors), 237-002 (public
  actions + State renames), 237-003 (TW0002 adoption + guard retirement + template) if the
  first pass shows the split is cleaner — decide in the planning step, not mid-implementation.

## Results

_Pending._

### How to validate

_Pending._

# Add client TrackEvent pipeline behavior that forwards opted-in SPA actions to the analytics endpoint

## Description

The server-side analytics slice already exists
(`source/container-apps/web/features/analytics/track-event/track-event-contracts.cs` —
`TrackEvent.Command`, `[ApiEndpoint]`, `[ApiRoute("Analytics/TrackEvent", HttpVerb.Post)]`,
`[EndpointAllowAnonymous]`, validator requires `EventName`; handler
`track-event-handler-application.cs` is a deliberate no-op with a TODO) but the SPA has no
analytics client. This task completes the client half and becomes the template's client
pipeline-behavior exemplar, replacing `event-stream` (retired in a follow-up task).

## Requirements

- New SPA slice `web-spa/features/analytics/` (SPA convention, not the cohesive
  `features/<slice>/` tree):
  - `[TrackEvent]` attribute — opt-in marker applied to action types.
  - `AnalyticsState : State<AnalyticsState>` with a `TrackEvent` ActionSet whose handler calls
    the existing web API service to POST `TrackEvent.Command { EventName, CorrelationId }`.
    Fire-and-forget semantics: failures logged at Debug, never surfaced, must not block the
    traced action.
  - `TrackEventBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>`
    constrained to `IAction`, registered in `program.cs` beside the existing behaviors
    (`ActiveActionBehavior<,>`, `EventStreamBehavior<,>` — see
    `source/container-apps/web/projects/web-spa/program.cs:145-146`). After `await next()`
    succeeds, check `typeof(TRequest)` for `[TrackEvent]`, dispatch via the generated
    `AnalyticsState.TrackEvent(...)` method off `IStore` (TWA0022 bans direct `Mediator.Send`
    in SPA code), skip its own action type to avoid recursion, and guard teardown exceptions
    (`OperationCanceledException` / `ObjectDisposedException`) exactly like
    `source/container-apps/web/projects/web-spa/features/event-stream/pipeline/event-stream-behavior.cs`
    does.
- Correlation id: grep `CorrelationId` in `web-spa` first and reuse whatever the SPA already
  carries per session/request if one exists; otherwise a per-app-load `Guid` held on
  `AnalyticsState`. Do not change `track-event-contracts.cs` unless `CorrelationId` is missing
  from the Command — if you must add it, keep it optional and update the validator and the
  contract tests.
- Tag `IncrementCounterActionSet.Action` in
  `source/container-apps/web/projects/web-spa/features/counter/counter-state/counter-state.increment-counter.cs`
  with `[TrackEvent]` so the demo fires; document the choice in its Design region.
- Mock mode: add a `MockResponseFactory` for `TrackEvent.Response` per the
  `tw-mock-response-factory` skill so mock-mode runs do not hit the network (`MockWebApiService`
  otherwise falls through to the real API for responses with no factory).
- Purpose/Design regions on every new file. Design must state: the client is intentionally
  dumb (event name + correlation id only — no payload, no vendor SDK); the server decides the
  sink; actions opt in via `[TrackEvent]` rather than tracking every action by default; and why
  a pipeline behavior rather than a post-action notification (keeps a client pipeline-behavior
  exemplar in the template).
- Tests (Jaribu, suite-shaped under
  `tests/container-apps/web/web-spa-integration-tests/features/analytics/`, C-create or
  `SpaSessionFixture` per AGENTS.md fixture-lifetime rules):
  - A `[TrackEvent]`-tagged action results in exactly one `TrackEvent` POST carrying the
    action's type name.
  - An untagged action results in no POST.
  - An API failure does not fail the traced action.
  - `AnalyticsState` clone test.
  - Serialization round-trip for `TrackEvent.Command` if it changed.
- Do not delete `event-stream` in this task (follow-up task, see Notes).

## Checklist

- [x] `[TrackEvent]` attribute added
- [x] `AnalyticsState` + `TrackEvent` ActionSet added
- [x] `TrackEventBehavior<,>` added and registered in `program.cs`
- [x] `IncrementCounterActionSet.Action` tagged `[TrackEvent]`, Design region updated
- [x] `MockResponseFactory` for `TrackEvent.Response` added
- [x] Purpose/Design regions on all new files
- [x] Tests: tagged-action POST, untagged-action no-POST, API-failure-does-not-block, state
      clone, contract round-trip (if changed)
- [x] `dev build` 0/0
- [x] `dev test`
- [x] `dev template-smoke`
- [x] Checked whether `skills/tw-slice-isolation/SKILL.md` lists pipeline behaviors — update
      only if it does (expected: no)
- [x] Implementation review (effort 1, general) — disposition `clean`

## Notes

- Origin: session discussion 2026-09-14 replacing the `event-stream` demo. Precedent
  (reference only, do not copy code): trinsic's client `EventTrackingProcessor` fires after
  actions marked `[TrackEvent]`, dispatches a `TrackEvent` action whose handler POSTs
  `api/Analytics/TrackEvent` with `EventName` + `CorrelationId`; the server decides the sink
  (trinsic used Segment). Decision here (Steve, 2026-09-14): client stays dumb (name +
  correlation id), server sink is OpenTelemetry, not a vendor SDK — see the follow-up task that
  wires the server sink.
- Follow-ups: server OTel sink task (depends on this one being mergeable independently — see
  that task's Notes); retire-`event-stream` task (depends on this task).
- Cockpit session: https://claude.ai/code/session_01QYpqCSgnvvLRpXrMKxu5ED

## Session

- Created: 2538042 (2026-09-14)
- Implementer: grok session 01a09def-2b52-7c11-a9e3-9b5d3bb55aa8 (2026-09-14)
- Review oracle: grok session 01a09e10-1006-7870-83f9-63b7048257c2 (2026-09-14)

## Results

SPA analytics client is wired as the template's pipeline-behavior exemplar. Opted-in actions
(`[TrackEvent]`) POST `TrackEvent.Command { EventName, CorrelationId }` after they succeed;
failures log at Debug and never fail the traced action. `event-stream` is untouched.

**Files**

- `source/container-apps/web/projects/web-spa/features/analytics/` — `[TrackEvent]` attribute,
  `AnalyticsState` + `TrackEventActionSet`, `TrackEventBehavior<,>`
- `source/container-apps/web/projects/web-spa/program.cs` — behavior registration
- `source/container-apps/web/projects/web-spa/features/counter/counter-state/counter-state.increment-counter.cs` —
  tagged + Design + `[CrossSliceReference]` (Counters → Analytics demo edge)
- `source/container-apps/web/features/analytics/track-event/track-event-contracts.cs` — optional
  `CorrelationId`, `GetMockResponseFactory()`
- `tests/container-apps/web/web-spa-integration-tests/features/analytics/` — C-create host +
  tagged/untagged/failure/clone facts
- `tests/container-apps/web/web-contracts-tests/features/analytics/` — Command round-trip

**Decisions**

- No existing SPA `CorrelationId`; per-app-load `Guid` on `AnalyticsState`.
- `EventName` is `Type.FullName` (nested Action types all have `Name == "Action"`).
- Handler does not use `DefaultApiHandler` (that path toasts). HTTP exceptions and
  problem-details arms are swallowed at Debug.
- `skills/tw-slice-isolation/SKILL.md` does not list pipeline behaviors — not updated.

**Test outcomes**

- `dotnet run tools/dev-cli/dev.cs -- build` — 0/0
- `dotnet run tools/dev-cli/dev.cs -- test` — passed (web-spa-integration-tests 22 succeeded / 1
  skipped weather quarantine; web-contracts-tests 42 succeeded)
- `dotnet run tools/dev-cli/dev.cs -- template-smoke` — succeeded (including SmokeNoApi after
  fully qualifying `IWebServerApiService` in the analytics test host)

### How to validate

**Smoke**

```bash
cd tests/container-apps/web/web-spa-integration-tests
dotnet test -c Release -- --filter-class TrackEventBehavior_
dotnet test -c Release -- --filter-class AnalyticsState_
cd ../web-contracts-tests
dotnet test -c Release -- --filter-class TrackEvent
```

**Expect**

- `TrackEventBehavior_`: 3 passed — tagged IncrementCounter POSTs once with
  `EventName == typeof(IncrementCounterActionSet.Action).FullName` and the state's
  `CorrelationId`; untagged `ToggleMenu.Action` POSTs nothing; `HttpRequestException` from the
  API still leaves Counter `Count` incremented.
- `AnalyticsState_`: 1 passed — clone copies `CorrelationId`, new `Guid`.
- `TrackEvent`: 2 passed — Command round-trips with and without `CorrelationId`.

**Automated gate**

```bash
dotnet run tools/dev-cli/dev.cs -- build   # expect: 0 Warning(s) 0 Error(s)
dotnet run tools/dev-cli/dev.cs -- test    # expect: Tests completed successfully!
dotnet run tools/dev-cli/dev.cs -- template-smoke  # expect: Template smoke SUCCEEDED
```

**Not in scope:** live OpenTelemetry sink (follow-up 216); retiring `event-stream` (follow-up 217).

**Review disposition**

- Rounds: 1. Effort 1, roster: general.
- Final counts: bug 0 / suggestion 0 / nit 0 (all open=0, fixed=0, wontfix=0).
- Outcome: **clean** (no findings raised).
- Paths: `review/review-framework.md`, `review/round-1/general.md`, `review/round-1/merged.md`, `review/disposition.md`.

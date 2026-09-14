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

- [ ] `[TrackEvent]` attribute added
- [ ] `AnalyticsState` + `TrackEvent` ActionSet added
- [ ] `TrackEventBehavior<,>` added and registered in `program.cs`
- [ ] `IncrementCounterActionSet.Action` tagged `[TrackEvent]`, Design region updated
- [ ] `MockResponseFactory` for `TrackEvent.Response` added
- [ ] Purpose/Design regions on all new files
- [ ] Tests: tagged-action POST, untagged-action no-POST, API-failure-does-not-block, state
      clone, contract round-trip (if changed)
- [ ] `dev build` 0/0
- [ ] `dev test`
- [ ] `dev template-smoke`
- [ ] Checked whether `skills/tw-slice-isolation/SKILL.md` lists pipeline behaviors — update
      only if it does (expected: no)

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

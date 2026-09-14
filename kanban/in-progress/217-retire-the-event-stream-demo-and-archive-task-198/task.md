# Retire the event-stream demo and archive task 198

## Description

With the client `TrackEvent` pipeline-behavior task (215) providing the template's client
pipeline-behavior exemplar, `event-stream` (an in-app action log standing in for Redux
DevTools) is redundant. Decision (Steve, 2026-09-14): delete it as an immediate follow-up to
215.

## Requirements

- Delete the `event-stream` inventory:
  - `source/container-apps/web/projects/web-spa/features/event-stream/**` (behavior, state +
    its 2 partials, `components/EventStream.razor`, `pages/EventStreamPage.razor(.cs)`).
  - `tests/container-apps/web/web-spa-integration-tests/features/event-stream/event-stream-state-clone-tests.cs`.
  - Registration/reference lines: `web-spa/program.cs:146` (`EventStreamBehavior<,>`),
    `web-spa/_Imports.razor:27`, `web-spa/global-usings.cs:75`,
    `web-spa/components/NavMenu.razor:40`,
    `tests/container-apps/web/web-spa-integration-tests/global-usings.cs:20`.
- Update `AGENTS.md:26` demo list from "counter, event-stream" to "counter, analytics" (client
  slice added by 215).
- Update `skills/tw-slice-isolation/SKILL.md` "Removing a demo slice" section to name Counter
  and the analytics client slice instead of EventStream.
- Adjust the `tools/dev-cli/services/template-smoke-harness.cs` web aggregator's expected test
  count (currently 148, hand-maintained) only if the deleted test was a web-jaribu aggregator
  runfile under `source/` — check whether `event-stream-state-clone-tests.cs` is aggregated
  (under `source/`) or suite-shaped (under `tests/`, as listed above) before touching the
  number. Per the inventory above it is under `tests/`, so the count likely does not change —
  confirm and record the finding in Results either way.
- `ganda kanban archive 198` with a one-line reason in 198's body: "event-stream retired by 217;
  guard no longer exists" (198 exists only to test event-stream's teardown guard, which is
  deleted by this task).
- Confirm `git grep -i "eventstream\|event-stream"` is empty across `source/`, `tests/`,
  `skills/`, and `AGENTS.md`.
- `dev build` 0/0, `dev test`, `dev template-smoke`.

- Move `[TrackEvent]` (`web-spa/features/analytics/track-event-attribute.cs`) from the
  `Features.Analytics` slice namespace to the SPA Features-substrate tier (bare
  `TimeWarp.Architecture.Features`, same tier as `RoleIds`/`ModuleIds`; see `tw-feature-placement`
  "Features substrate") so any slice can tag an action without a `[CrossSliceReference]` opt-out.
  Then drop the `[CrossSliceReference(typeof(AnalyticsState), …)]` added to
  `counter-state.increment-counter.cs` by 215 and reconcile both Design regions. Keep
  `AnalyticsState`/`TrackEventBehavior` in `Features.Analytics`. (Raised in 215's review.)

## Checklist

- [x] `[TrackEvent]` moved to the substrate namespace; counter's CrossSliceReference removed; Design regions reconciled
- [x] `web-spa/features/event-stream/**` deleted
- [x] `event-stream-state-clone-tests.cs` deleted
- [x] `program.cs:146` behavior registration removed
- [x] `_Imports.razor:27` using removed
- [x] `global-usings.cs:75` (web-spa) using removed
- [x] `NavMenu.razor:40` nav link removed
- [x] `global-usings.cs:20` (web-spa-integration-tests) using removed
- [x] `AGENTS.md:26` demo list updated to counter, analytics
- [x] `skills/tw-slice-isolation/SKILL.md` "Removing a demo slice" updated
- [x] `template-smoke-harness.cs` web aggregator count checked; changed only if warranted
- [x] `ganda kanban archive 198` with reason recorded in 198's body
- [x] `git grep -i "eventstream\|event-stream"` empty in source/, tests/, skills/, AGENTS.md
- [x] `dev build` 0/0
- [x] `dev test`
- [x] `dev template-smoke`

## Depends on

- 215

## Notes

- 215 (PR #356) and 216 (PR #355) merged 2026-09-14; this task is unblocked.

- 215 must land first: the client TrackEvent pipeline behavior is the replacement exemplar.

- Origin: task 210 review discussion plus session discussion 2026-09-14. Task 198
  (`kanban/to-do/198-deterministic-test-for-event-stream-trace-guard-against-disposed-state-dispatch.md`)
  exists only to test event-stream's teardown guard; its rationale is void once the guard's host
  is deleted.
- Cockpit session: https://claude.ai/code/session_01QYpqCSgnvvLRpXrMKxu5ED

## Session

- Created: 2544819 (2026-09-14)
- Implementer: grok session 01a09f3f-6cb8-7c41-8188-a92ab3ba377b (2026-09-14)

## Results

Event-stream is gone. `[TrackEvent]` is Features substrate so any slice can opt in
without a product→product `[CrossSliceReference]`. `AnalyticsState` /
`TrackEventBehavior` stay in `Features.Analytics`. Task 198 is archived.

**Files**

- Deleted `source/container-apps/web/projects/web-spa/features/event-stream/**`
  (behavior, state + 2 partials, `EventStream.razor`, `EventStreamPage`)
- Deleted `tests/container-apps/web/web-spa-integration-tests/features/event-stream/event-stream-state-clone-tests.cs`
- `web-spa/program.cs` — `EventStreamBehavior<,>` registration removed
- `web-spa/_Imports.razor`, `web-spa/global-usings.cs`,
  `web-spa-integration-tests/global-usings.cs` — EventStreams usings removed
- `web-spa/components/NavMenu.razor` — EventStreamPage nav link removed
- `web-spa/features/analytics/track-event-attribute.cs` — namespace
  `TimeWarp.Architecture.Features`; Design records substrate choice; XML `cref`
  to product `TrackEventBehavior` dropped (would be substrate→product under TWA0009)
- `web-spa/features/counter/counter-state/counter-state.increment-counter.cs` —
  `[CrossSliceReference]` and Analytics using removed; `[TrackEvent]` kept
- `web-spa/features/analytics/pipeline/track-event-behavior.cs` and
  `web-spa/hubs/chat-hub-connection.cs` — Design regions restated without event-stream
- `AGENTS.md` — demo list `counter, analytics`
- `skills/tw-slice-isolation/SKILL.md` — Removing a demo slice names Counter and
  the analytics client slice
- `kanban/archived/198-…` — one-line reason: "event-stream retired by 217; guard
  no longer exists"

**Decisions**

- `[TrackEvent]` stays physically under `features/analytics/` (same pattern as
  `RoleIds` under `admin/roles`) with the bare `…Features` namespace.
- `template-smoke-harness.cs` web aggregator count left at **148**. The deleted
  clone test is suite-shaped under `tests/…/web-spa-integration-tests/`, not a
  web-jaribu aggregator runfile under `source/`. Confirmed: `dev test` and
  template-smoke both report `web-jaribu-tests` 148/148.

**Test outcomes**

- `git grep -i "eventstream\|event-stream" -- source tests skills AGENTS.md` —
  empty
- `./bin/dev build` — 0 Warning(s) 0 Error(s)
- `./bin/dev test` — passed (web-spa-integration-tests 21 succeeded / 1 skipped
  weather quarantine; web-jaribu-tests 148 succeeded)
- `./bin/dev template-smoke` — Template smoke SUCCEEDED (web-jaribu 148/148 in
  generated Smoke and SmokeNoApi apps)

### How to validate

**Smoke**

```bash
git grep -i "eventstream\|event-stream" -- source tests skills AGENTS.md
# expect: no matches (exit 1)

ls source/container-apps/web/projects/web-spa/features/event-stream
# expect: No such file or directory

cd tests/container-apps/web/web-spa-integration-tests
dotnet test -c Release -- --filter-class TrackEventBehavior_
```

**Expect**

- Grep over `source/`, `tests/`, `skills/`, `AGENTS.md` is empty.
- Event-stream slice folder is gone; Developer nav has no EventStreamPage.
- `TrackEventBehavior_`: 3 passed — tagged IncrementCounter POSTs once;
  untagged `ToggleMenu.Action` POSTs nothing; API `HttpRequestException` still
  leaves Counter `Count` incremented. `[TrackEvent]` on IncrementCounter
  compiles with no `[CrossSliceReference]`.
- `kanban/archived/198-deterministic-test-for-event-stream-trace-guard-against-disposed-state-dispatch.md`
  exists and contains the line `event-stream retired by 217; guard no longer exists`.

**Automated gate**

```bash
./bin/dev build            # expect: 0 Warning(s) 0 Error(s)
./bin/dev test             # expect: Tests completed successfully!
./bin/dev template-smoke   # expect: Template smoke SUCCEEDED; web-jaribu 148/148
```

**Not in scope:** live Aspire dashboard / SPA click-through of `/EventStream`
(route deleted). Reviewer spawn is the later host review node.

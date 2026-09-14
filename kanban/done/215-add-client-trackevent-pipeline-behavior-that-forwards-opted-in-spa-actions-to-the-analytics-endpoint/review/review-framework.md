# Review framework — task 215

**Date:** 2026-09-14
**Host task:** kanban/in-progress/215-add-client-trackevent-pipeline-behavior-that-forwards-opted-in-spa-actions-to-the-analytics-endpoint/
**Diff scope:** branch `task/215-add-client-trackevent-pipeline-behavior-that-forwa` vs `origin/master` (commit `c2b39f76`)
**Plan / brief:** kanban/.../task.md — SPA `[TrackEvent]` opt-in pipeline behavior POSTs `TrackEvent.Command { EventName, CorrelationId }` after successful actions; template client pipeline-behavior exemplar; event-stream left in place
**Effort:** 1 (general only)
**Reviewer roster:** general
**Session IDs:** grok review oracle 01a09e10-1006-7870-83f9-63b7048257c2 (2026-09-14)

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`

## Brief (what must be true)

- `[TrackEvent]` opt-in on action types; not every action
- `AnalyticsState` holds per-app-load `CorrelationId`; `TrackEvent` ActionSet POSTs via `IWebServerApiService` (not `DefaultApiHandler` / no toasts)
- `TrackEventBehavior<,>` constrained to `IAction`, registered beside `EventStreamBehavior<,>`; after `await next()` succeeds, dispatch via generated `AnalyticsState.TrackEvent(...)` (TWA0022); skip own action; guard `OperationCanceledException` / `ObjectDisposedException` like event-stream
- Failures log at Debug; never fail the traced action
- IncrementCounter tagged; Design + `[CrossSliceReference]` for Counters → Analytics
- `GetMockResponseFactory()` on `TrackEvent` (generator registers it)
- Tests: tagged POST once with type FullName; untagged no POST; API failure does not fail traced action; state clone; Command round-trip
- Do not delete event-stream

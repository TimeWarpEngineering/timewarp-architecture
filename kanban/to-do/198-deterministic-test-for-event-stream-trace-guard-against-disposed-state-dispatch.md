# Deterministic test for event-stream trace guard against disposed-state dispatch

## Description

Task 196 (round-2 finding M10) widened the event-stream trace guard in
`source/container-apps/web/projects/web-spa/features/event-stream/pipeline/event-stream-behavior.cs`
to `catch (Exception exception) when (exception is OperationCanceledException or ObjectDisposedException)`
— after `State<TState>.Dispose` cancels and then disposes its CTS, reading the state's
`CancellationToken` throws `ObjectDisposedException`, and a lost trace entry must never fail the
action it traces. The guard shipped **untested** (recorded decision in task 196's
`review/disposition.md`): it is fail-safe by construction, but it has been wrong once already
(the round-1 version caught only the unreachable `OperationCanceledException`).

The round-3 reviewer supplied a deterministic recipe — no race, no flake:

- `State<TState>` implements `IDisposable` publicly, so a test can call
  `scope.Store.GetState<EventStreamState>().Dispose()` and then dispatch any action, asserting
  the action completes (and the trace entry is simply lost).
- The real constraint is **fixture lifetime, not flakiness**: the test needs a **C-create host**
  (own `HostGraphFactory` graph). Disposing a state inside a shared session fixture's store
  (e.g. `SpaSessionFixture`) would leak a dead state into sibling classes sharing that store.

## Checklist

- [ ] Add a C-create web-spa integration test: dispose `EventStreamState`, dispatch an action, assert it completes
- [ ] Assert the guard's failure mode (trace lost, action unaffected) rather than internals
- [ ] Suite green; `dev build` 0/0

## Notes

- Origin: task 196 review round-3 (`kanban/.../196-.../review/round-3/general.md`), offered as
  optional follow-up — explicitly not a blocker on 196's disposition.

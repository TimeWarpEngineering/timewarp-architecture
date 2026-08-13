# Round 1 — merged findings
**Date:** 2026-08-14
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 0 | 0 |
| suggestion | 0 | 4 | 1 |
| nit | 0 | 3 | 0 |

## Issues

### M1 — Severity: suggestion — Status: fixed
- File: AGENTS.md:214 (also source/Directory.Build.props:33, timewarp-architecture-convention-analyzers.csproj:6)
- Description: Range update overshot — three places claim the convention package carries
  "TWA0002–0022", but TWA0017/0018/0019 live in the Generators package. Precision regression
  (csproj previously said "TWA0002–0016, TWA0020").
- Suggestion: Write `TWA0002–0016, TWA0020–0022` in all three places.
- Source: general
- Disposition notes: fix this round.

### M2 — Severity: suggestion — Status: fixed
- File: spa-mediator-send-analyzer.cs:77-81
- Description: `OperationKind.Invocation` misses method-group conversions
  (`Func<...> dispatch = Mediator.Send;` then `dispatch(...)`) — the one realistic way to rebuild
  the deleted `BaseComponent.Send` leak without tripping the analyzer. (Extension-method gap is
  theoretical — none exist in TimeWarp.Mediator 13.0.0.)
- Suggestion: Also register `OperationKind.MethodReference` with the same `IsSenderType`
  predicate + test; note the extension-method gap in Design.
- Source: general
- Disposition notes: fix this round (register MethodReference + test + Design note).

### M3 — Severity: suggestion — Status: fixed
- File: event-stream-behavior.cs:74 (also chat-hub-connection.cs:38)
- Description: Sites that previously dispatched with `CancellationToken.None` now use the state's
  token, which is cancelled permanently on state disposal — teardown-only widening of the failure
  surface; Design regions claim only "strictly safer".
- Suggestion: Guard the event-stream trace dispatch with `catch (OperationCanceledException)`
  (a lost trace entry must never fail the action it traces) and record the teardown semantics in
  both Design regions.
- Source: general
- Disposition notes: fix this round (guard in event-stream behavior; Design notes both files).

### M4 — Severity: suggestion — Status: wontfix
- File: default-api-handler.cs:20 (also file-response-api-handler.cs:19)
- Description: Base handlers keep dead written-never-read `Sender` fields fed by ten derived
  constructors — preserves the affordance TWA0022 removes.
- Suggestion: Drop the `ISender` plumbing from both base handlers and derived ctors.
- Source: general
- Disposition notes: deferred to follow-up **task 197** (out of this diff's blast radius; the
  fields predate this change and TWA0022 already blocks any use of them). Decider: orchestrator.

### M5 — Severity: suggestion — Status: fixed
- File: spa-mediator-send-analyzer-tests.cs:357
- Description: Two cheap uncovered cases: conditional access `Mediator?.Send(...)` (different
  receiver-name derivation path) and dispatch through public `IState.Sender`
  (`Store.GetState<ChatState>().Sender.Send(...)`) — the most reachable bypass remaining.
- Suggestion: Add both cases.
- Source: general
- Disposition notes: fix this round.

### M6 — Severity: nit — Status: fixed
- File: chat-hub-connection.cs:34-39
- Description: Await-ness of the inbound handler rests on fragile overload resolution between
  `On<T1>(string, Func<T1, Task>)` and `On<T1>(string, Action<T1>)`; a later edit could silently
  revert to async-void.
- Suggestion: Assign the handler to an explicit `Func<ReceiveMessage.Command, Task>` local.
- Source: general
- Disposition notes: fix this round.

### M7 — Severity: nit — Status: fixed
- File: event-stream-behavior.cs:65-72
- Description: `if (request is BaseRequest)` / `else` branches assign identical strings
  (pre-existing, adjacent to this diff's edits).
- Suggestion: Collapse to a single assignment.
- Source: general
- Disposition notes: fix this round.

### M8 — Severity: nit — Status: fixed
- File: spa-mediator-send-analyzer.cs:141-144
- Description: Suffix-list exemption trade-off undocumented: user-authored `*.g.cs` is silently
  exempt; a generator with a non-`.g.cs` hint name and no `[GeneratedCode]` would be
  false-positived.
- Suggestion: One Design-region line naming the trade-off.
- Source: general
- Disposition notes: fix this round.

## Duplicates / conflicts

- None — single source.

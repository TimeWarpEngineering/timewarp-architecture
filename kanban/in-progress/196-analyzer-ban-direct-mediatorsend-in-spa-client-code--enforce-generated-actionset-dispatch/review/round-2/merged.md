# Round 2 — merged findings
**Date:** 2026-08-14
**Sources:** general

Carries round-1 IDs (M1–M8) with re-review verdicts; new findings M9–M11 from the round-1 fix
delta (43aa5f1a). Statuses below reflect the round-2 fix commit a4dca616.

## Counts (cumulative, final statuses)

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 0 | 0 |
| suggestion | 0 | 5 | 1 |
| nit | 0 | 5 | 0 |

(suggestions: M1, M2, M3, M5, M10 fixed; M4 wontfix → task 197. nits: M6, M7, M8, M9, M11 fixed.)

## Prior IDs — re-review verdicts (round 2)

- **M1 resolved** — range now `TWA0002–0016, TWA0020–0022` in all three places.
- **M2 resolved** — `OperationKind.MethodReference` registered with shared `IsSenderType`
  predicate; mutation-checked test `Given_Method_Group_Reference_To_Send_Flags`.
- **M3 resolved via M10** — the round-1 guard caught only `OperationCanceledException`, which is
  unreachable on this path; final fix in a4dca616 (see M10).
- **M4 wontfix accepted** — task 197 captures the finding accurately (files, ten derived ctors,
  defence-in-depth rationale, TWA0022-doesn't-flag note).
- **M5 resolved** — conditional-access and `IState.Sender` cases added.
- **M6 resolved** — explicit `Func<ReceiveMessage.Command, Task>` local; `Action<T>` overload
  now inapplicable.
- **M7 resolved** — identical branches collapsed.
- **M8 resolved** — suffix-list trade-off documented both directions.

## New issues (round 2, from fix delta 43aa5f1a)

### M9 — Severity: nit — Status: fixed
- File: spa-mediator-send-analyzer.cs:22 and :95
- Description: Design/inline examples illustrated the method-group gap with
  `Func<IRequest, Task> dispatch = Mediator.Send;`, which does not compile (CS0123 — optional
  parameters cannot be elided in a method-group conversion), contradicting the test.
- Suggestion: Use the compilable token-including shape.
- Source: general
- Disposition notes: fixed in a4dca616 — examples now
  `Func<IRequest, CancellationToken, Task>` with a CS0123 parenthetical.

### M10 — Severity: suggestion — Status: fixed
- File: event-stream-behavior.cs (trace-dispatch guard)
- Description: The round-1 guard caught `OperationCanceledException`, but that exception is
  unreachable on this path (no `ThrowIfCancellationRequested` in TimeWarp.State/State.Plus
  dispatch; Mediator 13.0.0 checks cancellation only in DI scanning; the AddEvent handler ignores
  its token). What teardown actually throws is `ObjectDisposedException`: `State<TState>.Dispose`
  cancels and then disposes the CTS, after which reading the state's `CancellationToken` throws.
  Verified by decompiling all three assemblies.
- Suggestion: Widen to
  `catch (Exception exception) when (exception is OperationCanceledException or ObjectDisposedException)`;
  correct the Design-region wording in both files (token cannot be read at all after disposal).
- Source: general
- Disposition notes: fixed in a4dca616 exactly as suggested; OCE kept, labelled forward cover.
  **Recorded decision:** the widened guard has NO test — pinning it would need a web-spa
  integration case that disposes a state mid-pipeline (flake-prone, beyond the finding's ask).
  Correctness rests on the decompilation evidence + round-3 review. Decider: orchestrator,
  reviewer concurrence requested in round 3.

### M11 — Severity: nit — Status: fixed
- File: spa-mediator-send-analyzer-tests.cs
- Description: `nameof(Mediator.Send)` was the untested neighbour of the new MethodReference
  registration.
- Suggestion: Negative case pinning it does not flag.
- Source: general
- Disposition notes: fixed in a4dca616 — own named case `Given_Nameof_Mediator_Send_Does_Not_Flag`
  (passes first-run; Roslyn surfaces no `IMethodReferenceOperation` for nameof). Suite 118/118.

## Duplicates / conflicts

- None — single source. M3/M10 are one defect across two rounds; both entries cross-reference.

# Round 3 — general
**Date:** 2026-08-14
**Scope reviewed:** fix commit a4dca616 against round-2 findings M9/M10/M11, plus the delta itself

## Summary

All three round-2 findings are resolved as specified, and the commit introduces no new defects.
M10's fix is the substantive one and it is correct in both halves: the guard now catches the
exception this path can actually raise, and both Design regions were rewritten to state the real
mechanism rather than the approximation they carried. I concur with shipping the widened guard
untested, with one qualification recorded below. **Review closes at 0 open.**

## Prior findings — verdicts

| ID | Verdict | Evidence |
|----|---------|----------|
| M9 | resolved | spa-mediator-send-analyzer.cs:22 and :96 now read `Func<IRequest, CancellationToken, Task> dispatch = Mediator.Send;`, which compiles: `TRequest` infers to `IRequest` and satisfies its own constraint, and the identity conversion beats the `Send(object, …)` candidate, so the group is unambiguous. The CS0123 parenthetical states the rule correctly. Comments and test now agree. |
| M10 | resolved | event-stream-behavior.cs:78 is `catch (Exception exception) when (exception is OperationCanceledException or ObjectDisposedException)` — exactly the suggested form, with OCE retained and labelled forward cover. Both Design regions (event-stream-behavior.cs:12-20, chat-hub-connection.cs:12-18) now say the token cannot be read at all after disposal and name `State<TState>.Dispose`'s cancel-then-dispose ordering; the chat hub keeps its deliberate-unguarded rationale intact. |
| M11 | resolved | `Given_Nameof_Mediator_Send_Does_Not_Flag` (spa-mediator-send-analyzer-tests.cs:359) pins the case as its own named test rather than folding it into the unrelated-`Send` case, which is the better placement. It passing settles the open question empirically: Roslyn surfaces no `IMethodReferenceOperation` for a bare `nameof` method group, so the new registration cannot break the build there. |

## Fix-delta sweep — no new defects

The widened catch cannot mask anything beyond its intent: it wraps only the trace dispatch, the
filter variable is used in the `when` clause so no unused-local applies, and the sole non-teardown
way to reach it would be an `ObjectDisposedException` from a genuine defect inside a handler that
does nothing but append a string — logged at Debug rather than lost. The two Design rewrites and
the two comment corrections are documentation-only, and the new test adds a negative case without
touching any existing one (117 → 118).

## Position on the untested guard

**Concur — ship it.** The guard is fail-safe by construction: its failure mode is not catching
something, which lands back on the pre-existing behavior, so an untested guard cannot break a path
that works today. Weighed against a test that would exercise a window the real app reaches only at
teardown, that is the right trade.

One qualification worth recording, since this guard has now been wrong once: a deterministic test is
more available than "dispose a state mid-pipeline" suggests. `State<TState>` implements `IDisposable`
publicly, so a test can call `scope.Store.GetState<EventStreamState>().Dispose()` and *then* dispatch
an action, asserting the action completes — no race, no timing. The real constraint is fixture
lifetime, not flakiness: such a test must own a C-create host (the repo default per 145-008), because
disposing a state inside `SpaSessionFixture`'s shared store would leak a dead state into the sibling
classes that share it. Offered as an optional follow-up alongside task 197, explicitly not a blocker
and not a dissent from the disposition.

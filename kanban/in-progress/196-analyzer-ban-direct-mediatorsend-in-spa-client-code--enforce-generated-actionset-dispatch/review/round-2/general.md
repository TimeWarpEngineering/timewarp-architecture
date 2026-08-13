# Round 2 — general
**Date:** 2026-08-14
**Scope reviewed:** fix commit 43aa5f1a against the round-1 ledger (review/round-1/merged.md), plus the fix delta itself for new defects

## Summary

Seven of the eight round-1 findings are genuinely resolved, and M4's wontfix is legitimate — task
197 captures the finding accurately, with the right file:lines and rationale. The one exception is
**M3: partially resolved.** The Design documentation half landed and the guard was added, but the
guard catches `OperationCanceledException`, which this dispatch path cannot actually raise, while
the exception state disposal really produces — `ObjectDisposedException` from reading a disposed
`CancellationTokenSource.Token` — passes straight through. I verified that by decompiling all three
assemblies in the path (details in M10). The new `AnalyzeMethodReference` handler is correct: it
shares `IsExemptGeneratedCode`, derives the receiver name the same way, reports at the reference
syntax, and cannot double-report against the Invocation handler. The three new test cases pin
exactly what they claim, including that the delegate *invocation* is deliberately not flagged.
Two new findings, both minor: one incorrect code example repeated in two comments, one cheap
untested edge on the newly registered operation kind.

## Prior findings — verdicts

| ID | Verdict | Evidence |
|----|---------|----------|
| M1 | resolved | All three sites now read `TWA0002–0016, TWA0020–0022`: AGENTS.md:214, source/Directory.Build.props:33, timewarp-architecture-convention-analyzers.csproj:6. Matches the convention project's `AnalyzerReleases.Unshipped.md`. |
| M2 | resolved | `OperationKind.MethodReference` registered (spa-mediator-send-analyzer.cs:96-101) routing to `AnalyzeMethodReference`, which applies the same `SendMethodName` / `IsSenderType` / `IsExemptGeneratedCode` gates and is pinned by `Given_Method_Group_Reference_To_Send_Flags`. Extension-method residue recorded in Design. See M9 for the comment's example. |
| M3 | **partially resolved** | Design regions updated in both files and the guard added, but the caught exception type does not match the one this path throws — see M10. |
| M4 | wontfix accepted | kanban/to-do/197-drop-dead-isender-plumbing-from-spa-base-api-handlers-and-derived-constructors.md reproduces the finding faithfully (both file:lines, the "written-but-never-read" characterization, the ten derived constructors, the defence-in-depth rationale, and an origin pointer back to merged.md M4). Its closing note — "TWA0022 does not flag the dead fields (no `Send` invocation)" — is correct, and the deferral rationale holds: any *use* of those fields would now be flagged, so the debt is inert rather than dangerous. |
| M5 | resolved | Three cases added, not two: `Given_Conditional_Access_Send_Flags` pins `Mediator?.Send(...)` resolving the receiver name to `IMediator` through the conditional-access instance shape, and `Given_Dispatch_Through_Public_State_Sender_Flags` pins `Store.GetState<ChatState>().Sender.Send(...)` → `ISender`. Spans are on the invocation only, so they pin location as well as detection. |
| M6 | resolved | chat-hub-connection.cs:42-46 assigns an explicit `Func<ReceiveMessage.Command, Task>` local and passes it to `HubConnection.On`, so the `Action<T1>` overload is no longer applicable at all — the binding cannot silently drift to async-void. Rationale recorded in Design. |
| M7 | resolved | event-stream-behavior.cs:67 is now one `string message = $"{tag}:{request.GetType().Name}";`; the dead `BaseRequest` test is gone and no using became unused. |
| M8 | resolved | Design paragraph added (spa-mediator-send-analyzer.cs:36-39) naming both directions of the suffix-list trade-off, with the reason the bias is deliberate (a false positive on generated code cannot be suppressed from source the user controls). |

## Issues

### Issue M9 — Severity: nit
- File: source/analyzers/timewarp-architecture-convention-analyzers/spa-mediator-send-analyzer.cs:22 (repeated verbatim at :95)
- Description: Both comments illustrate the closed gap as
  `Func<IRequest, Task> dispatch = Mediator.Send;`, which does not compile. A method-group
  conversion requires the delegate's parameter list to correspond one-for-one with the method's;
  optional parameters cannot be elided (CS0123), and every `ISender.Send` overload takes
  `(request, CancellationToken cancellationToken = default)`. The suite's own
  `Given_Method_Group_Reference_To_Send_Flags` uses the compilable form,
  `Func<Ping, CancellationToken, Task> dispatch = Mediator.Send;`, so the comments and the test
  disagree about the very construct the new operation kind exists to catch.
- Suggestion: Use the test's form in both comments (`Func<Ping, CancellationToken, Task>`, or
  `Func<IRequest, CancellationToken, Task>` to stay abstract).
- Status: fixed

### Issue M10 — Severity: suggestion
- File: source/container-apps/web/projects/web-spa/features/event-stream/pipeline/event-stream-behavior.cs:73 (Design region at :12-15)
- Description: The M3 guard catches the wrong exception type, so it is inert against the failure it
  was written for. Decompiling the three assemblies in this path:
  `State<TState>.Dispose(true)` calls `CancelOperations()` **and then**
  `CancellationTokenSource.Dispose()`; the generated ActionSet wrapper evaluates the state's
  `CancellationToken` property on every call (`linkedCts?.Token ?? CancellationToken`), and
  `CancellationTokenSource.Token` throws **`ObjectDisposedException`** once the source is disposed.
  That is what a post-disposal `AddEvent` actually raises, and `catch (OperationCanceledException)`
  does not catch it. Conversely, the caught type is unreachable here: TimeWarp.State and
  TimeWarp.State.Plus contain no `ThrowIfCancellationRequested` and no
  `OperationCanceledException` throw; TimeWarp.Mediator 13.0.0's only cancellation checks are in DI
  registration/assembly scanning, not dispatch; and `AddEventActionSet.Handler.Handle` ignores its
  `cancellationToken` entirely (it just appends to `EventList`). So a merely *cancelled* state token
  produces no exception at all, and the one exception the teardown window can produce escapes the
  guard and fails the traced action — the exact outcome M3 asked to prevent.
- Suggestion: Widen the guard, e.g.
  `catch (Exception exception) when (exception is OperationCanceledException or ObjectDisposedException)`
  (keeping OCE is still worthwhile as forward cover if a future handler observes the token), and
  adjust the Design wording in both this file and chat-hub-connection.cs:12-16 — "cancelled
  permanently once the state is disposed" understates it; the token cannot be read at all after
  disposal.
- Status: fixed

### Issue M11 — Severity: nit
- File: tests/analyzers/timewarp-architecture-analyzers-tests/spa-mediator-send-analyzer-tests.cs:455
- Description: The newly registered `OperationKind.MethodReference` has one untested neighbouring
  shape: `nameof(Mediator.Send)`. I did not verify how Roslyn shapes that operand (a bare method
  group is normally surfaced as an invalid operation rather than a method reference, in which case
  nothing fires), and I deliberately did not compile a probe. It is worth pinning precisely because
  the downside is asymmetric: in a repo where warnings are errors, a false positive there would
  break the build on an expression that dispatches nothing.
- Suggestion: Add a one-line negative case (`_ = nameof(Mediator.Send);` in an SPA-gated source,
  expecting no diagnostic) to the existing `Given_Unrelated_Send_Does_Not_Flag`.
- Status: fixed

## Fix-delta scan — cleared

- `AnalyzeMethodReference` (spa-mediator-send-analyzer.cs:127-142): shares the generated-code
  exemption (`IsExemptGeneratedCode` keys off `context.Operation`/`ContainingSymbol`, both
  operation-kind agnostic), derives `{0}` as `reference.Instance?.Type?.Name` with the same
  `ContainingType.Name` fallback, and reports at `reference.Syntax` — the `Mediator.Send` member
  access, not the whole statement, which is what the new test's 52→65 span pins. No double-report
  risk: a normal call site produces an `IInvocationOperation` whose receiver is an instance
  reference, and a delegate creation wraps exactly one method reference.
- The `try`/`catch` is correctly scoped: it wraps only the trace dispatch, sits inside the recursion
  guard, and leaves `next()` outside, so a cancellation from the traced action itself is never
  swallowed. The catch body is narrow and the log call is a proper structured template with one
  matching argument. (Its exception type is the M10 issue; the shape is right.)
- `HubConnection.On(nameof(ReceiveMessage), onReceiveMessage)` still binds the awaitable overload —
  with a `Func<T, Task>`-typed argument the `Action<T1>` overload is not applicable, so `T1` infers
  from the local and the dispatch stays awaited.
- Test count moves 114 → 117, matching the three added cases exactly; no existing case was edited.

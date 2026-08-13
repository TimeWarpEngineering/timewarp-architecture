# Round 1 — general
**Date:** 2026-08-14
**Scope reviewed:** commits 84ef1fd5..32b68eda + surrounding call sites

## Summary

The change adds TWA0022 (`SpaMediatorSendAnalyzer`), converts all seven web-spa direct-`Send`
call sites to generated ActionSet dispatch, deletes the `[Obsolete] BaseComponent.Send` wrapper,
and wires the Blazor-WASM SDK property as the SPA gate in both `Directory.Build.props` files.
The analyzer's semantics are sound where it matters: it keys on the *containing type* implementing
`TimeWarp.Mediator.ISender` rather than on the receiver expression, so the inherited `Mediator`
member, an injected `ISender`, the concrete `Mediator` class, the now-public `IState.Sender`, and
a generic-constrained receiver are all covered, while `Publish`/`CreateStream` and non-mediator
`Send` are not. I verified the two load-bearing claims independently: the emitted
`*ActionSet_Method.g.cs` wrapper carries no `GeneratedCodeAttribute` (so the path exemption really
is load-bearing) and is textually identical in shape to the hand-written `FiveSecondTask` it
replaces (so that deletion is signature- and behavior-compatible with `TestPage.razor:9`).
`grep` finds zero remaining `Send(` call sites in web-spa and no dangling references to any renamed
symbol. No correctness bugs found. The findings below are one docs-precision regression, three
robustness/follow-up suggestions, one behavioral edge worth recording, and three nits.

## Issues

### Issue 1 — Severity: suggestion
- File: AGENTS.md:214 (also source/Directory.Build.props:33, source/analyzers/timewarp-architecture-convention-analyzers/timewarp-architecture-convention-analyzers.csproj:6)
- Description: The range update overshot. All three places now claim the convention-analyzers
  package carries "TWA0002–0022", but TWA0017/0018/0019 live in the *Generators* package
  (`source/analyzers/timewarp-architecture-analyzers/`, confirmed by
  `ingress-route-prefix-generator.cs` and that project's `AnalyzerReleases.Unshipped.md`) — the very
  next AGENTS.md row says so. The convention package's actual set is TWA0002–0016 plus
  TWA0020–0022 (its `AnalyzerReleases.Unshipped.md`). The csproj previously used the precise form
  ("TWA0002–0016, TWA0020"), so this is a precision regression rather than an inherited staleness.
- Suggestion: Write the set as `TWA0002–0016, TWA0020–0022` in all three places.
- Status: open

### Issue 2 — Severity: suggestion
- File: source/analyzers/timewarp-architecture-convention-analyzers/spa-mediator-send-analyzer.cs:77-81
- Description: `RegisterOperationAction(..., OperationKind.Invocation)` only sees call sites, so a
  method-group conversion escapes the rule entirely:
  `Func<IRequest<T>, CancellationToken, Task<T>> dispatch = Mediator.Send;` is an
  `OperationKind.MethodReference`, and the later `dispatch(...)` is a delegate invocation whose
  `TargetMethod` is `Invoke` on the delegate type. That is the one realistic way to rebuild the
  deleted `BaseComponent.Send` leak without tripping the analyzer. (Extension methods named `Send`
  over `ISender` are likewise invisible, since `TargetMethod.ContainingType` is the static class —
  no such extension exists in TimeWarp.Mediator 13.0.0, so that one is theoretical.)
- Suggestion: Also register `OperationKind.MethodReference` and report when the referenced method
  matches the same `IsSenderType` predicate; or, if the gap is deliberately accepted, say so
  explicitly in the `#region Design` so the next reader does not assume invocations are exhaustive.
- Status: open

### Issue 3 — Severity: suggestion
- File: source/container-apps/web/projects/web-spa/features/event-stream/pipeline/event-stream-behavior.cs:74
- Description: Cancellation semantics changed for the two sites that previously dispatched with no
  token. `Sender.Send(addEventAction)` used `CancellationToken.None`; the generated wrapper uses
  `linkedCts?.Token ?? CancellationToken`, i.e. the *state's* token. Decompiling
  TimeWarp.State 12.0.0-beta.1 shows `State<TState>.CancellationToken` comes from a per-state
  `CancellationTokenSource` that `Dispose`/`CancelOperations()` cancels permanently — it is never
  reset. So once `EventStreamState` is disposed, every traced action's `AddEvent` throws
  `OperationCanceledException` from inside a pipeline behavior that wraps *every* dispatch, where
  before it could not fail. The same applies to `chat-hub-connection.cs:38` against `ChatState` for
  inbound hub messages. Nothing in the repo calls `CancelOperations()` and both states live for the
  app lifetime, so this is teardown-only and low-likelihood — but it is a real widening of the
  failure surface that the Design regions currently describe only as "strictly safer".
- Suggestion: No code change needed; record the teardown behavior in the two Design regions (or
  guard the event-stream trace with a `try/catch (OperationCanceledException)`, since a lost trace
  entry during teardown should never fail the action it is tracing).
- Status: open

### Issue 4 — Severity: suggestion
- File: source/container-apps/web/projects/web-spa/features/base/default-api-handler.cs:20 (also features/base/file-response-api-handler.cs:19)
- Description: `DefaultApiHandler.Sender` and `FileResponseApiHandler.Sender` are now
  written-but-never-read private fields (they already were at 84ef1fd5 — `HandleError` uses the
  generated `ToastNotificationState.AddProblemDetails`), and this change removed the last derived
  uses of the injected sender: ten handler constructors still thread `ISender sender` through DI
  purely to feed dead fields. Keeping a live `ISender` on the base of every SPA handler preserves
  exactly the affordance TWA0022 exists to remove — defence in depth would be to delete it.
- Suggestion: Follow-up task: drop the `ISender sender` parameter and field from both base handlers
  and the ten derived constructors. Out of this diff's blast radius, but squarely in its theme.
- Status: open

### Issue 5 — Severity: suggestion
- File: tests/analyzers/timewarp-architecture-analyzers-tests/spa-mediator-send-analyzer-tests.cs:357
- Description: The suite pins every claim the plan made, including both generated-code divergences
  (and the task notes record mutation checks for each, which is the right standard). Two cheap cases
  are missing, both of which pass today but nothing holds them: (a) conditional access
  `Mediator?.Send(...)` — the invocation is still reported, but `invocation.Instance` is an
  `IConditionalAccessInstanceOperation`, so the `{0}` receiver-name derivation at line 101 takes a
  different path than any covered case; (b) dispatch through the public `IState.Sender`
  (`Store.GetState<ChatState>().Sender.Send(...)`) — `State<TState>.Sender` is `public ISender { get; set; }`,
  making it the most reachable bypass now that every hand-rolled sender field is gone.
- Suggestion: Add the two cases alongside `Given_Concrete_Receiver_Implementing_ISender_Flags`.
- Status: open

### Issue 6 — Severity: nit
- File: source/container-apps/web/projects/web-spa/hubs/chat-hub-connection.cs:34-39
- Description: The Design region's claim that the inbound message "is awaited, so ... no longer
  fire-and-forget" rests on overload resolution picking `On<T1>(string, Func<T1, Task>)` over
  `On<T1>(string, Action<T1>)` — both extension overloads exist, both are applicable to
  `async (command) => await ...`, and `Func<T1, Task>` wins only via the better-conversion-from-
  expression rule (inferred return type `Task` beats a void-returning target). It binds correctly
  today, but a later edit that stops the lambda from being awaitable silently reverts it to
  `async void` with no diagnostic.
- Suggestion: Make the contract explicit — assign the handler to a
  `Func<ReceiveMessage.Command, Task>` local (or cast at the call) so the binding cannot drift.
- Status: open

### Issue 7 — Severity: nit
- File: source/container-apps/web/projects/web-spa/features/event-stream/pipeline/event-stream-behavior.cs:65-72
- Description: The `if (request is BaseRequest)` / `else` branches assign the identical string, so
  the test does nothing. Pre-existing, not introduced here, but it sits three lines from the edited
  recursion guard and dispatch in a file this diff otherwise reconciled.
- Suggestion: Collapse to the single assignment while the file is open.
- Status: open

### Issue 8 — Severity: nit
- File: source/analyzers/timewarp-architecture-convention-analyzers/spa-mediator-send-analyzer.cs:141-144
- Description: The path exemption is a suffix list, which cuts both ways: a *user-authored* file
  named `foo.g.cs` / `foo.generated.cs` / `foo.designer.cs` is silently exempt (this repo's TW0001
  kebab rule permits multi-dot partial filenames, so such a name is legal here), and conversely a
  generator that emits a hint name not ending in `.g.cs` and does not mark its output with
  `[GeneratedCode]` would be false-positived. Both are acceptable — the list matches Roslyn's own
  heuristic, and the sibling analyzers inherit the same behavior implicitly via
  `GeneratedCodeAnalysisFlags.None` — but the Design region presently reads as if the list were
  exhaustive and safe.
- Suggestion: One line in `#region Design` naming the trade-off (user files named `*.g.cs` are
  exempt by convention; generators with non-`.g.cs` hint names need `[GeneratedCode]`).
- Status: open

## Plan alignment

Every §4 ripple item is present and complete: the `ThrowExceptionActionSet` rename reached
`clone-state-behavior-tests.cs:59`, the `AddEvent`/`ServerToClientMessage` renames reached the
recursion guard, the XML `<remarks>`, `event-stream-state.debug.cs:6`, and
`chat-state.client-to-server-message-action.cs:7`, and the CA1711 suppression was dropped with the
rename that made it moot. `FetchCredentials(externalCancellationToken: cancellationToken)` correctly
preserves `includeRevoked: false` from the old `new Action()`. All §6 docs edits landed, including
the `source/Directory.Build.props:33` comment (see Issue 1 for its accuracy) and the
`AnalyzerReleases.Unshipped.md` row; the "no skill edits" conclusion re-verified — no skill carries a
TWA table or documents SPA dispatch. Region maintenance is genuinely done rather than perfunctory on
all eleven touched source files, and the record→class change on `FiveSecondTaskActionSet.Action` is
correctly explained as what makes the generator emit (it is also a small improvement: a parameterless
record makes all instances value-equal, which is wrong for per-instance action tracking).

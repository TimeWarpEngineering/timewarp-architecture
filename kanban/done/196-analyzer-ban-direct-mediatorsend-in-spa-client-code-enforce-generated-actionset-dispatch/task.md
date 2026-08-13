# Analyzer: ban direct Mediator.Send in SPA client code — enforce generated ActionSet dispatch

## Description

Client-side (SPA) code must never call `Mediator.Send(...)` directly — all dispatch goes through
the TimeWarp.State generated ActionSet methods (e.g. `ToastNotificationState.AddNotification(...)`),
which wire in `CancellationToken` automatically. Access modifiers cannot enforce this: `Mediator`
is a protected member inherited from TimeWarp.State's base component class, a derived class cannot
reduce inherited visibility, and `private new` shadowing is defeated by a base-type cast. So this
is an analyzer rule (new TWA diagnostic — next free ID, likely TWA0022), per the standing
"prefer analyzers over convention-by-memory" directive.

### Evidence: no legitimate client-side use exists (COPIC sweep, 2026-08-13)

Swept the shipped COPIC product (TimeWarp.State 11.0.0-beta.83, `[StateAccessMixin]` era —
ActionSet generation available and in active use):

- **8/8 client-side direct-send call sites were replaceable** by generated ActionSet methods.
  Every one dispatched a nested `ActionSet.Action` of a `[StateAccessMixin]` state class; none
  needed a raw send.
- The violations were inconsistency, not necessity: the same files used generated methods for
  sibling actions lines away (e.g. `CertificateHolderTable.razor` raw-sent
  `FetchCertificateHoldersForInsured` while calling generated
  `CertificateHolderState.DownloadCertificates(...)` below it); the production auth path used
  generated `AuthorizationState.FetchCurrentUser()` while the dev-only mock did the same job via
  raw send.
- COPIC's `BaseComponent.Send` wrapper (not `[Obsolete]`) was the funnel for all 7 component
  violations — the wrapper was the leak, not the fix.

Conclusion (Steve, 2026-08-13): client-side code has no legitimate direct `Mediator.Send` use.
The `BaseComponent.Send` wrapper cannot enable making `Mediator` private and loses automatic
cancellation-token wiring — no intermediate wrapper step; go straight to the analyzer and delete
the wrapper.

### Rule sketch

- **Flag:** any invocation of `Send` on the mediator (`IMediator`/`ISender` receiver, including
  the inherited `Mediator` property) from user-written SPA/component code.
- **Exempt:** generated code (`GeneratedCodeAttribute` / `.g.cs`, same convention as existing
  TWA analyzers) — the generated ActionSet dispatchers legitimately call `Mediator.Send`.
- **Scope decision to settle:** component code only (where ActionSet methods are the
  alternative) vs. all SPA client code. COPIC evidence says even non-component client services
  (mock auth provider) had ActionSet-equivalent paths — lean toward all client code, but confirm
  handler/pipeline internals (e.g. error-toast plumbing inside shared API handlers) are either
  generated-exempt or explicitly allowed.
- **Escape hatch:** COPIC evidence suggests none is needed for client code. If one is added
  anyway, follow the reasoned-attribute pattern (`[CrossSliceReference]`-style, reason required).

### In-repo cleanup this task owns

- `source/container-apps/web/projects/web-spa/features/base/base-component.cs:30` — delete the
  `[Obsolete]` `Send(IRequest)` wrapper (superseded by the analyzer).
- `source/container-apps/web/projects/web-spa/features/style-guide/pages/StyleGuidePage.razor.cs:27`
  — the one existing violation: `Mediator.Send(new CounterState.ThrowException.Action(...))`;
  switch to the generated `CounterState.ThrowException(...)` method.

## Checklist

- [x] Decide rule scope (components only vs all SPA client code) and escape-hatch posture; record in Design
- [x] Reserve next TWA ID and register descriptor in diagnostic-descriptors SSOT
- [x] Implement analyzer (flag mediator `Send` invocations; exempt generated code)
- [x] Analyzer tests: violation in component, violation via wrapper, generated-code exemption, non-client exemption per scope decision
- [x] Fix `StyleGuidePage.razor.cs` to use generated `CounterState.ThrowException(...)`
- [x] Delete `BaseComponent.Send` wrapper and reconcile base-component Design region
- [x] Update AGENTS.md TWA table + relevant skill docs with the new diagnostic
- [x] `dev build` 0/0

## Notes

- Origin: chat 2026-08-13 — Mediator exposed by base component; nesting of Actions inside State
  cannot help because visibility of the inherited `Mediator` member is not ours to restrict.
- COPIC sweep details (call-site list with file:line) are in the session that created this task;
  key conclusions captured above.

### Plan (2026-08-14)

Full implementation plan: `notes/implementation-plan.md`. Settled decisions:

- **Scope decided: all SPA client code, strict — handlers/behaviors included, no carve-outs.**
  Sites 3–5 below sit in `IRequestHandler` implementations yet are one-line replaceable;
  `DefaultApiHandler.HandleError` already uses a generated ActionSet method.
- **Diagnostic: TWA0022** (`Design`/Warning) in `timewarp-architecture-convention-analyzers`,
  new file `spa-mediator-send-analyzer.cs`.
- **Client-code gating:** `build_property.UsingMicrosoftNETSdkBlazorWebAssembly` (SDK-set,
  zero opt-in) via `CompilerVisibleProperty`; reference-detection rejected (web-server
  references web-spa for prerendering → would misfire).
- **Generated-code handling diverges from sibling analyzers:** must use
  `GeneratedCodeAnalysisFlags.Analyze|ReportDiagnostics` with a path-based exemption —
  razor-generated `*_razor.g.cs` trees (user `@code`) ARE analyzed; other `.g.cs`
  (incl. TimeWarp.State `*ActionSet_Method.g.cs`, which carries no GeneratedCodeAttribute)
  are exempt.
- **Spec corrections:** the StyleGuidePage violation lives in `StyleGuidePage.razor:37`
  (not the `.razor.cs`); there are SIX call sites in web-spa (not one) — style-guide, chat hub,
  principal/role/credentials handlers, event-stream behavior — all replaceable; several need
  `<Name>ActionSet` renames + explicit Action constructors (the generator ignores primary
  constructors) to trigger generation.

### Implementation notes (2026-08-14)

- **A SEVENTH call site existed** that the plan's grep missed:
  `application-state.five-second-task.cs` held a hand-written `FiveSecondTask` dispatcher that
  predated the generator (its sibling `two-second-task.cs` documented it as such). Found by
  running the analyzer, not by grep. Fixed the same way: its `Action` became a plain sealed class
  (matching `TwoSecondTaskActionSet`) so the generator emits the wrapper, and the hand-written one
  was deleted.
- **`FetchCredentials` takes a leading `bool includeRevoked = false`**, so its two call sites pass
  the token as `externalCancellationToken:` by name rather than positionally.
- Both generated-code divergences are pinned by mutation check, not just asserted: flipping the
  analyzer to `GeneratedCodeAnalysisFlags.None` fails exactly `Given_Razor_Generated_Tree_Flags`,
  and disabling the generated-path exemption fails exactly
  `Given_Generated_ActionSet_Dispatcher_Does_Not_Flag`.

## Results

### What changed

- **New analyzer TWA0022** (`source/analyzers/timewarp-architecture-convention-analyzers/spa-mediator-send-analyzer.cs`):
  bans direct `Send` on TimeWarp.Mediator `ISender`/`IMediator` (invocations AND method-group
  references) in SPA client code. Gated on `build_property.UsingMicrosoftNETSdkBlazorWebAssembly`
  (SDK-set, zero opt-in; wired via `CompilerVisibleProperty` in source/ + tests/
  `Directory.Build.props`). Diverges from sibling analyzers: razor-generated `*_razor.g.cs`
  trees ARE analyzed (user `@code`); other `.g.cs` trees (incl. TimeWarp.State
  `*ActionSet_Method.g.cs`, which carries no `GeneratedCodeAttribute`) are exempt.
- **Seven web-spa call sites converted** to generated ActionSet dispatch (style-guide page, chat
  hub, principal/role/credentials handlers, event-stream behavior, five-second-task hand-written
  dispatcher deleted), with `<Name>ActionSet` renames + explicit Action constructors where
  generation needed enabling. `BaseComponent.Send` wrapper deleted.
- **Event-stream trace guard**: trace dispatch wrapped in
  `catch (... is OperationCanceledException or ObjectDisposedException)` — a lost trace entry
  never fails the traced action, including dispatch against a disposed state.
- **Tests**: `tests/analyzers/timewarp-architecture-analyzers-tests/spa-mediator-send-analyzer-tests.cs`
  (component/non-component/all-overloads violations, method-group reference, conditional access,
  `IState.Sender` dispatch, razor-tree flagging, ActionSet-dispatcher exemption, non-SPA
  exemption, nameof negative, unrelated-Send negative). Divergences pinned by mutation checks.
- **Docs**: AGENTS.md TWA0022 row; package-range corrected to `TWA0002–0016, TWA0020–0022`;
  `AnalyzerReleases.Unshipped.md` row; Design regions reconciled on every touched file.

### Commits

`c0e7a0c6` (call-site conversions) → `20e95046` (analyzer) → `aa1d6b88` (tests) → `32b68eda`
(docs) → `43aa5f1a` (round-1 review fixes) → `a4dca616` (round-2 review fixes). Review/kanban
artifacts in `84ef1fd5`, `c3a328bb`, and the disposition commit.

### Review (Phase 4b)

3 rounds, effort 1 (single general reviewer; builder = twa0022-builder, reviewer =
twa0022-reviewer, orchestrator = this session). Final counts: bug 0/0/0, suggestion 0 open /
5 fixed / 1 wontfix, nit 0 open / 5 fixed. Disposition: **accepted-exceptions** — M4 (dead
`ISender` plumbing in base API handlers) deferred to **task 197**; recorded decision: M10's
teardown guard ships untested with reviewer concurrence, optional deterministic test captured
as **task 198**. Paths: `review/review-framework.md`, `review/round-{1,2,3}/`,
`review/round-2/merged.md` (final ledger), `review/disposition.md`.

### How to validate

**Smoke (enforcement is live):**

```bash
# 1. Introduce a violation in any web-spa file, e.g. StyleGuidePage.razor's TriggerException:
#      await Mediator.Send(new CounterState.ThrowExceptionActionSet.Action("x"), CancellationToken);
./bin/dev build   # from repo root
# 2. Revert the change.
git checkout -- source/container-apps/web/projects/web-spa/features/style-guide/pages/StyleGuidePage.razor
```

**Expect:** step 1 FAILS with `error TWA0022` at the `.razor` line (observed:
`StyleGuidePage.razor(38,11): error TWA0022: Direct 'IMediator.Send' call in SPA client code; …`);
after revert, `./bin/dev build` returns 0 warnings / 0 errors. Server projects (web-server
references web-spa for prerendering) build clean — the rule fires only in the WASM compilation.

**Automated gates (all verified passing at close):**

```bash
./bin/dev build                                                        # 0/0
cd tests/analyzers/timewarp-architecture-analyzers-tests && dotnet test -c Release   # 118/118
cd tests/container-apps/web/web-spa-integration-tests && dotnet test -c Release      # 15 pass / 1 pre-existing skip (task-058)
./bin/dev test                                                         # full sweep green
./bin/dev template-smoke                                               # SUCCEEDED (93/93 web-jaribu)
```

**Depends on / not in scope:** template-smoke required a `./bin/dev self-install` first (stale
AOT binary expected the pre-6ad90638 test count — environment issue, not this change). Dead
`ISender` plumbing removal → task 197; disposed-state guard test → task 198.

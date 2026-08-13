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

- [ ] Decide rule scope (components only vs all SPA client code) and escape-hatch posture; record in Design
- [ ] Reserve next TWA ID and register descriptor in diagnostic-descriptors SSOT
- [ ] Implement analyzer (flag mediator `Send` invocations; exempt generated code)
- [ ] Analyzer tests: violation in component, violation via wrapper, generated-code exemption, non-client exemption per scope decision
- [ ] Fix `StyleGuidePage.razor.cs` to use generated `CounterState.ThrowException(...)`
- [ ] Delete `BaseComponent.Send` wrapper and reconcile base-component Design region
- [ ] Update AGENTS.md TWA table + relevant skill docs with the new diagnostic
- [ ] `dev build` 0/0

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

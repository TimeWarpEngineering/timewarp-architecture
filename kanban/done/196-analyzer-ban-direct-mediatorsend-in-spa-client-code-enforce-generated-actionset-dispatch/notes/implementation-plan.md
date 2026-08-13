# Implementation Plan — Task 196: TWA0022, ban direct Mediator/ISender `Send` in SPA client code

Plan produced 2026-08-14 by the planning agent; decisions grounded in repo investigation.

## 0. Decisions already made (recorded, not reopened)

- Scope: ALL SPA client code, not just components (Steve, 2026-08-13; COPIC sweep 8/8 replaceable).
- No escape-hatch attribute in v1 (`#pragma warning disable TWA0022` remains the standard Roslyn valve if ever needed).
- Delete the `[Obsolete]` `BaseComponent.Send(IRequest)` wrapper (no intermediate wrapper step).
- Fix the StyleGuidePage violation via the generated `CounterState.ThrowException(...)` ActionSet method.
- Exempt generated code, per existing TWA convention.

## 1. Corrections to the task spec discovered by investigation

**a. The StyleGuidePage violation has moved.** `StyleGuidePage.razor.cs` is now a 17-line partial
holding only attributes; the `Mediator.Send` call lives in the **`.razor` `@code` block** at
`source/container-apps/web/projects/web-spa/features/style-guide/pages/StyleGuidePage.razor:37`.
This is architecturally decisive (see §3d): every existing TWA analyzer uses
`GeneratedCodeAnalysisFlags.None`, which **skips razor-generated trees** (`*_razor.g.cs` matches
Roslyn's generated-path heuristic) — a copy-paste of the TWA0021 pattern would never see this
violation. TWA0022 must analyze generated trees and implement its own exemption.

**b. There is not "one existing violation" — there are six non-generated `Send` call sites in
web-spa** (grep of `*.cs` + `*.razor`, obj/bin excluded), plus the wrapper:

| # | Site | Receiver | Replaceable by generated ActionSet method? |
|---|------|----------|--------------------------------------------|
| 1 | `features/style-guide/pages/StyleGuidePage.razor:37` | `Mediator` (IMediator) | Yes — after renaming `CounterState.ThrowException` → `ThrowExceptionActionSet` (§4a) |
| 2 | `hubs/chat-hub-connection.cs:34` | injected `ISender` | Yes — after renaming `ChatState.ServerToClientMessage` → `ServerToClientMessageActionSet` (§4c) |
| 3 | `features/admin/principals/principal-state/principal-state.set-principal-roles.cs:110` | `MediatorSender` (ISender) | Yes — `PrincipalState.FetchPrincipals(...)` already generated |
| 4 | `features/admin/roles/role-state/role-state.set-role-permissions.cs:99` | `MediatorSender` (ISender) | Yes — `RoleState.FetchRoles(...)` already generated |
| 5 | `features/identity/credentials-state/credentials-state.add-passkey.cs:114` and `credentials-state.revoke-credential.cs:58` | `Sender` (ISender) | Yes — `CredentialsState.FetchCredentials(...)` already generated |
| 6 | `features/event-stream/pipeline/event-stream-behavior.cs:73` | injected `ISender` | Yes — after renaming `EventStreamState.AddEvent` → `AddEventActionSet` (§4d) |
| 7 | `features/base/base-component.cs:30` | `Mediator` | Deleted with the wrapper |

**Stance on handler/pipeline internals (the point the task left open): no exemption — flag them
and fix them.** Evidence: sites 3–5 sit in `IRequestHandler` implementations yet are one-line
replaceable because their ActionSet-named siblings already generate dispatch methods, and the
`[StateAccess]` generator already gives `BaseHandler<TAction>` typed state accessors — indeed
`DefaultApiHandler.HandleError` **already** uses the generated
`ToastNotificationState.AddProblemDetails(...)`. Matches the COPIC conclusion with zero
carve-outs, keeps the analyzer simple, needs no structural handler-detection. The generated
method's linked-CancellationToken semantics are strictly safer than raw `Send(action, ct)`.

## 2. Analyzer home, ID, descriptor SSOT

- **Project:** `source/analyzers/timewarp-architecture-convention-analyzers/` — hosts all TWA
  `DiagnosticAnalyzer`s (TWA0002–0021). (`timewarp-architecture-analyzers/` hosts generators +
  TWE/SG descriptors — not where TWA descriptors go; convention analyzers declare descriptors
  inline, per `MockAuthenticationRegistrationAnalyzer.Rule`.)
- **ID:** **TWA0022** confirmed free.
- **New file:** `source/analyzers/timewarp-architecture-convention-analyzers/spa-mediator-send-analyzer.cs`,
  class `SpaMediatorSendAnalyzer`.
- **Descriptor (exact):**
  - Id: `TWA0022`
  - Title: `SPA client code must not call the mediator's Send directly`
  - MessageFormat: `Direct '{0}.Send' call in SPA client code; dispatch through the state's generated ActionSet method instead (name the nested action container '<Name>ActionSet' so TimeWarp.State generates one, and it wires the CancellationToken automatically)`
  - Category: `Design`; Severity: `Warning`, `isEnabledByDefault: true` (build-breaking via TreatWarningsAsErrors)
- **Release tracking:** add TWA0022 row to convention-analyzers `AnalyzerReleases.Unshipped.md` (RS2008).

## 3. Analyzer mechanics

**a. SPA-client gating — `build_property.UsingMicrosoftNETSdkBlazorWebAssembly`, auto-derived,
no per-project flag.** Precedent: TWA0009 reads `build_property.*` from
`AnalyzerConfigOptionsProvider.GlobalOptions` inside `RegisterCompilationStartAction` and no-ops
when absent (`slice-isolation-analyzer.cs:28-78`). The BlazorWebAssembly SDK sets
`<UsingMicrosoftNETSdkBlazorWebAssembly>true</...>` itself (SDK Sdk.props); web-spa is the repo's
only project on that SDK. Making the property compiler-visible turns "is this compilation SPA
client code" into an SDK fact, zero opt-in, template-safe.
**Rejected:** reference-detection (`ReferencedAssemblyNames` contains
`Microsoft.AspNetCore.Components.WebAssembly`) — web-server references web-spa for prerendering,
so WASM assets flow transitively into server compilations; it would misfire on the server host.
Analyzer bails at CompilationStart unless the property equals `true` (ordinal-ignore-case) or
`TimeWarp.Mediator.ISender` is unresolvable.

**b. MSBuild wiring (4 edits):** add
`<CompilerVisibleProperty Include="UsingMicrosoftNETSdkBlazorWebAssembly" />` beside the existing
`TimeWarpSliceRoot` entries in both conditional ItemGroups of `source/Directory.Build.props`
(lines 42, 46) and `tests/Directory.Build.props` (lines 49, 53). No `/tests/`-path exemption
(unlike TWA0021) — no test project uses the WASM SDK, so pipeline-driving test code is naturally
out of scope.

**c. What to flag (symbols verified in TimeWarp.Mediator 13.0.0):** `ISender` declares exactly
three `Send` overloads (generic IRequest<T>, generic IRequest, object); no `SendAsync`.
`IMediator : ISender, IPublisher` adds nothing. Implementation:
`RegisterOperationAction(OperationKind.Invocation)`; report when `TargetMethod.Name == "Send"`
and `ContainingType` is `ISender`/`IMediator` **or implements ISender** (covers concrete
`Mediator` class and `IState.Sender`). Do not flag `CreateStream`/`Publish`. Report at the
invocation with receiver type name as `{0}`.

**d. Generated-code handling — the one divergence from every sibling analyzer:** use
`GeneratedCodeAnalysisFlags.Analyze | ReportDiagnostics`, NOT `.None` (record in `#region Design`).
User-authored `@code` blocks compile into `*_razor.g.cs` trees that Roslyn's path heuristic calls
generated; `.None` would blind the rule to components. Custom path-based exemption on
`SyntaxTree.FilePath`:
- **Analyze** trees ending `_razor.g.cs` / `.razor.g.cs` / `_cshtml.g.cs` — user code
  (`#line` pragmas map locations back to the `.razor`).
- **Exempt** other generated trees (`.g.cs` / `.generated.cs` / `.designer.cs`) — covers
  TimeWarp.State ActionSet dispatchers (hint name `*.ActionSet_Method.g.cs`; decompilation
  confirms the emitted body calls `Sender.Send` and carries NO `GeneratedCodeAttribute`, so the
  path check is load-bearing). Belt-and-braces: containing-symbol `GeneratedCodeAttribute` check.

## 4. In-repo cleanup (ordered so the build stays green before the analyzer lands)

**a. Counter.** `counter-state.throw-exception.cs`: rename nested `ThrowException` →
`ThrowExceptionActionSet`; convert `Action`'s primary constructor to an explicit constructor
(the generator's `GetActionConstructorParameters` reads only `ConstructorDeclarationSyntax` —
primary ctor yields a non-compiling parameterless method). Yields
`public async Task ThrowException(string message, CancellationToken? externalCancellationToken = null)`
on `CounterState`. Delete the now-moot `#pragma warning disable CA1711`. Reconcile Design region.
`StyleGuidePage.razor:36-41`:
`await CounterState.ThrowException("Demo exception dispatched from the Style Guide.", CancellationToken);`
(update the TriggerException comment). Ripple:
`tests/container-apps/web/web-spa-integration-tests/pipeline/clone-state-behavior-tests.cs:59` →
`ThrowExceptionActionSet.Action` (only other reference).

**b. Handler fetch-chains (generated methods already exist):**
- `principal-state.set-principal-roles.cs:110` → `await PrincipalState.FetchPrincipals(cancellationToken);`; delete unused `MediatorSender` field.
- `role-state.set-role-permissions.cs:99` → `await RoleState.FetchRoles(cancellationToken);`; same cleanup.
- `credentials-state.add-passkey.cs:114` / `credentials-state.revoke-credential.cs:58` →
  `await CredentialsState.FetchCredentials(cancellationToken);`; remove leftover private `Sender` fields.

**c. Chat.** `chat-state.server-to-client-message-action.cs`: rename `ServerToClientMessage` →
`ServerToClientMessageActionSet`, explicit ctor `Action(ReceiveMessage.Command command)`.
`chat-hub-connection.cs`: replace injected `ISender` with `IStore`, async callback,
`await Store.GetState<ChatState>().ServerToClientMessage(command);` — also fixes the existing
un-awaited fire-and-forget `Send`. Reconcile both Design regions + cross-reference.

**d. Event stream.** `event-stream-state.add-event.cs`: rename `AddEvent` → `AddEventActionSet`;
explicit ctor `Action(string message)`. `event-stream-behavior.cs`: inject `IStore` instead of
`ISender`; recursion guard becomes `request is not AddEventActionSet.Action`; dispatch via
`await Store.GetState<EventStreamState>().AddEvent(message);`. Reconcile Design regions (they
document the raw-`Sender.Send` pattern) + stale mention in `event-stream-state.debug.cs:6`.

**e. Delete the wrapper.** `base-component.cs:29-30` — remove `[Obsolete] Send(IRequest)` (zero
callers) and rewrite the Design line about obsoleting Send to state TWA0022 enforcement instead.

## 5. Tests — `tests/analyzers/timewarp-architecture-analyzers-tests/spa-mediator-send-analyzer-tests.cs`

Follow TWA0021/TWA0009 shape: Jaribu MTP class, `CSharpAnalyzerTest<SpaMediatorSendAnalyzer,
RoslynTestVerifier>`, minimal TimeWarp.Mediator stubs, gate via
`test.TestState.AnalyzerConfigFiles.Add(("/.globalconfig", "is_global = true\nbuild_property.UsingMicrosoftNETSdkBlazorWebAssembly = true"))`.
Cases:
1. Component-shaped violation (protected `IMediator Mediator` property) → TWA0022.
2. Non-component client class (injected `ISender`, ChatHubConnection shape) → flagged; cover all
   three overloads + concrete receiver implementing ISender.
3. Razor-generated tree IS flagged (source at `/Pages_StyleGuidePage_razor.g.cs`) — pins the
   `.None`-blindness fix.
4. Generated-code exemption (source at `/App.CounterState.ThrowExceptionActionSet_Method.g.cs`) → clean.
5. Server/non-SPA not flagged (no `.globalconfig`; also property `= false`) → clean.
6. Non-mediator `Send` not flagged.
7. `dev build` 0/0 on the fixed tree is the end-to-end proof for StyleGuidePage.

## 6. Docs

- `AGENTS.md`: TWA0022 table row; update stale package-table range `TWA0002–0016` → `TWA0002–0022`.
- `source/Directory.Build.props:33` comment: same range update.
- Convention-analyzers `AnalyzerReleases.Unshipped.md`: TWA0022 row (Design / Warning).
- Skills: grep found no skill doc listing TWA0021 or ActionSet/Mediator.Send — no skill edits.
- Task 196: record settled decisions (strict incl. handlers/behaviors; SDK-property gating;
  razor-tree analysis), tick checklist, note spec corrections.

## 7. Build gates and verification

1. `dev build` 0/0. Full-rebuild caveat: analyzer DLLs go stale under incremental builds — if
   TWA0022 doesn't fire, clean convention-analyzers bin/obj or `dotnet build --no-incremental`.
   Sanity-check enforcement by temporarily re-introducing one `Mediator.Send` in web-spa and
   confirming the build breaks with TWA0022 at the `.razor` line; revert.
2. `cd tests/analyzers/timewarp-architecture-analyzers-tests && dotnet test -c Release`.
3. `cd tests/container-apps/web/web-spa-integration-tests && dotnet test -c Release` (rename ripple).
4. `dev test` full sweep.
5. `dev template-smoke` — every edit is template content; `CompilerVisibleProperty` wiring must
   survive template generation.

## 8. Ordered edit checklist

1. §4a–4e in-repo fixes + renames + wrapper deletion (build stays green).
2. `spa-mediator-send-analyzer.cs` + `AnalyzerReleases.Unshipped.md` row.
3. `CompilerVisibleProperty` wiring (4 spots).
4. Analyzer test suite.
5. Docs + Design-region reconciliation + kanban updates.
6. Verification gates.

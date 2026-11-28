# Keep an active Ask AI turn alive across route navigation instead of aborting when TimeWarpPage remounts AgentAsk

## Description

Live SigNoz GenAI traces and browser `ask-diag` logs (`1795875911` and `1795875956`) exposed a mid-turn abort bug whenever Ask AI invokes a tool that navigates to another route (`navigate`, `Role.OpenNewRole`, etc.):

1. This repo follows the empty-layout + per-page `TimeWarpPage` shell pattern (`tw-blazor-layout`), so navigating from `/` to `/Admin/Roles` (or `/Admin/Roles` to `/Admin/Roles/New`) disposes the outgoing `TimeWarpPage` and its child `AgentAsk`, then mounts a fresh `TimeWarpPage` and `AgentAsk` for the destination page.
2. `AgentAsk.razor` currently owns the live `UIAgent`, `AgentContext`, `FunctionInvokingChatClient`, and `CatalogAgentFunctions` as component fields and calls `ReleaseAgent()` in `Dispose()`.
3. Even though `AgentAsk.EnsureAgentAsync` explicitly tries to avoid mid-turn rebuilds (`if (TurnInProgress && BuiltGeneration == generation) return;`), `Dispose()` fires immediately when `RouteState.ChangeRoute` navigates to the new page!
4. That `Dispose()` cancels the in-flight `AgentContext` stream right after the navigation tool finishes (`tool-outcome completed=true`), **before**:
   - `UIAgent` can send the tool result back to the model for the next step (e.g., replying or calling `Prefill.Role.CreateRole` on `/Admin/Roles/New`), and
   - `AskConversationThread.CompleteTurn()` can commit the approved navigation tool call + result to `Committed`.
5. Result observed in SigNoz: Akili goes completely silent after navigating, drops the approved navigation call from conversation history, forces the user to re-prompt, and then repeats the exact same navigation tool call (`navigate("/Admin/Roles")` twice, `Role.OpenNewRole` twice).

## Propagation

likely

## Requirements

1. **Preserve the `tw-blazor-layout` empty-layout + `TimeWarpPage` architecture:**
   - Do **not** move app chrome out of `TimeWarpPage` into `MainLayout` (`tw-blazor-layout` rule).
   - Keep `AgentAsk` rendered inside `TimeWarpPage`'s aside, and keep the panel open across navigation.

2. **Decouple the live Ask agent session from `AgentAsk` component disposal (app/circuit-scoped host):**
   - Move ownership of the live `UIAgent`, `AgentContext`, `FunctionInvokingChatClient`, `AskTagChatClient`, `CatalogAgentFunctions`, and `BuiltPath`/`BuiltMode`/`BuiltGeneration` out of `AgentAsk.razor`'s component instance into an app/circuit-scoped service (alongside `AskConversationThreads`, which is already app/circuit-scoped).
   - When `AgentAsk` unmounts due to a route change while the Ask panel remains open (and the circuit is still alive), `AgentAsk.Dispose()` detaches its UI status subscription (`StatusSubscription`) and stops its local timer, **without** disposing or canceling the live `AgentContext` / `UIAgent`!
   - When the destination page's `TimeWarpPage` mounts the new `AgentAsk`, it reattaches to the **same** live `AgentContext` so `<MessageList>` continues rendering the active streaming turn seamlessly, `UIAgent` finishes sending the navigation tool result back to the model, `AskConversationThread.CompleteTurn()` commits the turn cleanly, and once the turn transitions to `Idle` (or `Error`), `EnsureAgentAsync` rebuilds the agent's tools/instructions for the new route (`BuiltPath != path`).
   - **Follow-up tool availability right after navigation (verify carefully!):** When a turn navigates to `/Admin/Roles/New` and the model's very next tool call in that **same** turn is `Prefill.Role.CreateRole` (or another page-scoped tool on the destination page), check how `CatalogAgentFunction.InvokeCoreAsync` handles `Wrapped` vs `current.RequiresApproval`:
     - On `/`, `Prefill.Role.CreateRole` was built with `wrapped: false` (off-page offer). After navigating to `/Admin/Roles/New` mid-turn, `current.RequiresApproval` is `true` (in `AskBeforeEditing` mode) while `Wrapped` is `false` — which would return `ApprovalRequiredError` instead of prompting for approval or completing cleanly!
     - Ensure that after a navigation tool completes mid-turn, either:
       - the turn commits cleanly and triggers the new page's rebuilt tools (or the model cleanly tells the user it arrived and offers/triggers the next step once rebuilt, OR the tool result / post-navigation rebuild hands off cleanly so the user is never stuck with a silent abort or a repeated navigation call, AND in `AutomaticallyEdit` mode prefill works seamlessly).
     - Document the chosen lifecycle clearly in the service's `#region Design`.

3. **Clean disposal on real teardown / conversation reset:**
   - The app/circuit-scoped host implements `IDisposable` so DI disposes the live `AgentContext` / `UIAgent` / `CatalogAgentFunctions` when the circuit/host ends.
   - `NewConversation` (generation change), sign-out / principal change, or readiness dropping from `Configured` still disposes/replaces the live agent immediately as before.
   - Closing the Ask panel (`CloseAsync`) still cancels pick mode, closes reference picker, restores focus to the opener, and behaves cleanly whether a turn is idle or in flight.

4. **Tests:**
   - Add automated integration tests (using `InteractiveHtmlRenderer` / `SpaTestScope` and a scripted `IChatClient`) proving:
     - Disposing and remounting `AgentAsk` mid-turn during a `navigate` (or `Role.OpenNewRole`) tool invocation does **not** cancel the in-flight `AgentContext`, commits the navigation tool call + result to `AskConversationThread`, and allows the model to complete its reply on the destination route.
     - After the turn settles to `Idle` on the destination route, the agent rebuilds for the new `BuiltPath` with the destination page's tools (including `Prefill.Role.CreateRole` wrapped for approval in `AskBeforeEditing`).
     - A subsequent turn sees the completed navigation in its message history and does not repeat the navigation call.
     - `NewConversation` and host disposal still cleanly dispose the previous `AgentContext` and `CatalogAgentFunctions`.
   - Verify `./bin/dev build` and targeted `./bin/dev test` pass with 0 warnings and 0 errors.

## Goal (implementer restatement)

Ask stays in `TimeWarpPage`'s aside. A route change still disposes that page and its `AgentAsk`, then mounts a new pair. The live turn must keep running across that swap.

The circuit-scoped `AskAgentSession` owns the `UIAgent`, `AgentContext`, invoking client, tag client, catalog functions, and the path, edit mode, and generation they were built for. Unmounting `AgentAsk` while the panel stays open only drops the panel's status subscription and its voice timer. The new panel attaches to the same context, so the model can finish the turn and the thread can commit the navigation call and its result. When the turn is idle or in error, the session rebuilds tools for the route now showing.

A tool set built before the navigation stays in place until that rebuild. An off-page tool that lands on its page and now requires approval does not run and does not pretend the edit mode changed: it tells the model this turn is done and the next turn can ask. In Automatically edit, approval is not required, so the same prefill runs on the page it just opened.

New conversation, a readiness drop, closing the panel, and disposing the circuit still drop the live agent. The next open or the next generation builds a new one.

## Results

`AskAgentSession` is the circuit-scoped owner of the live agent. `AgentAsk` still renders inside `TimeWarpPage`. Unmounting the panel while it stays open detaches the UI subscription and the voice timer and leaves the `AgentContext` running. Closing the panel, a new conversation, a readiness drop, and disposing the scope still release it.

The tool set from the start of the turn stays until the turn is idle or in error. An off-page tool that lands on its page and now requires approval returns `ArrivalNeedsApprovalError` and does not run. The idle rebuild then wraps that tool. In Automatically edit the same unwrapped function runs the prefill. The session records `agent-built`, `agent-deferred`, and `status` breadcrumbs, including `status=Idle` and `agent-built` for the destination path.

The build no longer suppresses `BL0013` on the session: it reads `AuthenticationStateProvider` from the scope on each rebuild instead of holding it. `ganda repo audit --fix` lowered the `AgentAsk.razor` `BL0013` baseline from 2 to 1 because that second suppression left with the rebuild.

### How to validate

Smoke: sign in, open Ask on `/`, and ask it to open the new role page. Approve the navigation. The panel stays open on `/Admin/Roles/New` and Akili finishes the reply. Ask again to fill the role. Expect one `navigate` call in the thread, the reply committed beside it, and no second navigation. After the turn is idle, diagnostics include `tool-start`, `tool-outcome`, `status=Idle`, and `agent-built` for `/Admin/Roles/New`, and `Prefill.Role.CreateRole` is wrapped for approval.

- `./bin/dev build` — passed, 0 warnings, 0 errors.
- `./bin/dev test tests/container-apps/web/web-spa-integration-tests/features/application/ask-agent-session-tests.cs` — passed, 3 tests. Not a standalone runfile (no `#if !JARIBU_MULTI`); CI runs it with the project. The doubles file is the other half of the same class.
- `./bin/dev test tests/container-apps/web/web-spa-integration-tests/features/application/ask-diagnostics-wiring-tests.cs` — passed, 9 tests. Not a standalone runfile; CI runs it.
- `./bin/dev test tests/container-apps/web/web-spa-integration-tests/features/application/agent-ask-readiness-tests.cs` — passed, 14 tests. Not a standalone runfile; CI runs it.
- `./bin/dev test tests/container-apps/web/web-spa-integration-tests/features/application/assistant-identity-tests.cs` — passed, 9 tests. Not a standalone runfile; CI runs it.
- `ganda repo audit` — passed, 44 passed, 0 failed.

Evidence: [evidence/agent-ask.png](evidence/agent-ask.png) is `AgentAsk` open on `/` for a signed-out visitor (Sign in to use Akili). [evidence/home.png](evidence/home.png) is the same home shell. Both shots are from this worktree's isolated AppHost (`aspire run --isolated`, http://127.0.0.1:63920) because a Development AppHost already held the shared dashboard and ingress ports. This evidence pass did not change product code; the gates above still stand.

## Notes

- Key files:
  - `source/container-apps/web/projects/web-spa/features/application/modals/agent-ask/AgentAsk.razor`
  - `source/container-apps/web/projects/web-spa/features/application/agent/ask-conversation-threads.cs`
  - `source/container-apps/web/projects/web-spa/features/application/agent/ask-conversation-thread.cs`
  - `source/container-apps/web/projects/web-spa/features/application/agent/catalog-agent-functions.cs`
  - `source/container-apps/web/projects/web-spa/features/application/agent/catalog-agent-session.cs`
  - `source/container-apps/web/projects/web-spa/program.cs` (DI registration)
- Keep `536-003`'s `AskDiagnostics` breadcrumbs working cleanly across remounts so SigNoz shows the full lifecycle (`tool-start`, `tool-outcome`, `status=Idle`, `agent-built` for the new path).

## Session

- Implement oracle: implemented by implementer-cursor (cursor, vendor xai, model grok-4.7-high), session not reported, max-turns uncapped (no --max-turns flag; bound 500), Done — 2026-11-28T16:26:06Z
- Implement oracle: implemented by implementer-cursor (cursor, vendor xai, model grok-4.7-high), session not reported, max-turns uncapped (no --max-turns flag; bound 500), Done — 2026-11-28T16:31:15Z

# Round 1 — security
**Date:** 2026-10-09
**Scope reviewed:** `git diff master...HEAD` (commit d2f3a442c) — `web-spa/features/application/agent/*`
(tool set, approval, arguments, functions, session, page scope, WebMCP dispatcher / approval gate /
publisher / registration), `agent-surface-state/*`, `AgentAsk.razor`, `WebMcpAgentSurface.razor`,
`page-agent-context.cs`, `services/js-web-mcp-model-context.cs`, `source/features/web-mcp.ts`,
`program.cs` registrations, `CommandPalette.razor` Ask button, catalog `[CatalogAction]` visibility
declarations, `catalog-agent-tests.cs`.

## Summary

Verified as holding:
- Human-only actions are never tools: `CatalogAgentToolSet.SelectAsync` requires
  `Visibility.HasFlag(ActionVisibility.Agent)` and both drivers go through it
  (`AddPasskey`, `AddExistingPasskey`, `LinkMicrosoft365`, `SignOut` are `Human`; test
  `Select_Is_Page_Scoped_Permission_Filtered_And_Drops_Human` covers it).
- Anonymous principal gets no catalog tools; per-entry permission uses
  `CommandPaletteRoster.IsPermittedAsync` (IAuthorizationService).
- WebMCP `InvokeTool` re-reads the auth state and re-selects the tool set for the current route on
  every call, so a stale registration / off-page tool name is rejected before approval.
- Mutating tools (name heuristic: not Fetch/Get/List/Search) are `ApprovalRequiredAIFunction` in chat
  and go through the in-app banner in WebMCP.
- No model credentials or secrets; no `IChatClient` is registered; Ask button and agent are idle
  without one. No server endpoint changed; handlers still call `[EndpointAuthorize]` endpoints.
- Argument binding throws on missing required args; malformed JSON throws `JsonException` before the
  gate is armed (fail-closed, though not mapped to the structured error — see Issue 6).

Gaps: the WebMCP approval gate is a single uncorrelated slot (concurrent calls can swap or share an
approval), the approval does not re-validate scope/permission after the wait, and the chat driver
never re-checks permission or page scope at invocation time.

## Issues

### Issue 1 — Severity: bug
- File: source/container-apps/web/projects/web-spa/features/application/agent/web-mcp-approval-gate.cs:18
- Description: `WebMcpApprovalGate` holds one `Pending` TCS and `ResolveApproval` carries only a
  `bool` — no correlation id, tool name, or argument hash. An external page agent controls call
  timing, so:
  (a) Call A arms + shows its banner; call B `Arm()`s (overwriting `Pending`) and `ShowApproval`
  replaces the banner text. A's TCS is orphaned and A hangs forever (`CancellationToken.None`).
  (b) If A yields between `Gate.Arm()` and `Gate.WaitAsync()` (it awaits the `ShowApproval`
  dispatch through the store/mediator pipeline at web-mcp-dispatcher.cs:89-90), B can arm in
  between; A's `WaitAsync` then captures B's TCS. One click on B's banner releases both, and A
  executes with arguments the person never saw.
  (c) Swap-under-click: the agent can issue a new call just as the person clicks Approve on the
  banner they read; the click resolves whatever is pending now, i.e. different tool/args than were
  read.
- Suggestion: give each pending approval an id (Guid) stored in `AgentSurfaceState` alongside the
  tool name and the exact arguments to be bound; `ResolveApproval(id, approved)` completes only that
  entry. Reject (or queue) a second mutating call while one is pending rather than overwriting.
  Add a timeout / cancellation so an unanswered or superseded call completes as rejected.
- Status: open

### Issue 2 — Severity: bug
- File: source/container-apps/web/projects/web-spa/features/application/modals/agent-ask/AgentAsk.razor:51
- Description: The chat driver selects tools once (`Started` is never reset) and
  `CatalogAgentFunction.InvokeCoreAsync` (catalog-agent-functions.cs:71-82) executes
  `Tool.Entry.Execute` with no re-check of principal permission or current page scope. The
  requirement is that permission filtering is re-checked at invocation time for both drivers;
  WebMCP does this, the chat driver does not. Tools listed for one route/principal stay callable
  after a same-instance navigation (query/fragment change, or any route where `TimeWarpPage`/
  `AgentAsk` is reused) or after a role/permission change, for the life of the component.
- Suggestion: in `InvokeCoreAsync` (or a shared helper used by both drivers) re-run
  `CatalogAgentToolSet.SelectAsync` for the current `AuthenticationState` + route and refuse if the
  tool is no longer present; reset `Started`/rebuild the agent on modal re-activation.
- Status: open

### Issue 3 — Severity: bug
- File: source/container-apps/web/projects/web-spa/features/application/agent/web-mcp-dispatcher.cs:90
- Description: Scope and permission are checked only before the approval wait. After `WaitAsync`
  returns true, `Execute` runs with the tool selected earlier even if the person has navigated away
  (the banner is in `TimeWarpPage`, state persists, so it is shown again on the new page and
  approving it runs the previous page's action) or the auth state changed (sign-out / role change)
  while the call was pending. This is the "tool called, then navigation" path the brief asks to
  close.
- Suggestion: after approval, re-read auth state and route, re-run `SelectAsync`, and refuse if the
  tool is no longer in scope; also clear/reject any pending approval in `SyncWebMcp` on location
  change.
- Status: open

### Issue 4 — Severity: suggestion
- File: source/container-apps/web/projects/web-spa/features/application/agent/web-mcp-dispatcher.cs:89
- Description: The banner shows the raw `argumentsJson` string, but what executes is the parsed
  dictionary bound case-insensitively (`CatalogAgentArguments.TryGet`), with unknown keys ignored
  and duplicate keys resolved last-wins by the deserializer. The displayed text and the bound values
  can diverge (e.g. `{"nickname":"a", ... ,"Nickname":"b"}`, or long whitespace padding pushing the
  effective value out of view in the `<pre>`).
- Suggestion: bind first (fail-closed on unknown/duplicate keys), then show a canonical rendering of
  the bound parameter values, and execute exactly those bound values after approval.
- Status: open

### Issue 5 — Severity: suggestion
- File: source/container-apps/web/projects/web-spa/components/WebMcpAgentSurface.razor:202
- Description: The WebMCP approval is an ordinary DOM button. A browser-resident agent that can both
  call WebMCP tools and actuate the page DOM (common for in-browser agents) can click Approve itself;
  Blazor click handlers cannot distinguish trusted user gestures. The in-app gate therefore does not
  stop an agent with DOM actuation.
- Suggestion: document this limit in the Design region; where the browser exposes it, use the
  WebMCP client's user-interaction API (`execute(input, client)` /
  `client.requestUserInteraction`) or require a trusted gesture (`event.isTrusted` checked in JS)
  for the confirmation.
- Status: open

### Issue 6 — Severity: nit
- File: source/container-apps/web/projects/web-spa/features/application/agent/web-mcp-dispatcher.cs:101
- Description: Only `ArgumentException` is mapped to a structured error. `JsonException` from
  `ParseArguments` (non-object or malformed JSON) or from `element.Deserialize` in
  `CatalogAgentArguments.ConvertValue` propagates as a JS-side rejection with .NET exception text.
  Still fail-closed (parse happens before arming; bind failure happens before Execute), but the
  bind/convert step runs after approval, so a person can approve a call that then fails to bind.
- Suggestion: catch `JsonException` alongside `ArgumentException`, and bind before showing the
  approval (ties in with Issue 4).
- Status: open

### Issue 7 — Severity: nit
- File: source/container-apps/web/projects/web-spa/features/application/agent/catalog-agent-approval.cs:24
- Description: Read-only is inferred from the action-name prefix (Fetch/Get/List/Search). A future
  mutating action named e.g. `GetOrCreate…` / `ListingPublish` would skip approval in both drivers.
  Fail-open direction for a naming slip.
- Suggestion: until timewarp-state has a read-only flag, keep an explicit allow-list of read-only
  catalog names (currently only `Credentials.FetchCredentials` is in any page scope) instead of a
  prefix rule.
- Status: open

# Round 1 — general
**Date:** 2026-10-09
**Scope reviewed:** `git diff master...HEAD` (d2f3a442c). All new web-spa agent files (`features/application/agent/*`, `agent-surface-state/*`, `AgentAsk.razor`, `WebMcpAgentSurface.razor`, `page-agent-context.cs`, `services/js-web-mcp-model-context.cs`, `services/web-mcp-js-module.cs`, `source/features/web-mcp.ts`), the `program.cs` / `TimeWarpPage.razor` / `CommandPalette.razor` wiring, and `catalog-agent-tests.cs` plus the test host changes. I checked these against the catalog actions in the page scope (`IncrementCounter`, `Credentials.*`, `UpdateProfile`, `CreateRole`, `UpdateSiteSettings`) and against `ContractSerializationDefaults`, `PasskeyPromptMode`, and `design.md`.

## Summary
The C# / TS contract matches. `WebMcpToolDescriptor` serializes as `{name, description, inputSchemaJson}`, which is what `web-mcp.ts` reads. `Replace` returns `{available, registered}`, which binds to `WebMcpApplyResult`. `invokeMethodAsync("InvokeTool", name, json)` matches the `[JSInvokable]` signature. The page scope, visibility, permission, and approval gates do what design.md says.

The main problems are these:
- The WebMCP approval gate allows only one pending call, and it has no cancellation and no re-check after approval.
- The argument binder does not use the contract seam's serializer options, so the enum in `UpdateSiteSettings` cannot be bound.
- Two page-scoped commands replace whole records, but the agent has no way to read the current values first.
- Several WebMCP error paths fall outside the `{action, error}` result shape.

Seven issues: three bugs, three suggestions, one nit.

## Issues

### Issue 1 — Severity: bug
- File: source/container-apps/web/projects/web-spa/features/application/agent/web-mcp-approval-gate.cs:18 (with web-mcp-dispatcher.cs:88-90)
- Description: Overlapping WebMCP calls break the single-slot gate.
  - `Arm()` overwrites `Pending` and does not complete the previous `TaskCompletionSource`.
  - `WaitAsync` reads `Pending` only after `await ShowApproval(...)`.
  - If an external agent sends two mutating calls A and B (for example a revoke and a rename), B's `Arm()` can run while A is suspended in `ShowApproval`. The banner then shows B, and both A and B await B's TCS. One Approve click runs A as well, and the person never saw A's tool name or arguments.
  - If B arms after A has already reached `WaitAsync`, A's TCS is orphaned and A never returns, because it waits with `CancellationToken.None`.
  - The JS `execute` promise for that call hangs forever in either case.
- Suggestion:
  - Have `Arm()` return its own TCS or a token, and have the dispatcher await that instance rather than re-reading the field.
  - Reject or queue a second call while one is pending. For example, `Arm()` completes the previous TCS with `false`, or the dispatcher returns `Error("Another action is waiting for confirmation.")`.
  - Add a test with two concurrent `InvokeTool` calls.
- Status: open

### Issue 2 — Severity: bug
- File: source/container-apps/web/projects/web-spa/features/application/agent/web-mcp-dispatcher.cs:88-98 (with components/WebMcpAgentSurface.razor:26-32)
- Description: The page and permission check runs once, before the approval wait. Nothing runs it again after approval, and nothing cancels the wait.
  - `AgentSurfaceState` is app-scoped state, so a pending confirmation survives navigation.
  - Example: the person moves from /Passkeys to /Counter, and the banner still shows a pending `Credentials.RevokeCredential`. Approving it there executes an action that is not in /Counter's page scope. This contradicts the Design region: "a stale registration cannot call an action the person can no longer run."
  - `TimeWarpFocusedPage` does not host `WebMcpAgentSurface`. If the person navigates to a focused page, nobody can answer the banner, and the WebMCP call waits forever.
  - Sign-out without navigation leaves the approval answerable.
- Suggestion:
  - In `OnLocationChanged`, before re-syncing, auto-reject any pending approval (dispatch `ResolveApproval(false)`).
  - After approval, re-select the tools for the current principal and path before calling `Execute`.
  - Consider a timeout or cancellation on `WaitAsync`.
- Status: open

### Issue 3 — Severity: bug
- File: source/container-apps/web/projects/web-spa/features/application/agent/catalog-agent-arguments.cs:17 (and :93), with catalog-agent-schema.cs:98
- Description: The binder uses its own `new JsonSerializerOptions(JsonSerializerDefaults.Web)` and has no `JsonStringEnumConverter`.
  - The contract seam uses `ContractSerializationDefaults.Options`, which has `JsonStringEnumConverter(allowIntegerValues: false)`.
  - `PasskeyPromptMode` (TimeWarp.Identity) has no type-level converter.
  - So `SiteSettings.UpdateSiteSettings` with `{"command":{"passkeyPromptMode":"Soft",...}}`, which is the shape the published schema asks for ("string"), throws `JsonException` in `Bind`. Only an integer would bind, and the schema never advertises one.
  - `ComplexSchema` describes nested non-primitive members as `{"type":"string","description":"<TypeName>"}`. For an enum it gives no `enum` list, so the model has to guess "Soft" or "Required".
  - This breaks AGENTS.md: "Serializer options for the contract seam come from ContractSerializationDefaults — never declare seam options inline."
- Suggestion:
  - Bind with `ContractSerializationDefaults.Options`, or a copy with `Apply(...)` plus web naming.
  - In `ComplexSchema`, emit `enum` names for enum members, the way `PropertySchema` already does at the top level.
  - Add a binder test for `UpdateSiteSettings`.
- Status: open

### Issue 4 — Severity: suggestion
- File: source/container-apps/web/projects/web-spa/features/application/agent/page-agent-scope.cs:35-37 (with components/page-agent-context.cs:39)
- Description: Two page-scoped tools replace whole records, but the agent cannot read the current record.
  - `Profile.UpdateProfile` takes a full `Command`: `Alias`, `Language`, `Region`, and `Theme` are all required by the generated schema.
  - `SiteSettings.UpdateSiteSettings` also takes a full `Command`, including the `Version` concurrency token.
  - `page_context` returns only `{path}` for /Profile and /Admin/Authentication. No read tool (`Get*`) is in either page's scope.
  - So a request like "turn off notifications" makes the model invent every other field and overwrite the stored profile. For site settings it can only fail on `Version` or clobber the settings.
  - Approval shows the arguments, but the banner and `FunctionApprovalBlock` show raw JSON without the current values, so the person cannot easily spot what was overwritten.
- Suggestion:
  - Extend `PageAgentContext.Describe` with the current profile and site-settings state, including `version`, for those routes.
  - Or drop those two actions from `PageAgentScope` until a read path exists.
  - Document the choice in the page-agent-scope Design region.
- Status: open

### Issue 5 — Severity: suggestion
- File: source/container-apps/web/projects/web-spa/features/application/agent/web-mcp-dispatcher.cs:97-104
- Description: The WebMCP error paths are inconsistent.
  - Only `ArgumentException` is turned into the `{action, error}` JSON result.
  - These escape to JS as a rejected `invokeMethodAsync` promise, outside the result contract the TS module and the Design region describe:
    - `JsonException` from malformed `argumentsJson` (a non-object, or a type mismatch in `element.Deserialize`, see Issue 3)
    - `NotSupportedException`
    - any exception from `Entry.Execute`
  - `CatalogAgentArguments.Bind` runs after the approval wait. A type error therefore shows up only after the person has approved.
- Suggestion:
  - Bind before `Arm()` and `ShowApproval`, so the confirmation shows a call that is known to bind.
  - Also catch `JsonException` and `NotSupportedException`, and decide whether `Execute` failures return an `Error(...)` result.
- Status: open

### Issue 6 — Severity: suggestion
- File: source/container-apps/web/projects/web-spa/features/application/agent/catalog-agent-session.cs:896-948 (`RunAsync`)
- Description: `RunAsync` is called only by `catalog-agent-tests.cs`. `AgentAsk` uses `CreateInvokingClient` together with `UIAgent` and `FunctionApprovalBlock`, which is a different approval loop.
  - The test "Fake_Client_Dispatches_Increment_Only_After_Approval" therefore proves the test helper's approval loop, not the one the product uses.
  - The `if (history.Count == before)` guard is dead, because `FunctionInvokingChatClient` never mutates the caller's list.
- Suggestion:
  - Either drive the test through `UIAgent` and `FunctionApprovalBlock`, or move `RunAsync` into the test project.
  - Keep `CreateInvokingClient` as the production seam.
  - Update the Design region ("Tests drive RunAsync…") to match.
- Status: open

### Issue 7 — Severity: nit
- File: source/container-apps/web/projects/web-spa/components/WebMcpAgentSurface.razor:26-32
- Description: Every navigation between two `TimeWarpPage` routes publishes the tools twice.
  - `TimeWarpPage` is re-created per page, but the old instance's `LocationChanged` handler still fires, because the delegate list was captured before the old page was disposed.
  - The new instance's `firstRender` then publishes again.
  - The two `ReplaceAsync` calls each import the module, and the second aborts the first registration.
  - The result is harmless today, but it explains the `ObjectDisposedException` catch, and it is easy to break.
- Suggestion:
  - Pick one trigger. Either subscribe once from a shell-lifetime host, or rely only on `firstRender` and keep the handler for focused-page transitions.
  - Note the choice in the Design comment.
- Status: open

# Round 1 — merged findings
**Date:** 2026-10-09
**Sources:** general, security, tests

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 4 | 0 |
| suggestion | 0 | 9 | 0 |
| nit | 0 | 3 | 1 |

## Issues

### M1 — Severity: bug — Status: fixed
- File: source/container-apps/web/projects/web-spa/features/application/agent/web-mcp-approval-gate.cs:18 (dispatcher :88-90, resolve-approval)
- Description: The WebMCP approval gate is a single slot with no correlation. A second call overwrites `Pending`, so the first call hangs forever or shares the second call's approval, and one click can release an action the person never saw. `ResolveApproval(bool)` resolves whatever call is pending when the person clicks.
- Suggestion: Give each pending call its own id and TCS. `ResolveApproval(id, approved)` resolves only that call. Reject a second mutating call while one is pending. Add a concurrency test.
- Source: general#1, security#1

### M2 — Severity: bug — Status: fixed
- File: source/container-apps/web/projects/web-spa/features/application/agent/web-mcp-dispatcher.cs:88-98; components/WebMcpAgentSurface.razor:26-32
- Description: Scope and permission are not re-checked after the approval wait. Navigation does not cancel the wait. Approving on another page runs the previous page's action. On a focused page (no banner) the call hangs forever.
- Suggestion: On location change, reject any pending approval. After approval, re-select the tools for the current principal and path, and refuse unless the same tool is still present on the same path.
- Source: general#2, security#3

### M3 — Severity: bug — Status: fixed
- File: source/container-apps/web/projects/web-spa/features/application/agent/catalog-agent-functions.cs:71-82; modals/agent-ask/AgentAsk.razor:51
- Description: The chat driver selects its tools once and runs `Execute` without re-checking permission or page scope when the tool is invoked.
- Suggestion: Re-select in `InvokeCoreAsync` for the current principal and route, and refuse if the tool is gone.
- Source: security#2

### M4 — Severity: bug — Status: fixed
- File: source/container-apps/web/projects/web-spa/features/application/agent/catalog-agent-arguments.cs:17; catalog-agent-schema.cs:98
- Description: The binder declares inline serializer options with no string-enum converter. `UpdateSiteSettings` with `"passkeyPromptMode":"Soft"`, the shape the schema advertises, throws. The nested schema does not list the enum values.
- Suggestion: Bind with the `ContractSerializationDefaults` options. Emit an `enum` list for nested enum members. Add a binder test.
- Source: general#3

### M5 — Severity: suggestion — Status: fixed
- File: features/application/agent/page-agent-scope.cs:35-37; components/page-agent-context.cs:39
- Description: `UpdateProfile` and `UpdateSiteSettings` replace whole records, including the Version token, but `page_context` returns only the path on those routes.
- Suggestion: Include the current profile and site-settings values (including version) in `page_context` for /Profile and /Admin/Authentication.
- Source: general#4

### M6 — Severity: suggestion — Status: fixed
- File: features/application/agent/web-mcp-dispatcher.cs:89-104
- Description: Arguments are bound only after approval. The banner shows raw JSON, not the bound values. Only `ArgumentException` is returned as a structured error; `JsonException` and `NotSupportedException` reject the JS promise.
- Suggestion: Bind before showing the approval. Show a canonical rendering of the bound values. Execute exactly those values. Map JSON and conversion errors to `{action, error}`.
- Source: general#5, security#4, security#6

### M7 — Severity: suggestion — Status: fixed
- File: features/application/agent/catalog-agent-session.cs (`RunAsync`)
- Description: Only tests call `RunAsync`. It contains a dead history-count guard. The fake-client test builds its tool by hand.
- Suggestion: Move the loop into the test project, or keep it with an honest Design region and remove the dead guard. Build the fake-client tools through `CatalogAgentToolSet.SelectAsync` and `CatalogAgentFunctions.Create`.
- Source: general#6, tests#5

### M8 — Severity: suggestion — Status: fixed
- File: components/WebMcpAgentSurface.razor
- Description: A browser agent that can also act on the DOM can click Approve itself.
- Suggestion: Document the limit in the Design region (and in design.md as a follow-up).
- Source: security#5

### M9 — Severity: suggestion — Status: fixed
- File: tests/.../catalog-agent-tests.cs:276
- Description: No test calls `InvokeTool` with an off-page, Human-only or unknown tool.
- Source: tests#1

### M10 — Severity: suggestion — Status: fixed
- File: tests/.../catalog-agent-tests.cs
- Description: The WebMCP reject path is untested.
- Source: tests#2

### M11 — Severity: suggestion — Status: fixed
- File: tests/.../catalog-agent-tests.cs
- Description: The permission-denial check on /Admin/Roles/New has no positive control.
- Source: tests#3

### M12 — Severity: suggestion — Status: fixed
- File: tests/.../catalog-agent-tests.cs
- Description: The WebMCP tool list is never tested with a denying principal on a page that has tools.
- Source: tests#4

### M13 — Severity: nit — Status: wontfix
- File: components/WebMcpAgentSurface.razor:26-32
- Description: Navigating between TimeWarpPage routes publishes the tools twice.
- Source: general#7
- Disposition notes: wontfix (orchestrator). Both triggers are needed: first render covers page-to-page moves; LocationChanged covers same-page route changes and focused pages with no banner. The replace is idempotent; the reason is recorded in the WebMcpAgentSurface Design comment.

### M14 — Severity: nit — Status: fixed
- File: features/application/agent/catalog-agent-approval.cs:24
- Description: Read-only is inferred from a name prefix. A mutating action that uses one of those prefixes would skip approval.
- Suggestion: Use an explicit read-only allow-list.
- Source: security#7

### M15 — Severity: nit — Status: fixed
- File: tests/.../catalog-agent-tests.cs
- Description: `Host_Has_No_Chat_Client` is true by construction.
- Source: tests#6

### M16 — Severity: nit — Status: fixed
- File: tests/.../catalog-agent-tests.cs
- Description: `await invoke` has no timeout. The /Settings visibility loop restates the filter instead of asserting that the Human-only actions are absent.
- Source: tests#7, tests#8

### M17 — Severity: suggestion — Status: fixed
- File: (absorbed) tests for M1/M2 concurrency and navigation-cancel
- Description: The fixes for M1 and M2 need regression tests: two concurrent calls, and navigation while a call is pending.
- Source: orchestrator

## Duplicates / conflicts

- general#1 + security#1 → M1. general#2 + security#3 → M2. general#5 + security#4 + security#6 → M6 (the strongest severity, suggestion, was kept). general#6 + tests#5 → M7. tests#7 + tests#8 → M16.

## Fix pass

Fixed in commit 3330690db (`fix(agent): address implementation review round 1 for task 271`). M15 was resolved by deleting the tautological test. Gates: dev build 0/0, CatalogAgent_Should 14/14, dev test passed, ganda repo audit passed.

# Round 1 — tests
**Date:** 2026-10-09
**Scope reviewed:** `git diff master...HEAD` test surface for task 271:
`tests/container-apps/web/web-spa-integration-tests/features/application/catalog-agent-tests.cs` (new),
`tests/container-apps/web/web-spa-integration-tests/infrastructure/aspire-spa-test-application.cs` (WebMCP + authorization registrations, `#if(!api)` principal provider),
checked against the task.md Phase 2 test list plus Steve's WebMCP addition, and against the product code under
`source/container-apps/web/projects/web-spa/features/application/agent/`.
Ran `dotnet test -c Release -- --filter-class CatalogAgent_Should`: 8/8 passed (1m 46s; Postgres health-check noise only). No AppHost started.

## Summary
Every required behavior has at least one real Shouldly assertion. The console `*-PROOF` lines always sit next to real assertions; none of them is the only check.
- Mapping: covered by the schema test (amount/integer present, CreateRole `userId` omitted) and JSON-to-int binding.
- Human-only exclusion: covered by the exact-list `ShouldBe` on `/Settings`, which drops AddPasskey, AddExistingPasskey and LinkMicrosoft365.
- Ask-path approval gating, rejected and approved: covered. If the approval wrapper were lost, the rejected branch would fail (count 10 → 15).
- No-model path: covered by `Program.ConfigureServices`.
- WebMCP absent-API no-op and per-page registration: covered.

Fixture use is fine. The suite uses the documented C-share `SpaSessionFixture`, which is this project's exemplar. Each test makes its own `SpaTestScope`, so store and navigation state stay per-test. No fixed-port problems were introduced.

The gaps are negative paths. These tests would still pass even if some guarded behaviors broke or lost their meaning:
- The WebMCP invoke guard has no test: an untrusted agent calling an off-page, Human-only or unpermitted action.
- WebMCP rejection has no test.
- The permission-denial assertion has no positive control.
- WebMCP permission filtering has no denying principal.

## Issues

### Issue 1 — Severity: suggestion
- File: tests/container-apps/web/web-spa-integration-tests/features/application/catalog-agent-tests.cs:276
- Description: The WebMCP dispatcher's untrusted-agent guard has no test. `WebMcpDispatcher.InvokeTool` selects tools again for the current principal and route, and its Design region says this is how "a stale registration cannot call an action the person can no longer run". That is the security boundary for an external browser agent, and no test exercises it. Nothing calls `InvokeTool` with a name that is off-page (for example `Role.CreateRole` while on `/Counter`), Human-only (`Credentials.AddPasskey` on `/Settings`), or unknown. If the re-selection were removed and `Catalog.Find(name)` executed directly, every test would still pass.
- Suggestion: Add a test that navigates to `/Counter`, then calls `InvokeTool("Role.CreateRole", …)` and `InvokeTool("Credentials.AddPasskey", …)`. Assert the result has the "not available on this page" error, `HasPendingApproval` stays false, and no state changes.
- Status: open

### Issue 2 — Severity: suggestion
- File: tests/container-apps/web/web-spa-integration-tests/features/application/catalog-agent-tests.cs:298
- Description: Only the approved branch of WebMCP approval is tested. `ResolveApproval(approved: false)` should return `{"action":…,"approved":false}` and leave `CounterState.Count` at 10. Nothing checks that, so a regression that executes the action whatever the gate returns, or that ignores `!approved`, would pass. The pre-approval `Count.ShouldBe(10)` proves the call waits, but not that rejection blocks it. Task guardrail 3 makes WebMCP's in-app confirmation the equivalent of `ApprovalRequiredAIFunction`, so rejection deserves the same coverage the ask path has.
- Suggestion: Add a rejected variant: arm the call, call `ResolveApproval(false)`, then assert the count is unchanged and the result contains `"approved":false`.
- Status: open

### Issue 3 — Severity: suggestion
- File: tests/container-apps/web/web-spa-integration-tests/features/application/catalog-agent-tests.cs:110
- Description: The only permission-denial assertion has no positive control. `developer` on `/Admin/Roles/New` returning empty shows filtering only while `Role.CreateRole` stays Agent-visible and mapped to that route. The neighbouring `everyone` case uses `/Admin/Roles`, an unmapped route that is empty for scope reasons, not permission reasons. If CreateRole's visibility, route key or name changed, the developer assertion would keep passing without testing permissions at all.
- Suggestion: Assert `everyone` on `/Admin/Roles/New` returns `["Role.CreateRole"]` next to the developer case. Optionally add a second denial with `Principal(PermissionIds.DeveloperAccess)` on `/Settings`, which lacks `CredentialManageSelf`, and expect empty.
- Status: open

### Issue 4 — Severity: suggestion
- File: tests/container-apps/web/web-spa-integration-tests/features/application/catalog-agent-tests.cs:253
- Description: The WebMCP list is not tested with a denying principal. The task requires WebMCP registration to be "permission-filtered". The only non-`everyone` call is `DeveloperAccess` on `/StyleGuide`, a route that has no catalog tools for anyone, so the result `[page_context]` says nothing about permissions. Coverage today depends on `DescribeAsync` delegating to `SelectAsync`.
- Suggestion: Call `DescribeAsync` with `Principal(PermissionIds.DeveloperAccess)` on `/Settings` and assert `[PageAgentContext.ToolName]` only. Also call it with a non-`DeveloperAccess` principal on `/Counter` and assert `IncrementCounter` is absent.
- Status: open

### Issue 5 — Severity: nit
- File: tests/container-apps/web/web-spa-integration-tests/features/application/catalog-agent-tests.cs:152
- Description: The fake-client end-to-end test builds its `CatalogAgentTool` by hand with `RequiresApproval: true`. It does not get the tool from `CatalogAgentToolSet.SelectAsync`. That leaves the production composition (select, then `CatalogAgentFunctions.Create`, then session) untested as one piece, and the approval flag is asserted in a different test.
- Suggestion: Build the tools from `SelectAsync(everyone, …, "/Counter", …)`, then `CatalogAgentFunctions.Create(tools)`.
- Status: open

### Issue 6 — Severity: nit
- File: tests/container-apps/web/web-spa-integration-tests/features/application/catalog-agent-tests.cs:214
- Description: `Host_Has_No_Chat_Client` checks the test host's own registrations, not the product's. `AspireSpaTestApplication` never registers `IChatClient`, and nothing registers the `IWebMcpModelContext` interface (the product registers the concrete `JsWebMcpModelContext`). So both assertions are true by construction. The real no-model check is `ConfigureServices_Does_Not_Register_A_Chat_Client`.
- Suggestion: Drop the test, or replace it with a check of observable no-model behavior. For example, with no client registered the palette roster or Ask UI gets no Ask entry.
- Status: open

### Issue 7 — Severity: nit
- File: tests/container-apps/web/web-spa-integration-tests/features/application/catalog-agent-tests.cs:299
- Description: `await invoke` has no timeout. If `ResolveApproval` stopped completing `WebMcpApprovalGate`, the test would hang until the session or CI timeout and never fail with a clear assertion. The polling loop before it is bounded at 5s.
- Suggestion: Use `await invoke.WaitAsync(TimeSpan.FromSeconds(5))`.
- Status: open

### Issue 8 — Severity: nit
- File: tests/container-apps/web/web-spa-integration-tests/features/application/catalog-agent-tests.cs:141
- Description: The loop that asserts each `/Settings` tool has `ActionVisibility.Agent` repeats the filter `SelectAsync` already applies, so it adds no coverage beyond the exact-list assertion at line 99.
- Suggestion: Remove it, or replace it with an explicit `ShouldNotContain` for `Credentials.AddPasskey`, `Credentials.AddExistingPasskey` and `Credentials.LinkMicrosoft365`, which states the Human-only intent.
- Status: open

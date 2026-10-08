# Round 2 — general
**Date:** 2026-10-09
**Scope reviewed:** fix commit 3330690db (`git diff d2f3a442c..3330690db`), re-read in full:
web-spa `features/application/agent/` (web-mcp-approval-gate, web-mcp-dispatcher,
catalog-agent-{approval,arguments,functions,schema,session,tool-set}, page-agent-scope),
`features/application/agent-surface-state/*`, `components/WebMcpAgentSurface.razor`,
`components/page-agent-context.cs`, `program.cs` registrations, `skills/tw-blazor/SKILL.md`,
task `design.md`, and `tests/.../features/application/catalog-agent-tests.cs`. General and security
lenses. Not run: build, tests, AppHost.

## Prior findings

| ID | Round-1 status | Verified |
|----|----------------|----------|
| M1 | fixed | fixed-verified. `WebMcpApprovalGate` is one slot keyed by a fresh `Guid`; `TryBegin` is synchronous (no await between check and claim), so a second call is refused with `BusyError`; `Complete(id, …)` is a no-op for a stale id; banner and `ResolveApproval` carry the rendered id. TCS uses `RunContinuationsAsynchronously`. Test covers busy + stale id. |
| M2 | fixed | fixed-verified. `LocationChanged` is subscribed before `ShowApproval` and removed in `finally` on every path (approve, reject, cancel, throw); `finally` also frees the slot and clears the banner by id. After the wait the dispatcher refuses on path change and re-selects. Focused pages (Login, ChooseMicrosoft365) have no `PageAgentScope` entry, so no mutating tool can start there; a call started elsewhere is cancelled by the hop. See N1 for a residual window. |
| M3 | fixed | fixed-verified. `InvokeCoreAsync` re-reads auth, calls `CatalogAgentToolSet.FindOfferedAsync` for the current route, returns a failed result if gone, and executes the re-selected entry. Test navigates away after selection and asserts no execution. |
| M4 | fixed | fixed-verified. Binder options copy `ContractSerializationDefaults.Options` (string enums, integers refused) plus case-insensitive reads; schema emits `enum` for top-level and nested enum members. Test binds `"Required"`, rejects `1`, and checks the enum list. |
| M5 | fixed | fixed-verified. `page_context` on /Profile returns all six `UpdateProfile.Command` fields (the contract has no Version); /Admin/Authentication returns the four fields including `version`. Test present. |
| M6 | fixed | fixed-verified. Bind and Render happen before the gate; `ArgumentException`/`JsonException`/`NotSupportedException`/`InvalidOperationException` map to `{action, error}` with no prompt; the banner shows `Render(bound)`; `Execute` receives the same `bound` array. Test asserts the unknown `extra` key is absent from the banner. |
| M7 | fixed | fixed-verified. The approval loop moved to the test file over `CatalogAgentSession.CreateInvokingClient`; the dead guard is gone; tools come from `SelectAsync` + `CatalogAgentFunctions.Create`. Design region matches. |
| M8 | fixed | fixed-verified. Limit stated in `WebMcpAgentSurface` Design and as a design.md follow-up. |
| M9 | fixed | fixed-verified. `WebMcp_Invoke_Refuses_Off_Page_Human_Only_Unknown_And_Unbindable_Calls`. |
| M10 | fixed | fixed-verified. `WebMcp_Invoke_Reject_Leaves_Counter_Unchanged`. |
| M11 | fixed | fixed-verified. Positive control with the all-permissions principal on /Admin/Roles/New. |
| M12 | fixed | fixed-verified. Denying principals on /Admin/Roles/New and /Profile publish only `page_context`. |
| M13 | wontfix | wontfix-accepted. Rationale recorded in the `WebMcpAgentSurface` Design comment; the replace is idempotent. |
| M14 | fixed | fixed-verified. Explicit allow-list (`Credentials.FetchCredentials` only); everything else requires approval. |
| M15 | fixed | fixed-verified. Tautological test removed; `ConfigureServices_Does_Not_Register_A_Chat_Client` exercises real registration. |
| M16 | fixed | fixed-verified. Every invoke/run await is bounded by `WaitAsync(Timeout)` and the pending poll has its own timeout; /Settings asserts the Human-only actions are absent. |
| M17 | fixed | fixed-verified. Concurrency (`WebMcp_Second_Call_While_Pending_…`) and navigation-cancel (`WebMcp_Navigation_Cancels_…`) regression tests. |

## Summary

All seventeen round-1 findings hold against the code; none reopened. The gate/dispatcher ordering
is sound under WASM async interleaving: the busy check and claim are one synchronous step, the
navigation handler is attached before the banner is shown and detached on every path, and the
banner resolves by id, so a click can only release the call it displayed. Purpose/Design regions
on every touched file match the new behavior; components only dispatch (TWA0026); file names are
kebab-case. Three new low-severity items below: one post-approval race window, one misreported
outcome, and one wording slip in a shipped skill.

## Issues

### N1
- **Severity:** suggestion
- **File:** source/container-apps/web/projects/web-spa/features/application/agent/web-mcp-dispatcher.cs (InvokeTool, post-approval block)
- **Description:** After approval the path check runs first, then `await FindOfferedAsync(path, name)` (auth state + `IAuthorizationService` awaits), then `Execute`. The re-selection uses the captured call-time `path`, not the current one. A navigation during that await is not seen (the `LocationChanged` handler was already detached in `finally`), so the approved action can still execute after the person left the page. The window is small (usually completes synchronously in WASM), but the Design region claims the dispatcher refuses unless the path is unchanged.
- **Suggestion:** Re-check `PageAgentScope.FromNavigation(Navigation)` against `path` after the re-selection (immediately before `Execute`), or re-select with the current path and compare it to `path`.
- **Status:** open

### N2
- **Severity:** nit
- **File:** source/container-apps/web/projects/web-spa/features/application/agent/web-mcp-dispatcher.cs (WaitForApprovalAsync / InvokeTool)
- **Description:** Any `LocationChanged` cancels the wait, including a query/hash-only change on the same route (`/Counter` → `/Counter?x=1`) or A→B→A before the dispatcher resumes. The normalized path then still matches, so the agent gets `{"approved":false}`, which reads as the person rejecting the call when nobody did.
- **Suggestion:** Record that cancellation came from navigation (e.g. a local flag set in `Cancel`) and return `PageChangedError` in that case regardless of the final path.
- **Status:** open

### N3
- **Severity:** nit
- **File:** skills/tw-blazor/SKILL.md (agent bullet on `PageAgentContext`)
- **Description:** "the current record (with its Version token) for pages whose command replaces a whole record". `UpdateProfile` replaces a whole record but has no Version token; only site settings carry one. This skill ships in generated apps.
- **Suggestion:** "the current record (and, for site settings, its Version token)".
- **Status:** open

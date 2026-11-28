# Emit development browser diagnostic logs for Ask AI tool invocations and FormPrefillScope outcomes

## Description

When the app runs in Interactive WebAssembly, Ask AI's tool loop (`AgentAsk`, `CatalogAgentFunctions`) and form prefill application (`FormPrefillScope`) execute in the browser. Today `browser-log-forwarder.ts` only forwards `console.error`, `console.warn`, `window.error`, and `unhandledrejection` to `POST /api/browser-logs` (category `Web.Spa.Browser`).

When a tool call executes without throwing — for example, `Prefill.Role.CreateRole` returning an off-page navigate offer, returning `ApprovalRequiredError`, failing `FormPrefill.TryNormalize`, or `FormPrefillScope` skipping/applying a draft — there is zero trace in Aspire Dashboard or SigNoz showing what happened in the browser.

Add a narrow, Development-only diagnostic breadcrumb path through the existing `POST /api/browser-logs` pipeline (which already supports `info`/`debug` levels and is fail-closed 404 outside Development/Testing) so Ask AI tool execution and `FormPrefillScope` lifecycle decisions appear in structured server logs (and therefore in both Aspire Dashboard and SigNoz).

## Propagation

likely

## Parent

536

## Requirements

1. **Narrow dev-only browser diagnostic hook (`browser-log-forwarder.ts` + C# helper):**
   - Do **not** blanket-forward all `console.log`/`console.info` calls (to avoid third-party noise and rate-limit exhaustion).
   - Expose a small explicit helper on the browser-log forwarder (for example `window.__twBrowserDiag(level, message)` or a small ES module helper that posts through the same `browser-log-forwarder` queue/batching/404-disable/429-backoff pipeline with `level: "info"` and `source: "console"`).
   - Add a small C# helper/service in `web-spa` (e.g., `AskDiagnostics`) that emits structured single-line diagnostic breadcrumbs **only when `IWebAssemblyHostEnvironment.IsDevelopment()` (or `IHostEnvironment.IsDevelopment()` on Server)** and is a complete no-op outside Development (no JS interop, no network traffic, never throws if JS is unavailable or during prerender).
   - Never log secrets, tokens, or raw user-entered form field values — log only structural metadata (route, tool name, field names/counts, state flags, error reason).

2. **Instrument the three Ask AI / prefill browser decision points:**
   - **`AgentAsk.razor` (`EnsureAgentAsync` / `OnStatusChanged`):**
     - Log when an agent is built/rebuilt (`path`, `mode`, `generation`, tool count, list of `Prefill.*` tool names offered) and when a rebuild is deferred (`reason`: `TurnInProgress`, `Building`, `Readiness`).
     - Log `ConversationStatus` transitions (`Streaming`, `AwaitingInput`, `Idle`, `Error`).
   - **`CatalogAgentFunctions.cs` (`CatalogAgentFunction.InvokeCoreAsync`):**
     - Log tool invocation start and outcome: `toolName`, `route`, `wrapped`, `currentFound`, `requiresApproval`, `offPageRoute`, `prefill`, `completed`, `error`, and (for prefill) drafted `fields` count/names.
   - **`FormPrefillScope.razor` (`OnAfterRenderAsync` / `OnParametersSet`):**
     - Log when a waiting draft is seen in `FormSuggestionState`: `form`, `draftId`, `ready`, `isInteractive`, `applying`, `alreadyHandled`, `isDirty`, `accepted`, and outcome (`applied` with field names, `confirming`, `declined`, `cleared`).

3. **Tests:**
   - Add unit/integration tests covering:
     - Diagnostic message formatting (includes expected structured fields and excludes raw field values/secrets).
     - Production/non-Development no-op behavior (never invokes JS or throws).
     - `browser-log-forwarder` forwarding explicit diagnostic entries with `level: "info"` while still NOT forwarding arbitrary `console.log`/`console.info` calls.
   - Rebuild TS assets (`browser-log-forwarder.js`) if `browser-log-forwarder.ts` changes, and verify `./bin/dev build` and targeted `./bin/dev test` pass with 0 warnings and 0 errors.

## Notes

- Key files:
  - `source/container-apps/web/projects/web-spa/source/browser-log-forwarder.ts`
  - `source/container-apps/web/features/browser-logs/forward-browser-logs/forward-browser-logs-contracts.cs` (`AllowedLevels` already includes `"info"` and `"debug"`)
  - `source/container-apps/web/projects/web-spa/features/application/modals/agent-ask/AgentAsk.razor`
  - `source/container-apps/web/projects/web-spa/features/application/agent/catalog-agent-functions.cs`
  - `source/container-apps/web/projects/web-spa/features/application/components/FormPrefillScope.razor`
- Respect `TWA0026` / `DirectComponentSideEffect` conventions if invoking JS interop from a `.razor` component, and keep `FormPrefillScope` / `AgentAsk` clean of inline side-effect violations.

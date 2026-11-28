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

## Goal (implementer restatement)

Ask AI's tool loop and form prefill run in the browser, and a tool that returns without throwing leaves no line in the Aspire dashboard or SigNoz. I will add a Development-only breadcrumb that posts one structured line through the existing `POST /api/browser-logs` queue, and I will leave ordinary `console.log` and `console.info` unforwarded.

The browser script grows one explicit hook, `window.__twBrowserDiag`, that uses the same batching, 404-disable, and 429-backoff path with `source: "console"`. A small `AskDiagnostics` service in web-spa writes those lines only when the host environment is Development. Outside Development it does not call JavaScript and does not throw, including when JavaScript is missing or the page is still prerendering. The lines carry route, tool name, field names and counts, and state flags. They do not carry tokens, secrets, or the values a person typed.

The three call sites are the agent build and status changes in `AgentAsk`, each tool return in `CatalogAgentFunction.InvokeCoreAsync`, and the draft decision in `FormPrefillScope`. Tests check the line shape, the production no-op, and that the forwarder posts an explicit info diagnostic while still ignoring plain `console.log` and `console.info`.

Round 1 left six gaps. A waiting draft is reported even when this form already applied that id, so `alreadyHandled` can be true, and the seen-key still drops a repeat of the same flags. Error codes read the same constants the tool and the prefill normalizer write, including the not-available sentence. The report helpers return before a snapshot or a key when diagnostics are off. A tool body that throws still writes `tool-outcome` with the exception type name, then rethrows. The browser script does not declare `Window` again. A wiring run records the `ask-diag` lines, and those lines do not contain typed values.

## Review

- Round 1: fix pass implemented on this id. Finding statuses stay open in `review/round-1/merged.md` for review round 2.
- Round 2: Done
## Notes

- Key files:
  - `source/container-apps/web/projects/web-spa/source/browser-log-forwarder.ts`
  - `source/container-apps/web/features/browser-logs/forward-browser-logs/forward-browser-logs-contracts.cs` (`AllowedLevels` already includes `"info"` and `"debug"`)
  - `source/container-apps/web/projects/web-spa/features/application/modals/agent-ask/AgentAsk.razor`
  - `source/container-apps/web/projects/web-spa/features/application/agent/catalog-agent-functions.cs`
  - `source/container-apps/web/projects/web-spa/features/application/components/FormPrefillScope.razor`
- Respect `TWA0026` / `DirectComponentSideEffect` conventions if invoking JS interop from a `.razor` component, and keep `FormPrefillScope` / `AgentAsk` clean of inline side-effect violations.

## Session

AskDiagnostics posts one `ask-diag` line through `window.__twBrowserDiag` only when the host environment name is `Development`. The browser forwarder still wraps only `console.error` and `console.warn`. Report helpers on AgentAsk and FormPrefillScope carry `DirectComponentSideEffect` so the lifecycle methods stay free of the side-effect analyzer.
- Implement oracle: implemented by implementer-grok (grok, vendor xai, model grok-4.7, response model grok-4.7-build), session 01a2200f-3451-7e92-bbac-f55bb0e99130, max-turns 500, Done — 2026-11-28T03:40:12Z
- Evidence send-back: captured AgentAsk on `/` and the new-role FormPrefillScope on `/Admin/Roles/New` from this worktree's isolated Development app, signed in as the first passkey account (Member).
- Implement oracle: implemented by implementer-grok (grok, vendor xai, model grok-4.7, response model grok-4.7-build), session 01a2203d-3add-7633-8db3-775eaf855ff6, max-turns 500, Done — 2026-11-28T03:53:29Z
- Review oracle round 1: general reviewer (claude, model claude-opus-5-5), effort 3, turn cap 200, 2026-11-28. Six findings open (1 bug, 3 suggestion, 2 nit); fix pass requested. Files: `review/review-framework.md`, `review/round-1/general.md`, `review/round-1/merged.md`.
- Review oracle: review by reviewer-claude (claude, vendor anthropic, model claude-opus-5-5, response model claude-opus-5-5), session 050dc31f-e77d-4ffb-b16e-b49a1feab354, max-turns 200, artifacts general.md, merged.md — 2026-11-28T03:57:09Z
- Implement-fix-2 oracle: implemented the six open round-1 findings on this id (alreadyHandled seen lines, shared error-code constants, disabled diagnostics return before snapshot or key work, recorded wiring lines, tool-outcome on a thrown tool body, one Window type). Statuses in the round-1 ledger stay open for round 2. Session 01a2204c-be43-7582-90c7-db4cf2064a03 — 2026-11-28.
- Review oracle round 2: general reviewer (claude, model claude-opus-5-5), effort 3, turn cap 200, 2026-11-28. Checked the fix delta f04ccf592. All six round-1 findings are fixed and there are no new findings. Disposition is `clean`. Files: `review/round-2/general.md`, `review/round-2/merged.md`, `review/disposition.md`.

## Results

Development breadcrumbs go out as `level: info` and `source: console` on the existing `POST /api/browser-logs` queue. A non-Development host, including Testing, does not call JavaScript. Lines name routes, tools, field names, and flags. They do not carry tokens or the values a person typed.

The fix pass reports a waiting draft even after that draft was already applied, so one `prefill-seen` line has `alreadyHandled=true` and the apply does not run again. Error codes compare the shared `FormPrefill` and `WebMcpDispatcher` strings. Report helpers return before snapshot or key work when diagnostics are disabled. A tool body that throws writes `tool-outcome` with `completed=false` and `error` set to the exception type name, then rethrows. The browser script uses the `Window` type in `source/types/global.d.ts`.

Screenshots are from this worktree's isolated Development app (`https://127.0.0.1:14900` and `https://localhost:14900`). Mock authentication stays off. The signed-in shots use a passkey account shown as Member.

- [evidence/home.png](evidence/home.png) route `/` principal signed-out look at the Ask Akili panel open on the home shell
- [evidence/admin-roles-new.png](evidence/admin-roles-new.png) route `/Admin/Roles/New` principal signed-out look at the passkey sign-in gate in front of the new-role form
- [evidence/agent-ask.png](evidence/agent-ask.png) route `/` principal Member look at the Ask Akili panel open on the home shell
- [evidence/form-prefill-scope.png](evidence/form-prefill-scope.png) route `/Admin/Roles/New` principal Member look at the new-role form Name and Description fields inside FormPrefillScope
- [evidence/ask-diag.txt](evidence/ask-diag.txt) route `/Admin/Roles/New` principal in-proc test user look at tool-start, tool-outcome, prefill-seen alreadyHandled, and agent-deferred lines with no typed values

### How to validate

Smoke: Run the app in Development, sign in, open Ask on `/Admin/Roles/New`, and invoke `Prefill.Role.CreateRole` with a name and a description. In the Aspire dashboard or SigNoz, filter category `Web.Spa.Browser` for `ask-diag`. The wiring lines already captured from the in-proc tests are in `evidence/ask-diag.txt`.

Expect: An `event=tool-start` line and an `event=tool-outcome` line name the tool and the field names, and neither line contains the typed name or description. An `event=prefill-seen` line appears while the draft is waiting, then `event=prefill-outcome` with `outcome=applied`. Putting that same draft back writes `alreadyHandled=true` and does not apply a second time. A thrown tool body writes `tool-outcome` with `completed=false` and `error=ArgumentException`. A Production or Testing host posts none of these lines. A plain `console.log` call does not show up under `Web.Spa.Browser`.

Gates:

- `./bin/dev build` — passed, 0 warnings, 0 errors
- `./bin/dev test tests/container-apps/web/web-spa-integration-tests/features/application/ask-diagnostics-wiring-tests.cs` — passed, 9/9. The file is one half of `AskDiagnostics_Should_`. Not a standalone runfile (no `#if !JARIBU_MULTI`); CI runs the integration project, and this command runs the whole class.
- `./bin/dev test tests/container-apps/web/web-spa-integration-tests/features/application/ask-diagnostics-tests.cs` — passed, 9/9. Same class, same note: not a standalone runfile.
- `ganda repo audit` — passed, 43 passed, 0 failed, 1 warning (`vscode-window-icon`, pre-existing, non-blocking)

### Review

Round 1 (general, effort 3) raised 6 findings: 1 bug, 3 suggestion, and 2 nit. The implement-fix-2 pass (f04ccf592) addressed M1 through M6.

Round 2 (general, effort 3) checked the fix delta. Final counts: bug 0 open, 1 fixed. Suggestion 0 open, 3 fixed. Nit 0 open, 2 fixed. Nothing wontfix, and no new findings. The reviewer did not re-run the gates and used the record above.

**Review disposition: clean** (2 rounds, roster general, effort 3, final open count 0, no exceptions or escalations).

- Framework: `review/review-framework.md`
- Last merged ledger: `review/round-2/merged.md`
- Disposition: `review/disposition.md`

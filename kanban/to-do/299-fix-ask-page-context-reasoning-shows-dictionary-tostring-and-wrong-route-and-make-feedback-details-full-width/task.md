# Task 299: Fix Ask page_context reasoning shows Dictionary ToString and wrong route, and make Feedback Details full width

## Description

Two bugs Steven found in the web app on the Feedback page (app version 2.0.0.0, WebAssembly
render mode), reported 2026-10-10 with screenshots.

### Bug 1: Ask AI page context is broken

On `/Feedback`, open the Ask side panel and send "Explain this page". The "See reasoning" block shows:

```
page_context
System.Collections.Generic.Dictionary`2[System....
```

and the model answers: "This is the site root (`/`). The page context doesn't include any item
ids, flags, or other content details, so there's nothing further on the page to describe..."

So (a) a `Dictionary<string, object?>` is being `ToString()`'d instead of serialized as JSON, and
(b) the model gets the wrong page: it believes it is on `/` instead of `/Feedback`, with no page state.

Starting points found while filing (verify; do not assume these are the only causes):

- `source/container-apps/web/projects/web-spa/features/application/modals/agent-ask/AgentAsk.razor`
  (~line 437-441): the reasoning block renders `<pre>@call.Arguments</pre>` for a
  `FunctionInvocationContentBlock`. `Arguments` is a dictionary (built in
  `relay-chat-client.cs` ~line 151-154 by deserializing `ArgumentsJson` into
  `Dictionary<string, object?>`), so Razor prints the type name. The approval block
  (`<pre data-qa="AgentApprovalArguments">@approval.Arguments</pre>`) likely has the same problem.
- `PageContextFunction.InvokeCoreAsync` in
  `projects/web-spa/features/application/agent/catalog-agent-functions.cs` computes the route
  with `PageAgentScope.FromNavigation(navigation)` from the function's service provider. Check
  whether that `NavigationManager` (scope/service provider used by the Ask panel's function
  invocation) reports the real current URI on WASM, or `/` (e.g. an uninitialized or different
  scope). `AgentAsk.razor` line ~81 uses `CurrentPath()` for the @ menu; the tool should agree with it.
- `PageAgentContext.Describe` (`projects/web-spa/components/page-agent-context.cs`) returns only
  `{"path": ...}` for routes other than Settings/Passkeys/Profile/Admin/Authentication. `/Feedback`
  (and `/Feedback/{id}`) has catalog tools in `PageAgentScope` but no page facts. Add relevant
  Feedback page state (e.g. page title/purpose, the draft kind/title presence, the user's
  filings list ids/titles if loaded, the open feedback id on `/Feedback/{id}`) so "Explain this page"
  can answer usefully. Keep it read-only and do not leak another user's data.
- `relay-chat-client.cs` `ToTurn` already serializes non-string tool results with
  `JsonSerializer.Serialize`; confirm the tool result that reaches the model is the JSON document
  and not a `ToString()` anywhere else on the path (client relay, server `xai-chat-upstream-server.cs`,
  agent-chat endpoint).

### Bug 2: Feedback Details textarea is narrow

On the Feedback form, Kind and Title stretch the full width of the form, but the Details
`FluentTextArea` (`projects/web-spa/features/feedback/pages/FeedbackListPage.razor` ~line 298-300,
wrapped in `<div class="feedback-details">`) renders as a narrow box. Make it full width like Kind
and Title, with a sensible minimum height (it currently sets `Height="9rem"`), and keep it
resizable vertically. Follow the tw-blazor-css-strategy conventions (scoped `.razor.css`,
`::deep` where styling the Fluent component's inner element, no inline style blocks, no new
global CSS unless the strategy says so). The paste/upload listener on the details host (task 295)
must keep working.

## Requirements

1. The reasoning block shows tool call arguments as readable JSON (or nothing when empty), never
   a CLR type name. Same for the approval arguments block.
2. Decide whether raw `page_context` output/arguments should be shown in the visible reasoning at
   all; record the decision and reason in Results (e.g. show the tool name plus pretty JSON
   arguments, collapsed; or hide the empty `{}` arguments for page_context).
3. Asking "Explain this page" on `/Feedback` gives the model `{"path":"/Feedback", ...}` with
   relevant Feedback page facts, and the answer describes the Feedback page, not the site root.
   `/Feedback/{id}` includes the open feedback id.
4. Regression tests:
   - a test that fails if tool arguments or the page_context result are rendered/sent via
     `ToString()` (e.g. assert the rendered reasoning/approval text and the relayed `ToolResult`
     do not contain `System.Collections.Generic` and do parse as JSON);
   - a test that page_context on `/Feedback` reports `/Feedback` (with the WASM/Ask panel service
     scope used in practice, not only a hand-built NavigationManager) and includes the new facts.
   Prefer extending the existing suites (`web-spa-integration-tests/.../catalog-agent-*-tests.cs`,
   `web-spa-playwright-tests/ask-surface-playwright-tests.cs`).
5. Feedback Details textarea is full width of the form with a sensible min height; add or extend a
   Playwright check that its width matches the Title field (within a few px).
6. Full build and full test suite (unit, integration, Playwright/WASM) green locally and in CI.

## Checklist

- [ ] Reproduce bug 1 and bug 2 (note how in Implementation Notes)
- [ ] Fix reasoning/approval argument rendering (JSON, no ToString)
- [ ] Decide and implement what the visible reasoning shows for page_context
- [ ] Fix page_context route on the Ask panel path (reports /Feedback, not /)
- [ ] Add Feedback page facts to PageAgentContext
- [ ] Regression tests for ToString and route
- [ ] Make Details textarea full width with min height (tw-blazor-css-strategy)
- [ ] Playwright width check for Details vs Title
- [ ] Full build and tests green

## Session

- Created: 70603 (2026-10-10)

## Notes

- Filed from Steven's screenshots (Feedback page, Ask panel, version 2.0.0.0 WebAssembly).
- Related: task 295 (feedback image paste/upload on the Details field) touched the same
  Details host; task 282 (page-scoped tools) and 271/292 (Ask side panel).

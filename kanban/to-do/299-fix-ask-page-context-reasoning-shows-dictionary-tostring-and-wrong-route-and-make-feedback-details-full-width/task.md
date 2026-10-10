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

- [x] Reproduce bug 1 and bug 2 (note how in Implementation Notes)
- [x] Fix reasoning/approval argument rendering (JSON, no ToString)
- [x] Decide and implement what the visible reasoning shows for page_context
- [x] Fix page_context route on the Ask panel path (reports /Feedback, not /)
- [x] Add Feedback page facts to PageAgentContext
- [x] Regression tests for ToString and route
- [x] Make Details textarea full width with min height (tw-blazor-css-strategy)
- [x] Playwright width check for Details vs Title
- [x] Full build and tests green

## Session

- Created: 70603 (2026-10-10)
- Implementer: grok session 01a12619-5367-7331-b76e-16240b28b34b (2026-10-10)
- Review oracle: Claude Code (claude-opus-5-5), effort 3, general reviewer (2026-10-10)

## Notes

- Filed from Steven's screenshots (Feedback page, Ask panel, version 2.0.0.0 WebAssembly).
- Related: task 295 (feedback image paste/upload on the Details field) touched the same
  Details host; task 282 (page-scoped tools) and 271/292 (Ask side panel).

## Implementation Notes

### Bug 1, reproduced

On `/Feedback` the Ask panel renders a `FunctionInvocationContentBlock` with `<pre>@call.Arguments</pre>`. `Arguments` is a `Dictionary<string, object?>` built by deserializing `ArgumentsJson`, so Razor prints `System.Collections.Generic.Dictionary`2[...]`. `page_context` has an empty input schema, so that dictionary is `{}` and the type name is the whole visible argument. The approval `<pre>` did the same.

The model still received a string tool result (`RelayChatClient.ToTurn` already JSON-serializes non-string results). The wrong page came from the route. `PageContextFunction` read `NavigationManager` from the function's service provider. `FunctionInvokingChatClient` does not create a scope; it uses the provider passed into `CatalogAgentSession.CreateInvokingClient`. On WebAssembly that manager can stay at the base URI while the document is `/Feedback`, so `PageAgentScope.FromNavigation` returned `/`. `PageAgentContext.Describe` then returned only `{"path":"/"}` for Feedback.

The stuck-manager integration test builds that provider: it returns a fresh `NavigationManager` parked at `/` and delegates every other service, including `PageAgentRoute` and `IStore`, to the shell scope. Before `Observe`, `page_context` is `/`. After the shell navigates to `/Feedback` and `Observe` runs, the same stuck provider returns `/Feedback` and the Feedback facts.

The Playwright trip signs in, asks on Settings, then does a full `Goto` of `/Feedback` (a second ask in the same conversation would echo the previous tool result) and sends "Explain this page". The fake upstream answers with the relayed tool result. The answer contains `/Feedback` and `"page":"Feedback"`, and the opened reasoning block does not contain `System.Collections.Generic` or an arguments `<pre>` (the call arguments are empty).

### Bug 2, reproduced

Kind and Title stretch the form. Details sits in `<div class="feedback-details">` so the paste listener from task 295 has a host. `FormField` only stretches a direct child, and FluentUI 5 renders `fluent-field` around `fluent-textarea`. A direct-child `::deep > fluent-textarea` never matched. In the Playwright page the title and the `fluent-field` were 934px wide and the textarea was 288px (difference 646). Setting `display: block`, `width: 100%`, `max-width: 100%`, and `min-width: 0` on the textarea itself made its box 934px. `grid-column` and `display` on the field were not required.

### What changed

- `AgentArgumentText.Format` copies a non-empty argument dictionary and pretty-prints JSON. Null or empty returns null, and both the reasoning and approval blocks omit the `<pre>`.
- Visible reasoning shows the tool name. It shows pretty JSON arguments only when the dictionary is non-empty. It does not show the tool result. `page_context` arguments are always `{}`, so they are hidden. The result stays in the model transcript: on other pages it includes credential rows, and copying it into the panel would put that next to the chat.
- `PageAgentRoute` is scoped (one path per circuit, not a process singleton). The shell observes it. On an in-process runtime, `Observe` prefers `window.timeWarpPagePath` (pathname, search, hash), defined in `App.razor` before `blazor.web.js`. Server circuits are not in-process, so their `NavigationManager` stays the source. Callers with no observation keep `PageAgentScope.FromNavigation`.
- `/Feedback` facts: `page`, `purpose`, `filingsLoaded`, `filingCount`, up to 20 filings (`id`, `title`, `kind`), `emailCopyAvailable`, `draftAttachmentCount`, and `draft` (`kind`, `hasTitle`, `hasBody`) only while the composer is on screen. `/Feedback/{id}` adds `openFeedbackId`, and `openItem` (`id`, `title`, `kind`) when `FeedbackState.Current` is that id. No body text, no other principal's rows. The composer flags are a `NoteComposer` action the page dispatches when kind or presence changes, and clears on dispose. It is not a catalog tool.
- Details: scoped `.razor.css` reaches `fluent-textarea` and `fluent-text-area` as descendants, `display: block`, `width: 100%`, `min-height: 9rem`. The component sets `Width="100%"`, `Height="9rem"`, `Resize="TextAreaResize.Vertical"`. The paste host `@ref="DetailsHost"` is unchanged.

## Results

Ask on `/Feedback` no longer prints a dictionary type name, and `page_context` reports `/Feedback` with the filer's own page facts. The Details field is the width of Title.

### Decision

Show the tool name in the collapsed "See reasoning" block. Pretty-print JSON arguments when the dictionary has entries. Hide null, empty, and `{}` arguments for every tool, including `page_context` (its schema is empty, so `{}` was the `ToString` symptom and is noise). Do not copy the `page_context` result into the visible reasoning panel. The model still receives that JSON through the relay. The result can include credential rows on Settings and Passkeys, and the answer already quotes what the model used.

### Files

- `agent-argument-text.cs`, `AgentAsk.razor` (reasoning and approval).
- `page-agent-route.cs`, `App.razor` (`timeWarpPagePath`), `program.cs`, `catalog-agent-functions.cs`, `web-mcp-dispatcher.cs`, `web-mcp-publisher.cs`, `WebMcpAgentSurface.razor`.
- `page-agent-context.cs`, `feedback-state.cs`, `feedback-state.list-my-feedback.cs`, `feedback-state.note-composer.cs`, `FeedbackListPage.razor`, `FeedbackListPage.razor.css`.
- Tests: `catalog-agent-tests.cs` (`Arguments_Render_As_Json_And_Empty_Payloads_Are_Omitted`, `Relay_Sends_Tool_Arguments_And_Page_Context_As_Json`, `Page_Context_On_Feedback_Uses_The_Ask_Scope_Not_A_Stuck_Manager`), `catalog-agent-parity-tests.cs`, `agent-ask-readiness-tests.cs` (the panel now injects `PageAgentRoute`), `aspire-spa-test-application.cs`, `feedback-details-layout-tests.cs`, `ask-sign-in-playwright-tests.cs`, `feedback-attachment-playwright-tests.cs`.

### Tests

- `./bin/dev build`: succeeded, 0 Warning(s), 0 Error(s), 00:00:05.12.
- `./bin/dev test`: `Tests completed successfully!`, exit 0. Previously red projects in that run: `aspire-tests` 38 passed, `web-spa-integration-tests` 183 passed, `web-spa-playwright-tests` 6 passed. Every other project in the same run passed with 0 failures.
- After the review fixes: `./bin/dev build` 0 Warning(s), 0 Error(s). `web-spa-integration-tests` 184 passed, 0 failed. `web-spa-playwright-tests` 6 passed, 0 failed.
- `ganda repo audit`: Repository passes, exit 0, 1 advisory (non-blocking) warning. `memsearch-scaffold` reports `git config core.hooksPath=.githooks`. That hook path was already set and was not changed.

### Review

- Rounds: 2. Effort 3, roster `general`.
- Final counts: bug 0, suggestion 2 fixed, nit 0. Open 0, wontfix 0.
- Disposition: `clean`.
- M1 (suggestion, fixed): an observed `PageAgentRoute` always beat the live `NavigationManager`, so it could go stale after a focused-page hop or on InteractiveServer. That weakened the WebMCP dispatcher's re-selection. `PathOr` now re-reads `timeWarpPagePath` when `Observe` found it. It returns the live path when the caller holds the observed manager and keeps the stored path only for a different (stuck) manager. `Current` delegates to `PathOr`. Test: `Route_Follows_The_Observed_Manager_And_Keeps_The_Path_For_A_Stuck_One` in `catalog-agent-tests.cs`.
- M2 (suggestion, fixed): `timeWarpPagePath` returned the absolute `location.pathname`. It now removes the `document.baseURI` path, so it matches `ToBaseRelativePath` under a non-root base href.
- Artifacts: `review/review-framework.md`, `review/round-1/`, `review/round-2/merged.md`, `review/disposition.md`.

### How to validate

**Smoke**

```bash
cd tests/container-apps/web/web-spa-integration-tests
dotnet test -c Release -- --filter-class FeedbackDetails_
```

Signed in, WebAssembly, open `/Feedback`:

1. Kind, Title, and Details are the same width. Details is about 9rem tall and can be dragged taller. Pasting an image into Details still attaches it.
2. Open Ask and send `Explain this page`. Open "See reasoning".

**Expect**

- The layout command prints `Test run summary: Passed!` and `failed: 0`. It checks the paste host, `Width="100%"`, `Height="9rem"`, `Resize="TextAreaResize.Vertical"`, and a descendant `::deep fluent-textarea` rule (not a direct-child combinator).
- Details and Title differ by less than 8px. The Playwright check is `PasteAndUpload_Attaches_The_File_And_Keeps_The_Text` in `feedback-attachment-playwright-tests.cs`.
- "See reasoning" contains `page_context` and does not contain `System.Collections.Generic`. There is no arguments block for that call (`data-qa="AskReasoningArguments"` is absent). A tool that has arguments shows indented JSON in that block, and the approval block does the same.
- The answer describes the Feedback page. With the fake upstream it contains `/Feedback` and `"page":"Feedback"`. It does not say the page is the site root.
- On `/Feedback/{id}` the tool result includes `openFeedbackId` for that id. The integration proof is `Page_Context_On_Feedback_Uses_The_Ask_Scope_Not_A_Stuck_Manager`.

**Automated gate**

```bash
./bin/dev build
./bin/dev test
```

Expect build 0 warnings and 0 errors, and `Tests completed successfully!`.

Targeted regression (Docker; the catalog class boots Aspire):

```bash
cd tests/container-apps/web/web-spa-integration-tests
dotnet test -c Release -- --filter-method Page_Context_On_Feedback_Uses_The_Ask_Scope_Not_A_Stuck_Manager
dotnet test -c Release -- --filter-method Arguments_Render_As_Json_And_Empty_Payloads_Are_Omitted
dotnet test -c Release -- --filter-method Relay_Sends_Tool_Arguments_And_Page_Context_As_Json
```

Each invocation takes one `--filter-method`. Expect `failed: 0`. The relay assertion parses `ToolResult` as JSON and rejects `System.Collections.Generic`.

**Depends on**

Docker, for Aspire-backed integration tests and the Playwright host. The browser trip signs in with a passkey and uses the fake chat upstream, which echoes the tool result into the answer. `sg docker -c '...'` when the shell is not in the `docker` group.

**Not in scope**

Opening the PR, `ganda kanban done`, a live xAI key, and showing the raw `page_context` result in the reasoning panel.

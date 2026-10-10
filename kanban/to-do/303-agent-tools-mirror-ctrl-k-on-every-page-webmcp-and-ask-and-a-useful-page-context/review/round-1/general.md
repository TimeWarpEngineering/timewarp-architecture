# Round 1 — general
**Date:** 2026-10-11
**Scope reviewed:** branch task/303 vs master (e6adf7714)

## Summary

The safety core holds. `navigate` rebuilds the palette at invoke time and refuses any url that is not one of the principal's Page rows. Both drivers re-check permission through `FindOfferedAsync`, and a page-bound action off its page returns the offer on every WebMCP and Ask path without binding or executing. The approval bit is unchanged for tools that run, the conversation credential is checked on every path, and neither the palette code nor `command-palette-tests.cs` was touched. The `Credentials.AddPasskey` change from Human to Both leaves palette rows unchanged, because `IsPaletteCommand` accepts both values.

The weak part is `page_context`'s text surface. It is a single snapshot taken at navigation or first render. It is not keyed by path and is never refreshed when `page_context` is called. Its documented 12,000-character cap is not enforced. The required headings test injects the expected strings, so the DOM walk that produces them has no test.

## Issues

### Issue 1 — Severity: bug
- File: source/container-apps/web/projects/web-spa/components/page-agent-context.cs:108
- Description: `page_context` can show text from a different page, or text from before the page finished loading. `Envelope` merges `AgentSurfaceState.PageSurfaceJson` into every route's document without checking which path that JSON came from. The JSON is written in exactly one production place, `agent-surface-state.sync-web-mcp.cs:33-34`. That handler is dispatched only by `WebMcpAgentSurface.razor:42` (first render) and `:55` (LocationChanged). Neither `page_context` caller re-walks the DOM: `web-mcp-dispatcher.cs:88` and `catalog-agent-functions.cs:433` both call `PageAgentContext.Describe` straight from the store.
  - **Stale data:** pages that load data after first render are captured before the data arrives. `FeedbackListPage.razor:27-34` awaits `ListMyFeedback` in `OnInitializedAsync`, and Home's "Signed in" card sits inside `AuthorizeView` (`HomePage.razor:32-64`). Text that changes after an agent action is never re-captured either.
  - **Wrong page:** nothing ties the stored surface to a route. The task's own Results sample shows `/Feedback` with the Home headings and summary.
  - **Ask:** `AgentAsk.razor:229` copies `Describe` into the instructions when the agent is built. `EnsureAgentAsync` only rebuilds when the path, edit mode or generation changes, so a surface that arrives after the build is never picked up.
- Suggestion:
  - Store the surface together with the path it was captured on, and merge it only when that path equals the current normalized path. Otherwise return empty arrays.
  - Re-run the walk when `page_context` is invoked. Both callers already have a JS runtime available through a handler or service.
  - Add a test: remember a surface for `/`, describe `/Feedback`, and assert none of the Home headings appear.
- Status: open

### Issue 2 — Severity: bug
- File: source/container-apps/web/projects/web-spa/components/page-agent-context.cs:228
- Description: The Design region says `page_context` is capped at 12,000 characters, but `Bound` does not guarantee it. `Bound` clears `summary`, then clears `headings`, then returns `document.ToJsonString()` without checking the length again. Forms, buttons, items, credentials and filings are never trimmed.
  - The walk's own caps allow far more than 12,000 characters: forms 10 × (120 + 20 × 80) ≈ 17,200, items 40 × 280 = 11,200, buttons 30 × 120 = 3,600.
  - `MergeSurface` (lines 170-200) re-applies only the array counts and the summary length. It does not re-apply the per-string caps or the 20-field-per-form cap.
  - That contradicts `agent-surface-state.remember-page-surface.cs:7-8`, which says "The walk's caps are applied again when page_context merges the JSON." `RememberPageSurface` is a public action, so any caller can store JSON that ignores the walk's caps.
- Suggestion: Re-apply the string and field caps in `MergeSurface`. Then make `Bound` keep dropping arrays (items, then forms, then buttons) until the document is at or under `DocumentCap`. Add a test that remembers an oversized surface and asserts `Describe(...).Length <= DocumentCap`.
- Status: open

### Issue 3 — Severity: bug
- File: tests/container-apps/web/web-spa-integration-tests/features/application/catalog-agent-tests.cs:605
- Description: The required test ("page_context on `/` includes … 'Welcome to TimeWarp.Architecture', 'Built with', 'Signed in'") does not test what it claims. It stores a fixture that already contains those three strings through `RememberPageSurfaceActionSet`, then asserts that `Describe` returns them. The production capture path has no automated test at all: `page-surface.ts` → `PageSurfaceJsModule.SummarizeAsync` → `SyncWebMcp`. That path is where heading discovery, shadow-root walking, the `.twe-page__body` selector and timing (Issue 1) can fail. The Results state that Playwright was not run, yet the Playwright checklist item at task.md:136 is marked `[x]`.
- Suggestion: Test the real capture: a Playwright check on `/` signed in, or at least a DOM-level test of `summarize()` against Home-like markup (`Card` renders `h2.twe-card__title`). Alternatively, untick the checklist item and record the gap in the Results.
- Status: open

### Issue 4 — Severity: suggestion
- File: source/container-apps/web/projects/web-spa/features/application/agent/catalog-agent-tool-set.cs:107
- Description: This branch removes master's guard `if (user.Identity?.IsAuthenticated != true) return [];`. The page-tool loop (lines 107-125) now offers page-scoped Agent tools to an anonymous principal whenever `IsPermittedAsync` passes. `IsPermittedAsync` returns true for an entry with no `Permissions`. The palette's rule (command-palette-roster.cs:13-15, `|| !isAuthenticated` at :70) is that a command with no permissions still needs a signed-in principal. No current page action has empty permissions, so nothing is exposed today. But this template ships to every generated app, and its first permission-less page action would be offered to anonymous visitors. The anonymous-on-`/Counter` assertion was also dropped from the test.
- Suggestion: In the page-tool loop, skip entries when `user.Identity?.IsAuthenticated != true`, matching the palette. Restore an anonymous assertion on a route that has page tools.
- Status: open

### Issue 5 — Severity: suggestion
- File: source/container-apps/web/projects/web-spa/features/application/agent/agent-navigate.cs:388
- Description: An Ask `navigate` call probably cuts off its own turn. `AgentAsk` is rendered inside each page's `TimeWarpPage` (`TimeWarpPage.razor:158`). When Ask's `navigate` calls `RouteState.ChangeRoute` to another page component, that `TimeWarpPage` remounts and `AgentAsk.Dispose` → `ReleaseAgent` disposes the `AgentContext` while the function call is still running. The model likely never sees the `{navigated:true}` result, so "go to Settings and add a passkey" cannot continue in the same turn. The restored thread may also hold a function call with no result. This path is not exercised anywhere: no test drives `navigate` through `FunctionInvokingChatClient`, and Playwright was not run. (Confidence: plausible, not reproduced.)
- Suggestion: Run Ask → navigate end to end, by Playwright or by a test that disposes the panel scope. If the turn is cut off, either defer the route change until the turn is idle, or record the tool result in the thread before navigating.
- Status: open

### Issue 6 — Severity: suggestion
- File: tests/container-apps/web/web-spa-integration-tests/features/application/catalog-agent-tests.cs:1058
- Description: `ExpectedNamesAsync` copies `SelectAsync`'s algorithm line for line: the palette loop, the Agent-visibility filter, the de-dup, the `PageAgentScope` loop, and appending navigate. Assertions such as `Names(home).ShouldBe(await ExpectedWebMcpNamesAsync(...))` therefore compare the implementation with a copy of itself. A shared mistake, such as a wrong order or a wrongly included name, would pass. The `/` order quoted in the Results (`Credentials.AddPasskey, Feedback.ListMyFeedback, navigate, page_context`) is only printed, never asserted. Separately, `thrown.Message.ShouldNotContain("navigateTo")` at line 746 can never fail, because an exception message never contains offer JSON.
- Suggestion: Replace the oracle with literal expected arrays for the fixed test principals. At minimum, assert the exact `/` list for the "everyone" principal. Drop the assertion at line 746.
- Status: open

### Issue 7 — Severity: suggestion
- File: source/analyzers/timewarp-architecture-analyzers/generators/page-source-generator.cs:562
- Description: Some valid `Description` values produce broken or silently empty output.
  - **Unescaped control characters:** `CSharpString` escapes only `\` and `"`. A verbatim or raw-string `Description` that contains a line break (or `\r`, or other control characters) is written raw into a regular `"…"` literal, and the generated registry fails to compile (CS1010).
  - **Silently dropped values:** lines 256-261 accept only a `LiteralExpressionSyntax`. A `const` reference, a concatenation or `nameof` silently becomes `""` with no diagnostic. That is caught only later, by the integration test, and only for Navigable pages.
  - **No escaping test:** the generator tests do not cover quotes or backslashes.
- Suggestion: Emit with `SymbolDisplay.FormatLiteral(value, quote: true)`, or use the semantic model's constant value so const expressions work. Add a generator test whose description contains a quote, a backslash and a newline.
- Status: open

### Issue 8 — Severity: nit
- File: source/container-apps/web/projects/web-spa/features/application/agent/web-mcp-dispatcher.cs:198
- Description: The `tool.OffPageRoute is not null` branch for a tool that is not page-bound can never run. `ToTool` sets `OffPageRoute` only from `PageAgentScope.PrimaryPage(entry.Name)`, which is non-null only for page-bound tools. The Design sentence at lines 25-26 ("returns a navigate offer if that selection is now discovery-only") describes behavior that cannot happen.
- Suggestion: Remove the branch and the sentence, or keep it as a defensive check and say so.
- Status: open

### Issue 9 — Severity: nit
- File: source/container-apps/web/projects/web-spa/features/application/agent/agent-ask-instructions.cs:31
- Description: `For` cuts the `page_context` JSON at 6,000 characters, then cuts the whole string at 7,000. On a large page the model receives a JSON document cut off partway through. Issue 2 makes this more likely, because the document can exceed 12,000 characters.
- Suggestion: Bound the document structurally (Issue 2), or pass a reduced copy without summary and items, instead of cutting the string.
- Status: open

### Issue 10 — Severity: nit
- File: source/container-apps/web/projects/web-spa/components/page-agent-context.cs:122
- Description: The prefix fallback gives the wrong title and purpose to non-navigable child routes. `/Admin/Roles/New`, where `Role.CreateRole` lives, gets title "Roles" and purpose "List the roles an administrator can open." This tells the agent the page is a list, while its only page tool creates a role.
- Suggestion: Accept this and document it, or let non-navigable `[Page]`s carry a `Description` and look it up from the page attribute rather than from the registry.
- Status: open

### Issue 11 — Severity: nit
- File: source/container-apps/web/projects/web-spa/source/features/web-mcp.ts:2
- Description: The Purpose region still says "Registers the page's catalog tools". This branch changes the list to palette commands, navigate, page tools and `page_context` (Requirement 7: no stale page-scoped claims).
- Suggestion: Reword it to "Registers the agent tool list (palette commands, navigate, page tools, page_context) with the browser model context."
- Status: open

### Issue 12 — Severity: nit
- File: source/container-apps/web/projects/web-spa/features/application/agent/page-agent-scope.cs:47
- Description: `Feedback.ListMyFeedback` is a Ctrl-K command that runs from any page. Its handler navigates to `/Feedback` itself (feedback-state.list-my-feedback.cs:63-67), so it needs no page state. Because it is listed under `/Feedback` in `PageAgentScope`, the agent only gets a navigate offer for it off `/Feedback`, while Ctrl-K runs it. This follows the steering's literal definition (page-bound means "named by PageAgentScope"), but it is a visible difference from the palette that Steven may not expect.
- Suggestion: Flag it for Steven. If wanted, leave `ListMyFeedback` out of the page-bound set, or add a "self-navigating" exemption.
- Status: open

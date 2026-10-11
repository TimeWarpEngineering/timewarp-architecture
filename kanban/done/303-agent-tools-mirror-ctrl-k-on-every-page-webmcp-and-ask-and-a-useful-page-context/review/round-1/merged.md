# Round 1 — merged findings
**Date:** 2026-10-11
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 2 | 1 |
| suggestion | 0 | 3 | 1 |
| nit | 0 | 3 | 2 |

Fixes landed in `95f184981` (fix: key page_context text to its page and walk it live on call).

## Issues

### M1 — Severity: bug — Status: fixed
- File: source/container-apps/web/projects/web-spa/components/page-agent-context.cs:108
- Description: The stored page-body summary was merged on every route regardless of the page it came from, and page_context never re-walked the page. So /Feedback showed Home's headings, and text that loaded after the sync was missed.
- Suggestion: Key the summary by path; walk live when the tool is called.
- Source: general
- Disposition notes: `AgentSurfaceState.PageSurfacePath` records the route the summary was read on (SyncWebMcp stores `PageAgentRoute.PathOr`, and RememberPageSurface takes a path). `PageAgentContext.Describe` merges the stored copy only on that path. The WebMCP dispatcher and Ask's `PageContextFunction` call `PageSurfaceJsModule.TrySummarizeAsync` when invoked and pass the live JSON in. Tests: `/Feedback` no longer contains the home headings; `WebMcp_Page_Context_Walks_The_Page_When_Called` proves the live walk wins over a stale copy and passes `.twe-page__body`.

### M2 — Severity: bug — Status: fixed
- File: source/container-apps/web/projects/web-spa/components/page-agent-context.cs:228
- Description: The documented 12,000-character cap was not enforced; MergeSurface did not re-apply the per-string caps.
- Suggestion: Enforce the cap and re-apply the caps.
- Source: general
- Disposition notes: MergeSurface rebuilds headings, buttons, forms, and items with the per-string caps. `Bound(document, maxLength)` drops surface text whole (summary, items, forms, buttons, headings) until the document fits, so the JSON stays valid. Page facts have their own caps and are not dropped (documented). Test `Page_Context_Stays_Valid_Json_Under_Its_Cap`.

### M3 — Severity: bug — Status: wontfix
- File: tests/container-apps/web/web-spa-integration-tests/features/application/catalog-agent-tests.cs:605
- Description: The required headings test stores a fixture and reads it back; the real capture is not tested. The Playwright checklist item is ticked although Playwright was not run.
- Suggestion: Test the real capture path.
- Source: general
- Disposition notes: Partly addressed. The C# capture path (dispatcher → PageSurfaceJsModule import → summarizeJson(selector) → page_context) is now tested with a JS stand-in. The TypeScript DOM walk itself has no test host in this repo (no JS test runner), and a live Playwright pass needs the app running, which the task forbids on TWE-001. The checklist item reads "if practical" and Results state that Playwright was not run. Accepted, with a live-browser check as the follow-up recorded on task.md. Decided by: review oracle.

### M4 — Severity: suggestion — Status: fixed
- File: source/container-apps/web/projects/web-spa/features/application/agent/catalog-agent-tool-set.cs:107
- Description: The master guard that gave anonymous principals no page tools was removed. A page action with no permissions would be offered to an anonymous visitor.
- Suggestion: Restore the sign-in rule for page tools.
- Source: general
- Disposition notes: Page tools are selected only for an authenticated principal. The anonymous `/Counter` assertion is restored (navigate only).

### M5 — Severity: suggestion — Status: wontfix
- File: source/container-apps/web/projects/web-spa/features/application/agent/agent-navigate.cs
- Description: Ask lives inside TimeWarpPage, so navigate remounts the panel and disposes its conversation mid-turn. The model likely never answers after a navigate.
- Suggestion: Keep the conversation alive across navigation.
- Source: general
- Disposition notes: The navigation itself completes and the transcript is restored from AskConversationThreads after the remount. Only the model's follow-up sentence in that turn is lost. Fixing it means moving the Ask panel out of the per-page TimeWarpPage (shell/layout work), and the same applies to existing handlers that navigate (ListMyFeedback). That is out of scope for this task. Recorded as a follow-up on task.md. Decided by: review oracle.

### M6 — Severity: suggestion — Status: fixed
- File: tests/container-apps/web/web-spa-integration-tests/features/application/catalog-agent-tests.cs:1058
- Description: Tool-list tests compare against a copy of the selection algorithm; the `/` order is only printed; `ShouldNotContain("navigateTo")` on an exception message can never fail.
- Suggestion: Assert a literal list; drop the vacuous assertion.
- Source: general
- Disposition notes: `/` for a fully permitted member now asserts the literal `[Credentials.AddPasskey, Feedback.ListMyFeedback, navigate, page_context]`. The vacuous assertion is removed.

### M7 — Severity: suggestion — Status: fixed
- File: source/analyzers/timewarp-architecture-analyzers/generators/page-source-generator.cs:562
- Description: CSharpString escaped only backslash and quote, so a newline broke the generated code. A non-literal Description silently became "".
- Suggestion: Use a full literal formatter; surface non-literal values.
- Source: general
- Disposition notes: Uses `SymbolDisplay.FormatLiteral(value, quote: true)`; test `Should_Escape_A_Description_With_Quotes_And_A_Newline`. A non-literal Description stays "" (the generator reads literals only, like the route arguments). That is documented beside the formatter, and `PageRegistry_.All_Should_` fails for any navigable page with an empty description.

### M8 — Severity: nit — Status: fixed
- File: source/container-apps/web/projects/web-spa/features/application/agent/web-mcp-dispatcher.cs:198
- Description: The off-page offer branch for tools that are not page-bound was unreachable, and the Design region described it.
- Suggestion: Remove the branch; fix the Design text.
- Source: general
- Disposition notes: Branch removed; Design says only page-bound tools carry an OffPageRoute.

### M9 — Severity: nit — Status: fixed
- File: source/container-apps/web/projects/web-spa/features/application/agent/agent-ask-instructions.cs:31
- Description: The instructions cut page_context JSON at 6,000 characters mid-document.
- Suggestion: Bound the document instead of slicing it.
- Source: general
- Disposition notes: AgentAsk builds the copy with `Describe(..., AgentAskInstructions.ContextCap)` (valid JSON). `For` only clips the whole string under MaxLength as a last guard. The preface tells the model to call page_context for current text.

### M10 — Severity: nit — Status: wontfix
- File: source/container-apps/web/projects/web-spa/components/page-agent-context.cs:122
- Description: /Admin/Roles/New takes the Roles list's title and purpose through the prefix match.
- Suggestion: Give the sub-route its own purpose.
- Source: general
- Disposition notes: /Admin/Roles/New is not a navigable registry page, and the prefix fallback is the documented design (a sub-route belongs to its section). The `tools` list on that route is accurate, so the agent still sees the create action. Decided by: review oracle.

### M11 — Severity: nit — Status: fixed
- File: source/container-apps/web/projects/web-spa/source/features/web-mcp.ts:2
- Description: The Purpose region still said "page's catalog tools".
- Suggestion: Update the Purpose.
- Source: general
- Disposition notes: Purpose now names palette actions, navigate, page tools, and page_context.

### M12 — Severity: nit — Status: wontfix
- File: source/container-apps/web/projects/web-spa/features/application/agent/page-agent-scope.cs:47
- Description: Ctrl-K runs ListMyFeedback from any page (its handler navigates to /Feedback). For the agent it is a navigate offer off /Feedback.
- Suggestion: Confirm intent with Steven.
- Source: general
- Disposition notes: This follows Steven's steering literally: page-bound is defined from PageAgentScope reversed, and ListMyFeedback is in the /Feedback scope. The agent still reaches the list through navigate. Flagged on task.md for Steven. Decided by: review oracle (per steering).

## Duplicates / conflicts

- None. Single reviewer.

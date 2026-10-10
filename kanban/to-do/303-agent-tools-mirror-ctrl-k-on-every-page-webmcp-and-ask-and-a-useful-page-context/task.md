# Agent tools mirror Ctrl-K on every page (WebMCP and Ask) and a useful page_context

## Description

Steven, 2026-10-11 ~01:00 ICT, looking at Chrome's Model Context Tool Inspector on `/`: WebMCP
registers nothing of value. "All available routing actions should be available from everywhere
just like they are in Ctrl-K dialog ... Not sure why WebMCP and Ctrl-K listings are much
different ... And page_context surely should provide more than just the path???"

**Decided (Steven): every action and navigation destination the Ctrl-K command palette offers the
signed-in person is also an agent tool on every page**, in both WebMCP and the in-app Ask agent,
with the same permission filtering as the palette. Page-specific tools are added on top for the
current page. This replaces task 282's "executable tools are page-scoped" rule.

### Current state (master `b89ef48e3`, paths under `source/container-apps/web/projects/web-spa/`)

- `features/application/agent/catalog-agent-tool-set.cs`: `SelectAsync` returns only
  `PageAgentScope.ActionNamesFor(path)` entries; a route with no scope entry (`/`) gets `[]`.
  `FindOfferedAsync` re-selects for the current path, so a tool is refused after navigation.
- `features/application/agent/page-agent-scope.cs`: hand-written per-route map.
- `features/application/agent/web-mcp-publisher.cs` / `web-mcp.ts`: route's catalog tools +
  `page_context`. On `/` that is `page_context` only.
- `features/application/modals/agent-ask/AgentAsk.razor` (~205-222): same selection; prompt says
  "Drive this page only through the supplied tools."
- `components/page-agent-context.cs`: rich facts only for /Settings, /Passkeys, /Profile,
  /Admin/Authentication, /Feedback(/{id}); everything else is `{"path":"..."}`.
- Ctrl-K source: `features/application/command-palette/command-palette-roster.cs`
  `CommandPaletteRoster.BuildAsync` = generated `PageRegistry.All` (`[Page(Navigable = true)]`,
  `components/interfaces/i-navigation-destination.cs`, same set NavMenu links) policy-checked via
  `IAuthorizationService`, + `IActionCatalog` commands (Human/Both, parameterless, permitted,
  signed in). Navigation runs through `RouteState.ChangeRoute`
  (`command-palette-runner.cs`). Also see `command-palette-js-module.cs`.

## Requirements

1. **One source.** Factor the palette's filtering (pages + commands, permission checks) into a
   shared roster/selector used by both `CommandPaletteRoster` and the agent tool set, so the
   palette and the agent list cannot drift. No second hand-written list.
2. **Agent tool list = palette list + page tools.** For a signed-in principal on any route:
   - every palette command whose `Visibility` includes `Agent` (i.e. `Both`) becomes a tool;
     Human-only entries never become tools (keep this rule);
   - every permitted navigation destination is reachable through a navigate tool (see 3);
   - the current page's `PageAgentScope` tools (including argument-carrying, Agent-only ones not
     in the palette) are added on top, de-duplicated;
   - `page_context` is always present.
   Anonymous principal: same as Ctrl-K's anonymous set (navigation to Home / Sign in) or nothing;
   pick and document, matching the palette.
3. **Navigation tool.** Pick between one `navigate` tool with a JSON-schema `enum` of the
   permitted destination URLs (with titles/descriptions) vs one tool per destination; implement
   the cleaner one and justify it in the design region (expectation: a single `navigate` tool with
   an enum keeps the tool list short and maps 1:1 to palette Page rows). It must refuse anything
   not in the principal's permitted set and go through `RouteState.ChangeRoute` like the palette.
   Navigation is not an edit: decide and document its approval behaviour per edit mode.
4. **Keep the safety rules.** Server-side authorization on every call stays. Invocation-time
   re-check stays: `FindOfferedAsync` must re-check permission for the principal at call time,
   but must not refuse a global (palette) tool just because the person navigated; page-only tools
   are still refused off their page. Edit-mode approval bit unchanged
   (`CatalogAgentApproval.RequiresApproval`). The conversation credential is still enforced at
   invoke time.
5. **WebMCP and Ask both.** `WebMcpPublisher.DescribeAsync`, `web-mcp.ts`/`WebMcpDispatcher`, and
   `AgentAsk.razor`'s tool selection all use the new selection. Update the Ask instructions so the
   model knows it can run global actions and navigate from any page (stay well under the
   8,000-char instructions cap in `complete-agent-chat-contracts.cs`).
6. **Useful `page_context` on every page.** Cheap JSON, for every route including `/`: `path`
   (route), page `title`/name, a one-line `purpose` ("what this page is for"), visible items with
   ids and flags where the page has them (keep existing Settings/Passkeys/Profile/
   Admin/Authentication/Feedback facts), and `tools`: the tool names relevant on this page
   (page tools + navigate). Page title/purpose must be single-sourced (e.g. a `Description` on
   `[Page]` emitted into `PageRegistryEntry`, guarded by a test that every Navigable page has
   one), not a second hand-written list.
7. **Design regions.** Update every `#region Design` / doc that says page-scoped
   (`catalog-agent-tool-set.cs`, `page-agent-scope.cs`, `web-mcp-publisher.cs`,
   `page-agent-context.cs`, `AgentAsk.razor`, `documentation/developer/guides/webmcp-testing.md`,
   AGENTS.md / skills that describe the agent tool set) per tw-agent-context-regions.

## Checklist

- [ ] Shared palette/agent roster; `CommandPaletteRoster` uses it
- [ ] `CatalogAgentToolSet.SelectAsync` = global palette tools + navigate + page tools; no `[]` on `/`
- [ ] `navigate` tool (or justified alternative), permitted destinations only, via RouteState
- [ ] `FindOfferedAsync` re-checks permission; global tools survive navigation; page tools don't
- [ ] WebMCP publisher/dispatcher and Ask use it; Ask instructions updated
- [ ] `page_context` has title, purpose, route, items/flags, relevant tools on every page; single-sourced page descriptions with a guard test
- [ ] Design regions and docs updated (no stale "page-scoped" claims)
- [ ] Test: WebMCP list on `/` for a member == palette's permitted agent-visible entries + navigate + page_context
- [ ] Test: a permission-gated entry/destination is absent without the permission (and navigate refuses it)
- [ ] Test: `page_context` on `/` has more than `path`
- [ ] ActionCatalog / catalog-agent / parity tests updated for any name changes
- [ ] Playwright check if practical (WebMCP tool list on `/`, or Ask navigates from `/`)
- [ ] PR body lists the tool names an agent sees on `/` and sample `page_context` for `/` and `/Feedback`

## Notes

- Supersedes the execution-scope rule of task 282 and overlaps task 300 (site map + navigate,
  which assumed executable tools stay page-scoped). This task implements navigate and the page
  descriptions; task 300 should be re-scoped or archived after this merges (Steven decides). Do
  not edit task 300 in this task.
- Task 302 (Feedback Details textarea) is running in parallel; avoid touching its files.
- Acceptance: full ganda walk via `ganda task work 303 --yes` (implement, review, audit,
  done-move commit, PR), CI green, merge only via `ganda pr merge` after Steven approves.
  Don't run the app on TWE-001.

## Session

- Created: 238403 (2026-10-11 ICT)

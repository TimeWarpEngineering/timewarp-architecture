# Ask: give the AI app-wide page/navigation context like Ctrl-K

## Description

Ask only knows about the page it is open on. Steven asked "where can I add a passkey" on Home
(`/`), and Ask answered "This page (/) doesn't expose any passkey controls...". Ctrl-K already
knows every page the user may open and every runnable command; Ask should get the same app-wide
picture, from the same source, so it can say "Passkeys are added on Settings (/Settings)" and
offer to take the user there.

### Current state (master `650fa24f2`, paths under `source/container-apps/web/projects/web-spa/` unless noted)

What the model gets today:

- `features/application/modals/agent-ask/AgentAsk.razor:205-213` selects tools with
  `CatalogAgentToolSet.SelectAsync(user, ..., ActionCatalog.Entries, path, mode, ...)` for the
  **current path only**.
- `AgentAsk.razor:221-222` system prompt (ChatOptions.Instructions) is literally
  `"Drive this page only through the supplied tools. " + PageAgentContext.Describe(ScopedStore, path)`.
  Nothing about other pages.
- `features/application/agent/catalog-agent-tool-set.cs:55-64`: anonymous principal gets no tools;
  a route with no `PageAgentScope` entry gets no catalog tools.
- `features/application/agent/page-agent-scope.cs:23-50`: hand-written per-route map (Counter,
  Settings, Passkeys, Profile, Admin/Roles/New, Admin/Authentication, Feedback). `/` (Home) has
  no entry, so Home offers zero catalog tools (task 282 page scoping, by design).
- `features/application/agent/catalog-agent-functions.cs:61-63` always appends `page_context`.
- `components/page-agent-context.cs:39-70`: `page_context` returns rich facts only for
  /Settings, /Passkeys, /Profile, /Admin/Authentication, /Feedback(/{id}); every other route is
  `{"path": "..."}` (line 69). On Home the model sees `{"path":"/"}` and nothing else.
- WebMCP is the same: `features/application/agent/web-mcp-publisher.cs:6-7` publishes the
  route's permission-filtered catalog tools plus `page_context` only.
- Fake upstream (Development/CI): `source/container-apps/web/features/agent-chat/xai-chat-upstream-server.cs:13,200-223`
  only ever calls `page_context` and echoes its result.
- Instructions are capped at 8,000 chars:
  `source/container-apps/web/features/agent-chat/complete-agent-chat/complete-agent-chat-contracts.cs:30`.

What Ctrl-K uses (the source to share):

- `features/application/command-palette/command-palette-roster.cs:43-82`
  `CommandPaletteRoster.BuildAsync`: pages from the generated `PageRegistry.All`
  (`[Page(..., Navigable = true)]`), each policy-checked via `IAuthorizationService`
  (lines 60-66); commands from `IActionCatalog` (`[CatalogAction]`), Human/Both, parameterless,
  permission-checked, signed-in only (lines 68-79, 95-97). Sign in row when signed out (55-58).
- `PageRegistryEntry` (generated, `source/analyzers/timewarp-architecture-analyzers/generators/page-source-generator.cs:497-503`)
  = `PageType, RouteTemplate, Url, Title, NavIcon, Policy`. There is **no description**, so a
  model can't tell from the registry alone that Settings is where passkeys live.
- Passkey locations: `/Settings` (`features/application/pages/SettingsPage.razor.cs:57`, policy
  SettingsRead) offers `Credentials.AddPasskey` / `AddExistingPasskey` (page-agent-scope.cs:26-34);
  `/Passkeys` (`features/identity/pages/passkeys-page/PasskeysPage.razor.cs:33`) is
  DeveloperAccess-only and offers fetch/revoke/rename, not add.
- Palette navigation runs through `RouteState.ChangeRoute`
  (`features/application/command-palette/command-palette-runner.cs:29-31`).

Root cause: the model's whole world is the current route (tools + `page_context` + a prompt that
says "this page only"). There is no app-wide map.

## Requirements

- Give the model the app-wide navigation map and the global action catalog, built from the
  **same source as Ctrl-K** (`PageRegistry.All` + `IActionCatalog`, filtered with the same
  `IAuthorizationService` checks as `CommandPaletteRoster`). No second hand-written page list.
  Factor the shared filtering so palette and agent cannot drift.
- Add a read-only tool (e.g. `site_map` / `list_pages`, never approval-wrapped, like
  `page_context`) returning, for the current principal: each permitted page's title, URL, icon
  name and a one-line description of what can be done there, plus the permitted catalog actions
  grouped by the page whose buttons dispatch them (reverse of `PageAgentScope`) with their
  descriptions.
- Page descriptions: add an authored source (e.g. a `Description` on `[Page]` emitted into
  `PageRegistryEntry`, or the catalog action descriptions per page) so "add a passkey" maps to
  Settings. Keep it single-sourced and generated; an analyzer/test should flag a Navigable page
  with no description.
- Optionally add a short permitted site map to the instructions (stay well under the 8,000-char
  cap), and change the "Drive this page only" prompt so the model may answer about and point to
  other pages while still only *executing* the current page's tools.
- Add a `navigate` tool (target must be a URL in the permitted map; refuse anything else) that
  goes through `RouteState.ChangeRoute` like the palette. It follows the edit-mode approval rules
  (asks first in "Ask before editing"). Answers should also include the page as a link. After
  navigation, the existing rebuild-on-path-change picks up the new page's tools.
- Signed-out: the map is the anonymous set (Home + Sign in), same as Ctrl-K.
- WebMCP publishes the same read-only map tool (and `navigate` if adopted) with the same
  filtering; update `documentation/developer/guides/webmcp-testing.md` and the Ask/agent
  sections of the repo skills that describe the tool set.
- Extend the Development fake upstream so a CI test can exercise the new tool (e.g. a prompt
  mentioning "passkey" calls the map tool and answers with the Settings link).

## Checklist

- [ ] Shared, policy-filtered page + action source used by both `CommandPaletteRoster` and the agent
- [ ] Page descriptions single-sourced in the registry, with a guard for missing ones
- [ ] Read-only site-map tool in Ask (`CatalogAgentFunctions`) and WebMCP (`WebMcpPublisher`)
- [ ] `navigate` tool restricted to permitted registry URLs, approval per edit mode
- [ ] System prompt updated (no longer "this page only"); size under the instructions cap
- [ ] Fake upstream supports the new tool
- [ ] Unit tests: payload contents and policy filtering (admin vs plain user vs anonymous; DeveloperAccess pages hidden without it); navigate refuses non-registry/unpermitted URLs
- [ ] Integration test: Ask tool list and WebMCP list both include the map tool on every route
- [ ] Real-browser (Playwright) test: signed in, Ask on Home, "where can I add a passkey" answers with the /Settings link (fake upstream ok); navigate with approval lands on /Settings
- [ ] Docs and skills updated
- [ ] Proof screenshots in the PR (Home question + answer, approval card, landed on Settings)

## Notes

- Steven, 2026-10-11 00:19 ICT: "the AI should have context of all the damn pages like Ctrl-K
  has ... not just what can be done on the page." Screenshot: Ask on Home answering that `/`
  has no passkey controls.
- Keep task 282's rule that *executable* tools are page-scoped; this task adds knowledge and
  navigation, not cross-page execution.
- Related: task 301 (Ask AI action missing from Ctrl-K).
- Acceptance: full ganda walk via `ganda task work 300 --yes` (implement, review, audit,
  done-move commit, PR), CI green, merge only via `ganda pr merge` after Steven approves.
  Don't run the app on TWE-001; browser proof comes from CI.

## Session

- Created: 307060 (2026-10-11 ICT)

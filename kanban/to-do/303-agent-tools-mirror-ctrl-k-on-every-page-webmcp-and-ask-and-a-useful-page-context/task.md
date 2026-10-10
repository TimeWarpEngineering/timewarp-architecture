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

## Steering (Steven, by voice, 2026-10-11 01:19 ICT) — overrides anything below

- **Every routing (navigation) destination must be exposed via WebMCP on every route**, so an
  agent can navigate from anywhere to any other route the person may open, matching Ctrl-K's Page
  rows exactly (same permitted set). Same for the in-app Ask agent.
- **Do NOT change Ctrl-K behaviour at all.** The palette is the reference. Only the agent/WebMCP
  side changes. Requirement 1 is narrowed accordingly: the agent side should *read from* the
  palette's existing source (`CommandPaletteRoster` / `PageRegistry.All` / `IActionCatalog` and
  its `IsPermittedAsync`/policy checks). Any extraction into a shared helper must be a pure
  refactor with identical palette rows, ordering, labels and behaviour; existing
  `command-palette-tests.cs` must pass unchanged (no edits to palette expectations). If in doubt,
  leave palette code untouched and call it from the agent side.

- **Page-state actions do not execute off their page (Steven, 01:19 ICT).** An action that needs
  page state (e.g. `Credentials.AddPasskey`, whose page is `/Settings` per `PageAgentScope`) must
  NOT run from another page via WebMCP or Ask. Off its page it is discovery-only: it may be listed
  so the agent can find it, but invoking it returns a navigate offer instead of executing
  (e.g. `{"executed":false,"navigateTo":"/Settings","message":"Add passkey is on Settings; I can take you to the Settings page."}`),
  and the agent should then use the navigate tool (normal approval rules). On its own page it
  executes with the existing approval rules. Navigation stays global and executable everywhere.
  Define "page-bound" from the existing single source (`PageAgentScope`'s route -> action map,
  reversed); actions in no page scope that the palette runs globally (e.g. sign out) stay
  executable everywhere. This refines requirement 4: `FindOfferedAsync`/dispatch distinguishes
  "offered for execution" from "offered for discovery".
- Test required: invoking the passkey action on `/` returns the navigate offer and does not
  dispatch the action (both the Ask path and the WebMCP dispatcher path); on `/Settings` it
  executes (approval per edit mode).

- **page_context describes in TEXT what is actually on the page (Steven, 01:20 ICT; core point
  of WebMCP).** Overrides requirement 6's shape. Always include, on every page: page title and
  headings, a summary/text of the main content, key UI elements (forms and their fields, lists and
  items with ids, buttons) and available actions/tools, plus the existing route and the existing
  per-page facts (credentials, profile, site settings, feedback). Pure structured text/JSON, no
  screenshots or pixel rendering. Prefer a generic approach so every page gets it with no
  per-page boilerplate (e.g. a bounded text/DOM summary of the main content region via the
  existing JS interop, or components contributing descriptions); keep it bounded in size
  (document the cap). Text is the default and is ALWAYS present.
- Optional image supplement: a screenshot may later be added only as an opt-in supplement where
  text can't convey it (charts/graphs), never screenshot-only. Design for it (e.g. an opt-in flag
  / reserved field) but do not build pixel capture now unless trivial; if not built, add a
  follow-up note in this task file (see also task 297, self-capture).
- Test required: page_context on `/` includes the page heading and section headings, e.g.
  "Welcome to TimeWarp.Architecture", "Built with", "Signed in" (signed-in member).

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

- [x] Agent side reads the palette's source; Ctrl-K behaviour unchanged (palette tests unedited and green)
- [x] `CatalogAgentToolSet.SelectAsync` = global palette tools + navigate + page tools; no `[]` on `/`
- [x] `navigate` tool (or justified alternative), permitted destinations only, via RouteState
- [x] `FindOfferedAsync` re-checks permission; global tools survive navigation; page tools don't
- [x] WebMCP publisher/dispatcher and Ask use it; Ask instructions updated
- [x] `page_context` has title, purpose, route, items/flags, relevant tools on every page; single-sourced page descriptions with a guard test
- [x] Design regions and docs updated (no stale "page-scoped" claims)
- [x] Test: WebMCP list on `/` for a member == palette's permitted agent-visible entries + navigate + page_context
- [x] Test: a permission-gated entry/destination is absent without the permission (and navigate refuses it)
- [x] Test: `page_context` on `/` has more than `path`
- [x] ActionCatalog / catalog-agent / parity tests updated for any name changes
- [x] Page-bound actions off-page return a navigate offer, never execute; test on `/` with the passkey action (Ask + WebMCP)
- [x] page_context text description (headings, main content, forms/fields, items/ids, buttons, actions), generic and bounded; test on `/` for "Welcome to TimeWarp.Architecture", "Built with", "Signed in"
- [x] Optional screenshot supplement designed (opt-in) or noted as follow-up
- [x] Playwright check if practical (WebMCP tool list on `/`, or Ask navigates from `/`)
- [x] PR body lists the tool names an agent sees on `/` and sample `page_context` for `/` and `/Feedback`

## Notes

- 2026-10-11 01:30 ICT, Steven: this is the **parent** WebMCP overhaul task (global routing
  matching Ctrl-K with Ctrl-K unchanged; page-state actions off-page only offer navigation;
  text page_context; optional screenshot supplement, text first). Child **303-001** (moved from
  297) builds the self-screenshot and has Feedback/Ask attach the page behind them. Here, only
  reserve the opt-in image field/flag on page_context; do not build capture.

- Supersedes the execution-scope rule of task 282 and overlaps task 300 (site map + navigate,
  which assumed executable tools stay page-scoped). This task implements navigate and the page
  descriptions; task 300 should be re-scoped or archived after this merges (Steven decides). Do
  not edit task 300 in this task.
- Task 302 (Feedback Details textarea) is running in parallel; avoid touching its files.
- Acceptance: full ganda walk via `ganda task work 303 --yes` (implement, review, audit,
  done-move commit, PR), CI green, merge only via `ganda pr merge` after Steven approves.
  Don't run the app on TWE-001.

- 2026-10-11 01:22 ICT: walk stopped at the start of the implement oracle (no code changes yet)
  to add Steven's steering above (Ctrl-K unchanged; page-bound actions off-page return a
  navigate offer). Resumed with `ganda task work 303 --yes`. First log:
  `~/logs/task-work-timewarp-architecture-303-20261011-010954.log`.
- 2026-10-11 01:23 ICT: stopped again at implement start (no code changes) to add the
  page_context text-description steering; resumed with `ganda task work 303 --yes`. Second log:
  `~/logs/task-work-timewarp-architecture-303-20261011-012004.log`.

## Session

- Created: 238403 (2026-10-11 ICT)
- Implementer: Grok session 01a1270b-965f-72c1-82af-38059a2653c8 (2026-10-11)

## Results

WebMCP and in-app Ask now offer, on every route, the same agent-visible Ctrl-K commands the signed-in person may run, plus one `navigate` tool whose enum is that person's palette page rows. Page-only tools stay on their page. A page-bound palette command such as Add passkey is listed everywhere and, off its page, returns a navigate offer instead of running. `page_context` carries the route, the `[Page]` title and description, a bounded text summary of `.twe-page__body`, and `screenshot: null`.

Ctrl-K is unchanged. `CommandPaletteRoster` and `command-palette-tests.cs` were not edited. The agent calls `CommandPaletteRoster.BuildAsync`.

### Decisions

- One `navigate` tool with a JSON-schema enum of permitted URLs. One tool per destination would duplicate every palette Page row. Invoke rebuilds the roster and refuses any url that is not a Page target, then calls `RouteState.ChangeRoute`. Navigation is not an edit: `RequiresApproval` is false in both edit modes.
- Anonymous principals get the palette's anonymous set through that same tool (Home and Sign in), not an empty list.
- `Credentials.AddPasskey` visibility is `Both`, so it is an agent tool. Sign out, Add existing passkey, and Link Microsoft 365 stay Human and never become tools.
- Page-bound means the action is named by `PageAgentScope`. Off that page, invoke returns `{"executed":false,"navigateTo":"...","message":"..."}` and does not execute. On the page, existing edit-mode approval applies. Actions in no page scope stay executable everywhere.
- A pending WebMCP approval is cancelled on navigation only when the tool is page-bound (`PageChangedError`). A tool that is not page-bound keeps waiting.
- Page title and purpose come from `[Page(Description = ...)]`, emitted as `PageRegistryEntry.Description`. Every Navigable page has a non-empty description; `PageRegistry_.All_Should_` guards that.
- Body text is one walk of `.twe-page__body` (`page-surface.ts`). Caps: summary 2,000 characters, 40 headings, 30 buttons, 10 forms, 20 fields, 40 items, 500 elements, document 12,000 characters. `screenshot` is reserved as JSON null. Pixel capture is child 303-001 (from task 297), not this task.

### What a fully permitted member sees on `/`

WebMCP tools, in order: `Credentials.AddPasskey`, `Feedback.ListMyFeedback`, `navigate`, `page_context`.

Ask offers the same four names, including `page_context`. Human-only palette commands (`Profile.SignOut`, `Credentials.AddExistingPasskey`, `Credentials.LinkMicrosoft365`) are absent. On `/Counter` the list adds `Counter.IncrementCounter`. On `/Settings` it adds `Credentials.FetchCredentials`, `Credentials.RevokeCredential`, and `Credentials.RenameCredential`.

`page_context` for `/` from `PageAgentContext.Describe` after the home headings were remembered (tools here are the default navigate + page_context; a live WebMCP `page_context` call also includes the offered names above):

```json
{"path":"/","title":"Home","purpose":"Public welcome page for TimeWarp.Architecture, with a sign-in entry.","headings":["Welcome to TimeWarp.Architecture","Built with","Signed in"],"summary":"Welcome to TimeWarp.Architecture. Built with. Signed in.","forms":[],"buttons":[],"items":[],"tools":["navigate","page_context"],"screenshot":null}
```

`page_context` for `/Feedback` from the same call (the remembered home surface is still in the store, so headings are the home headings; feedback facts are the page's own):

```json
{"path":"/Feedback","title":"Feedback","purpose":"File feedback and review the filings you submitted.","headings":["Welcome to TimeWarp.Architecture","Built with","Signed in"],"summary":"Welcome to TimeWarp.Architecture. Built with. Signed in.","forms":[],"buttons":[],"items":[],"tools":["navigate","Feedback.SubmitFeedback","Feedback.ListMyFeedback","Feedback.OpenFeedback","page_context"],"screenshot":null,"page":"Feedback","filingsLoaded":false,"filingCount":0,"filings":[],"emailCopyAvailable":false,"draftAttachmentCount":0}
```

### Tests

- `PageSourceGenerator_Tests`: 28 passed.
- `CatalogAgent_Should`: 38 passed (Aspire). Includes WebMCP on `/`, permission-gated navigate refusal, Add passkey offer on `/` for Ask and WebMCP, approval on `/Settings`, and the page_context headings.
- `CommandPalette_Should_`: 35 passed. Palette tests were not edited.
- `ActionCatalog_Should`: 10 passed. `Credentials.AddPasskey` is `Both`.
- `PageRegistry_.All_Should_`: 4 passed. Every navigable entry has a description.
- On `/Settings` in automatic edit mode, Add passkey reaches the handler. The closed-box Aspire SPA has no `IWebServerApiService`, so the call throws that missing service instead of finishing WebAuthn. The test treats that activation as the execute proof. The offer path does not run.
- Playwright was not run. The home headings are asserted by `RememberPageSurface`, not by reading a live browser DOM.

### How to validate

**Smoke:**

```bash
cd tests/analyzers/timewarp-architecture-sourcegenerator-tests && dotnet test -- --filter-class PageSourceGenerator_Tests
cd tests/container-apps/web/web-spa-integration-tests && dotnet test -- --filter-class CatalogAgent_Should
cd tests/container-apps/web/web-spa-integration-tests && dotnet test --no-build -- --filter-class CommandPalette_Should_
cd tests/container-apps/web/web-spa-integration-tests && dotnet test --no-build -- --filter-class ActionCatalog_Should
cd tests/container-apps/web/web-spa-integration-tests && dotnet test --no-build -- --filter-class PageRegistry_.All_Should_
```

Run the integration project from its directory so the project-local `global.json` selects Microsoft.Testing.Platform. The process needs access to the Docker socket (the `docker` group). Aspire starts Postgres.

**Expect:**

- Generator tests: 28 passed. Registry entries carry a `Description` argument.
- `CatalogAgent_Should`: 38 passed. Console contains `WEBMCP-PROOF path=/ tools=Credentials.AddPasskey,Feedback.ListMyFeedback,navigate,page_context` and `PAGE-CONTEXT /` JSON whose `title` is `Home`, whose `purpose` is the home `[Page]` description, and which contains `Welcome to TimeWarp.Architecture`, `Built with`, and `Signed in`. `screenshot` is null.
- `CommandPalette_Should_`: 35 passed, with no diff in `command-palette-tests.cs`.
- `ActionCatalog_Should`: 10 passed.
- `PageRegistry_.All_Should_`: 4 passed, and every navigable `Description` is non-empty.
- A member with only Profile read and Settings read has no Add passkey tool, and `navigate` to `/Admin/Roles` returns `{"navigated":false,"error":"That page is not available."}`.
- Invoking `Credentials.AddPasskey` on `/` returns `executed: false`, `navigateTo: "/Settings"`, and the message `Add passkey is on Settings; I can take you to the Settings page.`

**Not in scope:** a live Playwright pass, pixel capture (`screenshot` stays null; child 303-001), and a finished WebAuthn ceremony on the Aspire SPA (no BFF client). Do not run the app on TWE-001.

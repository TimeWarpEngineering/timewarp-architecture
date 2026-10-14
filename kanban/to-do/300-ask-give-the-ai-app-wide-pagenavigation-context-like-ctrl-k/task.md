# Ask: give the AI app-wide page/navigation context like Ctrl-K

## Description

Ask only knows about the page it is open on. Steven asked "where can I add a passkey" on Home
(`/`), and Ask answered "This page (/) doesn't expose any passkey controls...". The left-hand
navigation menu already defines what is navigable for the current user. Ask must be able to
navigate anywhere the menu can, from every page including Home, with its navigation tools
**derived from the menu's source of truth**, not from a hand-written per-page map.

Steven's correction (2026-10-11): do NOT keep or extend the hand-written per-page tool map
(`page-agent-scope.cs:23-50`) or the per-page cases in `page-agent-context.cs`. Replace them
with derivation from the nav menu data (`PageRegistry` from `[Page(..., Navigable = true)]`).
One source feeds the menu, Ctrl-K, Ask and WebMCP.

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

What the left-hand menu uses (the source of truth to derive from):

- `components/NavMenu.razor`: every link is `<TimeWarpNavLink TPage=... />`, and
  `components/elements/TimeWarpNavLink.razor:2` constrains `TPage` to `INavigationDestination`,
  i.e. `[Page(..., Navigable = true)]`, so every menu link is a generated `PageRegistry.All` entry.
- The menu still reads two hand-written things that are NOT on `[Page]`: the grouping
  (`FluentNavCategory` "Admin", "Demos", "Developer", plus the `#if (api)`/`#if (grpc)` template
  conditionals) and the `AuthorizeView Policy=AdminAccess` / `DeveloperAccess` region gates
  around those groups. Each page's own `Policy` is on `PageRegistryEntry`. The "External" group
  (Freeze Team, GitHub) is plain `FluentNavItem` hrefs, not registry pages.
- Which registry entries the menu actually lists is the hand markup's choice; the registry may
  contain Navigable pages that are not in the menu (and vice versa nothing outside the registry
  can be an internal link).

What Ctrl-K uses (same registry):

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
says "this page only"), and its tools come from a hand-written per-route map instead of the
menu/registry data. There is no app-wide map and no way to navigate.

## Requirements

- **Derive, don't hand-write.** Delete the hand-written per-route map in
  `features/application/agent/page-agent-scope.cs:23-50` and the per-page `Describe` cases in
  `components/page-agent-context.cs:39-70` that exist only to feed it. Do not extend them.
  Ask's navigation knowledge and tools come from the nav menu's source of truth:
  `PageRegistry.All` (`[Page(..., Navigable = true)]`).
- **One source for menu, Ctrl-K, Ask and WebMCP.** Move what the menu still hand-writes onto
  `[Page]` so it is generated into `PageRegistryEntry`: menu group/category (and order), and any
  page description used by the AI. Then `NavMenu.razor` renders from the registry (keeping the
  template `#if` conditionals working, e.g. via the page types themselves being conditional),
  and the group gates come from the pages' own `Policy` rather than separate `AuthorizeView`
  regions. If a page needs to be navigable but hidden from the menu, that is a flag on `[Page]`,
  not a separate list. Factor one policy-filtered "destinations for this principal" service
  (the `IAuthorizationService` checks now in `CommandPaletteRoster.BuildAsync:60-66`) and use it
  from NavMenu, `CommandPaletteRoster`, Ask (`CatalogAgentToolSet`/`CatalogAgentFunctions`) and
  `WebMcpPublisher`, so they cannot drift.
- **Guard.** Add a test or analyzer that fails if a second hand-written list of pages/routes for
  the agent reappears (e.g. no route string literals / `typeof(...Page)` lists in
  `features/application/agent/**`), and that the menu, palette and agent destination sets are
  equal for the same principal.
- **`site_map` tool (read-only).** Generated from that source, never approval-wrapped (like
  `page_context`), available on every page including Home and signed-out: for the current
  principal, each permitted destination's title, URL, group, icon and description (description
  only if it lives on the `[Page]` declaration).
- **`navigate` tool.** Generated from the same source, available on every page including Home.
  Target must be a destination in the current principal's filtered set (exactly what the menu
  shows that user); refuse anything else. Goes through `RouteState.ChangeRoute` like the
  palette. Approval follows the edit-behavior setting (asks first in "Ask before editing").
  Answers also include the page as a link. After navigation, the existing rebuild-on-path-change
  picks up the new page.
- **Page-scoped execution.** Page actions the agent may execute are also derived (e.g. from the
  catalog actions the page's own components dispatch, or metadata on `[Page]`/the action),
  not from a hand-written route map. If full derivation of per-page actions is not possible in
  this task, record the gap in Notes rather than keeping the old map.
- **System prompt.** Remove "Drive this page only"; the model may answer about and navigate to
  any permitted destination. Optionally inline a short permitted site map; stay well under the
  8,000-char instructions cap.
- **Signed-out.** Destinations are the anonymous set the menu shows (plus Sign in), same as Ctrl-K.
- **WebMCP** publishes the same `site_map` and `navigate` tools with the same filtering; update
  `documentation/developer/guides/webmcp-testing.md` and the Ask/agent/nav sections of the repo
  skills.
- Extend the Development fake upstream so CI can exercise `site_map` and `navigate`.
- **Tests.** Keep the passkey acceptance test: signed in, Ask on Home, "where can I add a passkey"
  answers with the `/Settings` link and navigate (with approval) lands on `/Settings`. Add a test
  that every menu destination the user can see is a `navigate` target (and nothing else is), for
  admin, plain user, developer and anonymous principals.
- **Acceptance.** Full ganda walk via `ganda task work 300 --yes`, CI green, merge only via
  `ganda pr merge` after Steven approves.

## Checklist

- [x] `page-agent-scope.cs` hand-written map and per-page `page-agent-context.cs` cases deleted
- [x] Menu grouping/description metadata on `[Page]`, generated into `PageRegistryEntry`; NavMenu renders from the registry
- [x] One policy-filtered destination service used by NavMenu, Ctrl-K, Ask and WebMCP
- [x] Guard test/analyzer against a second hand-written list; menu = palette = agent destinations
- [x] Read-only `site_map` tool and `navigate` tool on every page (Ask + WebMCP), approval per edit mode
- [x] System prompt no longer "this page only"; under the instructions cap
- [x] Fake upstream supports the new tools
- [x] Unit tests: policy filtering (admin / plain user / developer / anonymous); navigate refuses non-menu URLs
- [x] Test: every menu destination visible to the user is a navigate target
- [x] Real-browser (Playwright) test: Ask on Home, "where can I add a passkey" -> /Settings link; navigate with approval lands on /Settings (written; runs in CI, not run on TWE-001)
- [x] Docs and skills updated
- [ ] Proof screenshots in the PR (Home question + answer, approval card, landed on Settings) (the Playwright test writes them; attach from the CI artifact when the PR opens)

## Results

Ask, Ctrl-K, the left-hand menu and WebMCP now read one destination list, and Ask can answer
about and move to any page the menu shows, from every page including Home.

- **One source.** `[Page]` gains `NavGroup`, `NavOrder` and `AgentActions` (generator:
  `page-source-generator.cs`). `PageRegistryEntry` carries `NavGroup`/`NavOrder`, and
  `PageRegistry.All` is sorted by `NavOrder`, so registry order is menu order. A new generated
  `PageRegistry.ActionScopes` lists every page that declares `AgentActions`, with all its route
  templates.
- **Destination service.** `features/application/navigation/navigation-destinations.cs`
  (`NavigationDestinations.ForAsync`) filters the registry by each page's own `Policy`.
  `NavMenu.razor` renders it, grouping by `NavGroup`. The hand-written links, the `#if`
  regions and the `AuthorizeView` category gates are gone; the template flags still work
  because those page types are not compiled. `TimeWarpNavLink.razor` is deleted.
  `CommandPaletteRoster`, the agent tools and WebMCP read the same list. The menu now also
  shows Profile; it was already a navigable registry page and a Ctrl-K row.
- **Agent tools.** `site_map` (`agent-site-map.cs`) is read-only and never approval-wrapped.
  It returns the menu's destinations (title, url, group, icon name, `[Page]` Description, plus
  Sign in while signed out). `navigate` uses the same rows, refuses anything else, and asks for
  approval in Ask before editing: Ask wraps it, and WebMCP takes the approval gate after
  refusing an unknown url without a prompt. Its result carries the page title. Both tools are
  on every page, for every principal.
- **No hand-written map.** `page-agent-scope.cs` keeps no route list; it matches the path
  against `PageRegistry.ActionScopes` (`{x:guid|int|long|bool}` tokens are type-checked). The
  per-route `page_context` cases are gone. States that own a page's actions implement
  `IAgentPageFacts` (`credentials-state`, `profile-state`, `site-settings-state` and
  `feedback-state` `.agent-page-facts.cs`) and add their own facts.
- **Prompt.** `AgentAskInstructions` no longer limits Ask to the page on screen. It tells the
  model to link pages as `[Title](url)` and inlines the site map (at most `SiteMapCap` 2,000
  characters). `page_context` gets whatever budget remains (`ContextCapFor`), and the total
  stays at most 7,000, under the 8,000 contract cap.
- **Fake upstream.** A "where" question with `site_map` offered calls `site_map`, then links the
  first destination whose title or description matches. "Take me there" calls `navigate` with
  the last linked url. Everything else keeps the `page_context` script.
- **Tests.** New `navigation-destinations-tests.cs` (Unit, in-proc) covers four principals:
  administrator, plain user, developer and anonymous. For each, the rendered NavMenu hrefs equal
  `NavigationDestinations`, the palette's Page rows, the `navigate` enum and the `site_map` rows
  (plus Sign in when signed out). Every other registry page, Login while signed in, and an
  unknown url are refused. A guard fails on a route string literal or `typeof(...Page)` in
  `features/application/agent/**` or `page-agent-context.cs`. Registry order and
  `AgentActions` catalog membership are also checked.
  - Updated suites: generator tests (NavOrder sort, NavGroup, AgentActions), catalog-agent
    tests (site_map in the lists, navigate approval in WebMCP and Ask, instructions cap), and
    the fake-upstream tests.
  - New Playwright test `Home_Should_Point_Passkeys_To_Settings_And_Navigate_After_Approval`
    in `ask-sign-in-playwright-tests.cs`. It signs in, opens Ask on Home and asks "Where can I
    add a passkey?". It checks for a /Settings link, then "Take me there", the navigate approval
    card with /Settings, Approve, and landing on /Settings. It writes ask-home-where-passkey*.png,
    ask-navigate-approval.png and ask-navigate-landed-settings.png.
- **Docs.** `documentation/developer/guides/webmcp-testing.md`, `skills/tw-blazor`,
  `skills/tw-blazor-layout` and `skills/tw-slice-isolation`.

Gates run locally on 2026-10-15:

- `dev build`: 0 warnings, 0 errors.
- web-spa Unit lane: 163/163 before the new class; `NavigationDestinations_` 6/6.
- Source-generator tests: 120/120.
- `XaiChatUpstream_`: 8/8.
- The Aspire-backed `CatalogAgent_Should` suite and the Playwright suites were not run here.
  Workers must not start an AppHost, so CI runs them.

### Review disposition

- Rounds: 2. Reviewer: general, effort 3 (budget by diff, 2532 lines).
- Final counts: bug 0; suggestion 4 fixed; nit 5 fixed, 1 wontfix; 0 open.
- Outcome: **accepted-exceptions**. M7 is wontfix: the off-page `navigateTo` ignores the
  principal's access to that page, but `navigate` refuses the url (see Notes).
- Fixes (273115a53 and the round-2 follow-up):
  - per-principal destination sets are pinned;
  - the guard is recursive and also catches `nameof`/`XPage.Member`;
  - each `NavGroup` must be one contiguous run;
  - the generator reads `NavGroup`/`Description` as constants;
  - unknown route constraints fail closed;
  - NavMenu recomputes only when the auth task changes.
- Gates after the fixes: `dev build` 0/0, `NavigationDestinations_` 6/6, generator 121/121,
  web-spa Unit 169/169.
- Artifacts: `review/review-framework.md`, `review/round-1/` (general, merged),
  `review/round-2/` (general, merged), `review/disposition.md`.

### How to validate

Smoke:

```bash
dev build
dotnet run --project tests/container-apps/web/web-spa-integration-tests -- --filter-class NavigationDestinations_
dotnet run --project tests/analyzers/timewarp-architecture-sourcegenerator-tests -- --filter-class PageSourceGenerator
dotnet run --project tests/container-apps/web/web-server-integration-tests -- --filter-class XaiChatUpstream_
```

Then let CI run `CatalogAgent_` and `AskSignInPlaywright_` (or run them on a machine that may
start an AppHost).

Expect:

- Build is 0/0.
- `NavigationDestinations_` passes 6/6 and prints `DESTINATIONS-PROOF` lines. Anonymous shows
  only `/`; PlainUser shows `/,/Settings,/Profile,/AgentLinks,/Feedback`.
- The generator suite passes.
- The fake-upstream suite passes 8/8.
- In CI, `Home_Should_Point_Passkeys_To_Settings_And_Navigate_After_Approval` passes and the
  three proof screenshots are in the Playwright artifact.
- By hand (signed in, Ask on Home), "where can I add a passkey" answers with a Settings link.
  "Take me there" shows an approval card for navigate with `/Settings`, and Approve lands on
  Settings.

## Notes

- Steven, 2026-10-11 00:19 ICT: "the AI should have context of all the damn pages like Ctrl-K
  has ... not just what can be done on the page." Screenshot: Ask on Home answering that `/`
  has no passkey controls.
- Steven, 2026-10-11 (voice call): correction. Do not keep or extend the hand-written per-page
  tool map. The left-hand nav menu already defines what is navigable; derive Ask's tools from
  that same menu/registry data so navigation works from every page, and Ask can navigate
  anywhere the menu can. Page descriptions, if any, live only on the `[Page]` declaration.
- Keep task 282's rule that *executable* page actions are page-scoped; this task adds
  app-wide knowledge and navigation, not cross-page execution.
- Related: task 301 (Ask AI action missing from Ctrl-K).
- Acceptance: full ganda walk via `ganda task work 300 --yes` (implement, review, audit,
  done-move commit, PR), CI green, merge only via `ganda pr merge` after Steven approves.
  Don't run the app on TWE-001; browser proof comes from CI.
- Page-scoped execution is metadata on `[Page]` (`AgentActions`). Deriving it from the actions a
  page's components actually dispatch was not attempted: Razor output is not visible to other
  source generators, and dispatch often sits in child components. The guard test keeps route
  and page lists out of agent code, but it does not prove a page's `AgentActions` match its
  buttons.
- Category gates now come from each page's own `Policy`. A principal with `settings.write` but
  not `admin.access` sees Admin > Authentication, and the same holds for the other admin
  read policies.
- Unverified risk: after an approved `navigate`, the page remounts `TimeWarpPage`, and with it
  the Ask panel. The model's follow-up text for that turn may be cut off; the existing
  palette-row navigate behaves the same way. The Playwright test asserts the landing on
  /Settings, not a follow-up answer.

- Review M7 (wontfix): `PageAgentScope.PrimaryScope`/`PrimaryPage` pick an action's off-page
  `navigateTo` by menu rank, not by what the principal may open. A principal with
  `credential.manage.self` but not `settings.read` is offered `/Settings`, and `navigate`
  refuses it. A follow-up could pass the principal's destinations into the scope choice.

## Session

- Created: 307060 (2026-10-11 ICT)
- Implement oracle: implemented by implementer-claude (claude, model claude-opus-5-5), session not reported, max-turns 500, Done — 2026-10-14T17:12:21Z
- Review oracle: claude (claude-opus-5-5), session not reported; effort 3, roster general, 2 rounds, accepted-exceptions — 2026-10-15

# Analyzer: a component link (Href, NavLink, anchor, FluentAnchor) is a State navigate action or an opted-out side effect

## Description

The rule since task 260: every SPA user interaction dispatches a TimeWarp.State action. TWA0026
(`component-side-effect-analyzer.cs`, task 265) enforces it for method calls only:
`NavigationManager.NavigateTo`, `IJSRuntime`, `IApiService`, `HttpClient`, browser storage. It
never looks at markup. An `Href="…"` in a `.razor` file compiles to an `AddComponentParameter`
call with a string name, which the analyzer ignores. So `FluentAnchorButton`, `FluentAnchor`,
`FluentLink`, `FluentButton Href`, `NavLink`, and raw `<a href>` all build clean, and
`RolesListPage.razor` line 31 ships a New Role button that is a plain link (task 207, 2026-09-02,
before the analyzer existed). 22 `.razor` files under web-spa carry an `Href`, `<a href`, or
`NavLink` today.

`skills/tw-blazor/SKILL.md` line 360 also says "Navigation is `FluentAnchor`", which contradicts
line 45 ("navigation — `RouteState.ChangeRoute(...)` for an in-app route"). The skill and the
analyzer must say the same thing.

Found 2026-11-24 while validating 490: Ask AI could not find a create-role entry point because New
Role is not an action.

## Depends on

- 510
- 511
- 512

## Requirements

- New diagnostic **TWA0050** in `timewarp-architecture-convention-analyzers` (next free id per the
  `AnalyzerReleases.Unshipped.md` header; update that header). Severity error. It reports, inside
  any `ComponentBase`-derived type's generated Razor code, a component parameter or HTML attribute
  named `Href`, `href`, or `To` whose value is not null, and any `NavLink` or `<a>` element with an
  `href`. External links (`http://`, `https://`, `mailto:`, `tel:`) with `target="_blank"` or
  `rel="external"` are not reported. Message names the fix: dispatch `RouteState.ChangeRoute` (or
  the page's navigate action) from a State action.
- Opt-out is the existing `[DirectComponentSideEffect("reason")]` on the component, reported by
  TWA0027 when the reason is empty. `NavMenu`, breadcrumbs, and the footer may opt out with a
  reason if an implementer judges a real anchor is required for accessibility or middle-click.
  Record the decision in that component's Design region.
- Convert the offenders. `RolesListPage` New Role becomes a `FluentButton` whose click dispatches
  a State action that navigates to `RolePage` (so it also appears in the action catalog for Ask
  AI). Do the same for every other hit the analyzer reports, or opt the component out with a
  reason. Zero TWA0050 on `dev build`.
- Fix `skills/tw-blazor/SKILL.md` line 360 and add TWA0050 to its analyzer table beside TWA0026.
- Analyzer tests: positive for `FluentAnchorButton Href`, `FluentAnchor Href`, `NavLink href`,
  `<a href>`, `FluentButton Href`; negative for an external `https://` link with
  `target="_blank"`, a component with `[DirectComponentSideEffect]`, and a `FluentButton` with
  `OnClick` only.

## Goal (implementer restatement)

Close the markup gap in "every SPA interaction is a State action". TWA0026 catches navigation
calls in C#, but a link written in Razor markup (`Href=`, `NavLink`, `<a href>`, Fluent anchors)
compiles to render-tree calls that nothing checks, so links bypass State actions and Ask AI
cannot see them.

Acceptance:

- A new error TWA0050 in the convention analyzers flags, inside a `ComponentBase` type, a link
  written in markup: a component parameter or attribute `Href`/`href`/`To` with a non-null value,
  and `<a>` / `NavLink` with an `href` (static markup included). An external link (http, https,
  mailto, tel) that opens in a new tab or says `rel="external"` is not flagged. The message names
  the fix (dispatch `RouteState.ChangeRoute` or the page's navigate action).
  `[DirectComponentSideEffect("reason")]` opts out (TWA0027 already covers an empty reason).
  `AnalyzerReleases.Unshipped.md` gets the row and the next-free-id header moves to TWA0051.
- Every web-spa hit is converted to a State action or opted out with a reason recorded in that
  component's Design region. `dev build` reports zero TWA0050.
- RolesListPage New Role is a `FluentButton` that dispatches a `[CatalogAction]` RoleState action
  which navigates to RolePage, so Ask AI can find it.
- `skills/tw-blazor/SKILL.md` no longer says "Navigation is `FluentAnchor`" and lists TWA0050 next
  to TWA0026; `dev lint-skills` passes.
- Analyzer tests: flagged for FluentAnchorButton Href, FluentAnchor Href, NavLink href, `<a href>`,
  FluentButton Href; not flagged for an external https link with `target="_blank"`, an opted-out
  component, and a FluentButton with only OnClick.
- Gates: `dev build` 0/0, analyzer tests and the web-spa tests that cover the changed components
  green, `ganda repo audit` green. A screenshot of `/Admin/Roles` goes in `evidence/` if a browser
  run is possible from this worker.

## Checklist

- [x] TWA0050 analyzer + tests; `AnalyzerReleases.Unshipped.md` and its next-free-id header updated
- [x] All web-spa hits converted to State actions or opted out with a reason in a Design region
- [x] `RolesListPage` New Role is a catalogued navigate action; Ask AI can reach it
- [x] `skills/tw-blazor/SKILL.md` reconciled; `dev lint-skills` green
- [x] Screenshot of `/Admin/Roles` with the converted button in `evidence/`
- [x] `dev build` 0/0, `ganda repo audit`, analyzer tests, web-spa integration tests green

## Session

- Created: 2026-11-24. Investigation: TWA0026 exists and is unsuppressed; the gap is markup
  links, never specced (tasks 260, 265).
- 2026-11-24/25 implementer (Opus 5.5, headless walk): TWA0050 analyzer, shared `RouteLink`,
  `RoleState.OpenNewRole`, all 28 first-build hits converted or opted out, skill reconciled.
  Audit boy-scout: `ganda repo audit` warned only on `vscode-window-icon` (no local
  `.vscode/settings.json`); `ganda repo audit --fix` wrote that gitignored file, nothing to commit.
- Implement oracle: implemented by implementer-claude (claude, vendor anthropic, model claude-opus-5-5, response model claude-opus-5-5), session 79a780c3-7854-40af-828e-d22e8d02fb0a, max-turns 500, Done — 2026-11-24T17:09:19Z

## Results

### What changed

- **TWA0050** (`source/analyzers/timewarp-architecture-convention-analyzers/component-link-analyzer.cs`,
  Error). Reads Razor's render-tree calls inside a `ComponentBase` type: a component parameter or
  attribute `Href`/`href`/`To` with a non-null value, `<a href>` from `OpenElement "a"`, and every
  `<a …>` tag in an `AddMarkupContent` string (static markup). Not reported: a constant
  http/https/mailto/tel href that opens a new tab (`target="_blank"` or a `Target` enum member
  `Blank`) or says `rel="external"`; a computed href with `rel="external"`; a null/false value;
  non-anchor elements (`<link href>`). Opt-out and gating reuse TWA0026 (the four helpers in
  `component-side-effect-analyzer.cs` are now `internal`). Release row added; header now says
  next free id TWA0051; csproj description range TWA0002–TWA0050.
- **`components/RouteLink.razor`**: the one shared in-app text link. One `Route` drives the
  href and the click, which is prevented and dispatches `RouteState.ChangeRoute`. Opted out with
  a reason (real anchor keeps middle-click, open in new tab, static prerender). A caller's scoped
  CSS reaches its anchor through `::deep` (breadcrumb, design-system catalog).
- **`RoleState.OpenNewRole`** (`role-state.open-new-role.cs`): `[CatalogAction]` Both, permission
  `admin.roles.manage`; handler navigates to `RolePage` (skips when already there). RolesListPage
  New Role is a `FluentButton` dispatching it; role names are RouteLinks. It is a Ctrl-K row
  ("Role: Open new role") and an agent tool on every page for a principal who may create roles.
- **Converted to RouteLink**: RolesListPage role name, PrincipalsPage, PrincipalDetailPage back,
  AgentLinksPage, FeedbackAdminListPage, FeedbackOwnerLink, FeedbackItemPage, FeedbackListPage
  (2), HomePage Settings/Admin, PasskeysPage, NotFoundPage, RedirectToLogin fallback,
  TimeWarpPage breadcrumbs, DesignSystemCatalog (All + parts), DefinitionListSample, LinkDisplay,
  AskPrivacyBanner, AskAnswerBar Support (that component's clipboard opt-out had hidden it).
- **Converted to a button that dispatches**: DesignSystemPage unknown-slug "All"
  (`RouteState.ChangeRoute`); UserCard is now `OnClick` (a `<button>` root) instead of `Href`, and
  ShellUserCard / UserCardSample dispatch `RouteState.ChangeRoute` to Profile.
- **Opted out with a reason in the Design region**: NavMenu (reason extended: FluentNavItem Href
  drives active-item Match and middle-click), FeedbackBody (attachment download is an API file,
  not an SPA route), SafeMarkdown `OpenAnchor` member (markdown content links; app links already
  go through `OnAppLinkClick`). HomePage "Built with" NavLinks stay: https + `target="_blank"`.
- **Deleted dead code**: `components/services/link-helper.cs`, `AuthorizedFluentNavLink.razor`,
  `SideNavigationLink.razor` (no references); the CA1052 NoWarn comment no longer names LinkHelper.
- **Skill**: `skills/tw-blazor/SKILL.md` "Navigation is `FluentAnchor`" replaced; a TWA0050
  paragraph sits beside TWA0026 (FluentButton + action for commands, RouteLink for text links,
  what is exempt, the opt-out cases).
- **Tests**: new analyzer tests (fixture + 8 flag + 6 exemption cases: FluentAnchorButton,
  FluentAnchor, NavLink, `<a href>`, FluentButton Href, static markup, child-content lambda,
  https without new tab; external https/enum Blank/rel external/mailto, class and member
  opt-out, empty reason still flags, FluentButton OnClick only, null/`<link>`/non-component,
  non-SPA). Updated: UserCard and TimeWarpPage render tests (button root), design-system page
  order marker, action-catalog roster (+`Role.OpenNewRole`), WebMCP home tool list, palette
  member deny list, new palette test (Enter on "open new role" lands on `/Admin/Roles/New`),
  role Playwright test (list shot, no href on New Role, click lands on the form).

### Gates

- `./bin/dev build` — passed, 0 warnings 0 errors, zero TWA0050 (first build with the analyzer
  reported 28 hits; all converted or opted out).
- Analyzer test files are project tests, not standalone runfiles (no `#if !JARIBU_MULTI`), so
  they ran through the dev CLI:
  - `./bin/dev test --project timewarp-architecture-analyzers-tests --filter-class _Links` —
    `component-link-flag-analyzer-tests.cs` + `component-link-exemption-analyzer-tests.cs`
    (fixture `component-link-test-fixture.cs`): 14/14 passed.
  - `--filter-class Side_Effects_In_Components` (TWA0026, helpers made internal): 14/14 passed.
- web-spa-integration-tests (project tests, not runfiles), `./bin/dev test --project
  web-spa-integration-tests --filter-class <ns>`:
  - `UserCardRender_` (`user-card-render-tests.cs`): 5/5 passed.
  - `TimeWarpPageRender_` (`time-warp-page-render-tests.cs`): 17/17 passed.
  - `DesignSystemPageContract_` (`design-system-page-contract-tests.cs`): first run 5/6 (page-order
    marker was the old card href); marker fixed, rerun 6/6 passed.
  - `ActionCatalog_` (`action-catalog-tests.cs`): 10/10 passed.
  - `CatalogAgent_` (`catalog-agent-webmcp-tests.cs` and siblings): first run 55/56 (home tool
    list now has `Role.OpenNewRole`, as intended); list updated, rerun 56/56 passed.
  - `CommandPalette_` (`command-palette-tests.cs`, `command-palette-navigation-tests.cs`):
    45/45 passed before the new test; `--filter-method Open_New_Role` 1/1 passed.
  - Covering, not edited: `AskPrivacyBanner_` 4/4, `DesignSystemCatalogCoverage_` 2/2,
    `SafeMarkdownRender_` 15/15, `AskRejection_` 4/4, `RoleFormPrefill_` 2/2 passed.
- web-server-integration-tests `ProtectedPageDeepLink_` (NewRole data-qa): 16/16 passed.
- web-spa-playwright-tests (in-process Web.Server, no AppHost): `RolePagesPlaywright_`
  (`role-pages-playwright-tests.cs`) 2/2, `BreadcrumbPlaywright_` 1/1,
  `DesignSystemCatalogPlaywright_` 1/1, `ShellNavPlaywright_` 2/2, `ProfileEditPlaywright_` 1/1
  passed.
- `./bin/dev lint-skills` — 9/9 skills passed.
- `./bin/dev check-version` — source 2.0.0-beta.32 is ahead of NuGet 2.0.0-beta.31; no bump.
- `ganda repo audit` — passed with one advisory (`vscode-window-icon`); `ganda repo audit --fix`
  fixed it (gitignored local file) and reported "Repository passes all audit checks."
- Not run here: full `./bin/dev test` and `dev template-smoke` (CI runs both on the PR).

### Evidence

- `evidence/admin-roles-list.png` — `/Admin/Roles` from `RolePagesPlaywright_`: New Role is a
  primary FluentButton, role names are RouteLinks, breadcrumb and user card styled as before.

### How to validate

- Smoke: `./bin/dev build`, then `./bin/dev test --project timewarp-architecture-analyzers-tests
  --filter-class _Links`, then `./bin/dev test --project web-spa-playwright-tests --filter-class
  RolePagesPlaywright_`. To see the rule fire, add `<a href="/Counter">x</a>` to any web-spa
  `.razor` page and build.
- Expect: build 0/0 with no TWA0050; 14/14 analyzer tests; the role Playwright test passes (New
  Role has no href and its click lands on `/Admin/Roles/New`). The added anchor fails the build
  with `error TWA0050: <a href> navigates without a TimeWarp.State action; dispatch
  RouteState.ChangeRoute …`. In the app, Ctrl-K "open new role" opens the form for an admin.

### Follow-ups / risks

- RouteLink prevents the default click, so Ctrl+click navigates in place; middle-click and the
  context menu still open a new tab.
- UserCard lost its `Href` parameter (now `OnClick`); a generated app that passes `Href` gets a
  compile error naming the parameter.
- TWA0050 does not read `@attributes` splats or generic components whose parameters go through
  Razor's TypeInference helper (recorded in the analyzer Design region).

---
name: tw-blazor
description: "Razor file authoring — one @code at the top, markup, optional <style> last. Use when creating or editing .razor files, @code blocks, component event handlers (they only dispatch TimeWarp.State actions), or in-file <style> tags. CSS placement: tw-blazor-css-strategy. App shell: tw-blazor-layout."
---

# `.razor` file order

1. Directives (`@namespace`, `@inherits`, `@using`, `@inject`, comments)
2. **One** `@code { … }` — omit if none or code-behind only
3. Markup
4. Optional `<style>` last (Exception B — `tw-blazor-css-strategy`)

Never two `@code` blocks. Never `@code` after markup. Never `<style>` above markup.

Hand-written members live in `@code`. A `.razor.cs` exists only for attributes the C# source
generators and class-level analyzers must see (`[Page]`, `[Authorize]`, `[CrossSliceReference]`).
`PageSourceGenerator` does not run on `.razor` files. Do not put `[Page]` in `@code` or use
`@page` on a page that already has `[Page]`.

```razor
@namespace TimeWarp.Architecture.Features.Example
@inherits BaseComponent

@code {
  [Parameter] public string? Title { get; set; }
}

<div class="twe-example">@Title</div>

<style>
  @(@"
    .twe-example { color: var(--twe-ink); }
  ")
</style>
```

# User interactions are actions

Every user interaction in the SPA is a TimeWarp.State action. A component event handler
(`OnClick`, `OnValidSubmit`, `@onkeydown`, `ValueChanged`, …) only dispatches generated ActionSet
methods, and at most sequences several of them (`await CredentialsState.AddPasskey();
await CredentialsState.FetchCredentials();`), reading state between dispatches to decide the next.
The work lives in the action's handler:

- navigation — `RouteState.ChangeRoute(...)` for an in-app route; a handler that injects
  `NavigationManager` for a full-page load (`forceLoad: true`, e.g. a BFF challenge) or a
  destination only known after the work (post-sign-in return URL)
- JS interop (`IJSRuntime`, JS modules, browser storage)
- API calls (`IWebServerApiService` / `IApiServerApiService`, ceremony clients)
- state changes

Why: an action is the one seam that the Ctrl-K palette (every action, by derivation), the action
catalog (`[CatalogAction]` — agent tools and palette metadata), State telemetry, and headless tests
all see. A method on a page is invisible to
all of them: it cannot be run from the palette, replayed, or dispatched in a test without a
renderer. A button whose handler does the work itself is a feature nobody else can reach.

Exempt — purely presentational, component-local UI state: hover, focus, scroll-into-view,
whether a local panel or editor is open, and the text bound to an input (including resetting a
form draft from state). Lifecycle work (`OnInitializedAsync` loads, a render-time redirect) is
held to the same rule: dispatch an action (`RouteState.ChangeRoute` for an already-signed-in
redirect, a handler with `forceLoad: true` for an auth-gate redirect).

**Compiler-checked — TWA0026.** In SPA client code (Blazor WASM SDK), any member of a
`ComponentBase` type — event handlers and lifecycle overrides alike — that calls one of these is
reported: `NavigationManager.NavigateTo` / `NavigateToLogin` / `Refresh`; `IJSRuntime` /
`IJSObjectReference` invokes; `IApiService` and its subtypes; `HttpClient` send/get/post/put/
delete/patch; session/local storage writes (`SetItem*`, `RemoveItem*`, `Clear*` — reads are
fine); and any first-party type marked `[SideEffectService]` (ceremony clients, `*JsModule`
helpers — mark new ones the same way). Handlers, services and static helpers are never flagged;
that is where the work goes. A genuinely presentational JS call (focus, scroll, hotkey
registration) opts out with `[DirectComponentSideEffect("reason")]` on the component (`@attribute`
in the `.razor`) or member; an empty reason does not opt out and is reported as TWA0027.

JavaScript can dispatch too: a plain-markup button whose JavaScript `onclick` calls
`timeWarpState.DispatchRequest("<alias>", { … })` reaches the store with no C# handler (see the
Counter demo, `source/features/counter.ts`). JavaScript dispatch is opt-in: allow the action in
`Program.AllowJavaScriptDispatch` with `Allow<TAction>(alias)`, where the alias is a `const` on the
ActionSet (`IncrementCounterActionSet.JavaScriptAlias`), so the script never spells a CLR type name.
Anything not allowed is rejected and nothing is sent. Use the `fluent-button` web component for
that, not `FluentButton`, whose `OnClick` parameter claims the `onclick` attribute.

Every `*ActionSet.Action` is a Ctrl-K command by derivation: the generated `ActionRoster` lists
it, and the palette shows it when it takes no required constructor argument and the user is signed
in. The label comes from the names (`AgentSurfaceState.OpenAskPanelActionSet` → "Agent surface:
Open ask panel"). Nothing opts an action in. Plumbing (loads, resets, ceremony steps, the palette's
own actions) opts out explicitly with `[CommandPaletteOptOut("reason")]` on the `Action` class; a
blank reason does not opt out, and a guard test fails on one. A label known only at run time (an
admin-set assistant name) comes from a registered `IActionDisplayOverride`, never from a literal
baked into the convention.

When an action is something an agent should run, or the palette needs better copy or a permission,
tag it `[CatalogAction]` with a real `Description`, the same `Permissions` as the page that offers
it, and `Visibility` (`Human`, `Agent`, `Both`). Its `DisplayName`, `Description`, `Visibility` and
`Permissions` override the palette defaults (Human, no permissions, generated label); an
Agent-only entry stays out of the palette. A derived command with no
`[CatalogAction(Permissions = …)]` is offered to every signed-in user, so an action behind a
permission must carry `[CatalogAction(Permissions = …)]` or opt out with
`[CommandPaletteOptOut("reason")]`. An action that takes a URL or path re-validates it in
the handler (for example `LoginPage.GetSafeReturnUrl`): the page is not the only caller.

Reference: `features/identity/sign-in-state/` (ceremonies, challenge navigation, post-sign-in
navigation) and `features/identity/credentials-state/credentials-state.link-microsoft-365.cs`
(a cataloged full-page navigation) under `source/container-apps/web/projects/web-spa/`.

# Server-owned availability

When whether an action applies right now depends on a server rule (last credential, already
linked, state of a record), the server decides and the client reads the answer. Never copy a
server rule into the client.

1. **The server owns availability through typed flags.** Each item in a list carries a typed flag
   per action (`CredentialSummary.CanRevoke`, `CanRename`); a page-level action gets a flag on the
   page's response (`GetCredentials.Response.CanLinkMicrosoft365`). The read handler sets them from
   the same rule code the action's handler enforces. Use typed flags, not a list of allowed action
   names: the compiler checks every read, and there is no string vocabulary to keep in agreement.
   Flags default to `false`, so a missing flag shows no action.
2. **Item actions live on the page.** A row button dispatches its action directly with the row's
   item (`RevokeCredential(id)`), shown or enabled from the row's flag, and the page then refreshes
   the read. The endpoint still enforces the rule (for example a 409 for a stale click), so the
   flag is guidance for an honest client, not the security boundary.
3. **Parameters the user types get a form on the page,** next to the button (Rename's nickname
   field). There is no generic form system.
4. **Ctrl-K and other global entry points get navigation and parameter-free actions only.** An item
   action is reached by going to its page ("revoke a passkey" means opening Passkeys). A
   parameter-free action whose rule may not hold (Link Microsoft 365) can stay a general command:
   its own flow reports when it does not apply. Actions that need an item are cataloged with
   `Visibility = Agent`.
5. **Agents get the Ctrl-K commands whose visibility includes Agent, on every page, plus that
   page's button actions.** Human-only commands stay out. A page-bound command is executable on
   its page and, off that page, returns a navigate offer instead of running. Item actions that
   are not palette commands stay on their page. There is no server-enumerated list of every
   action on every item.

Why: a client copy of a server rule drifts and races (the button shows, the server answers 409),
and every surface that wants the rule needs its own copy. A flag is the server's answer for this
caller and this snapshot, on the data it describes, so the list and its availability never come
from different snapshots. Enumerating every action for every item instead (actions × items) floods
global surfaces like Ctrl-K and needs string agreement between server and client for no benefit
the page does not already have.

Use a flag only where availability needs a server-owned rule. Purely local actions (counter,
theme, toggles, navigation, modals) need none, and static permission checks use
`[CatalogAction(Permissions = …)]` plus `AuthorizeView` or the catalog's permission filter.

Reference: `features/identity/get-credentials/` and `credential-rules-application.cs` (server)
under `source/container-apps/web/`; `features/application/pages/SettingsPage.razor` and
`features/identity/components/CredentialList.razor` (client) under `web-spa/`.

# Agentic UI and WebMCP

One catalog-to-tools mapping drives both the in-app model and an external browser agent.

- `CatalogAgentToolSet.SelectAsync` reads `CommandPaletteRoster` (the Ctrl-K filter). A command
  whose visibility includes Agent is a tool on every page. Human-only commands are never tools.
- The agent knows the whole app, not only the page on screen. `site_map` (read-only, never
  approval-wrapped) returns every destination the person's navigation menu shows: title, URL,
  menu group, icon and the `[Page]` Description, plus Sign in while signed out. The deep links
  Ctrl-K lists under a permitted page (`CommandPaletteRoster.DeepLinks`, the design-system parts)
  follow, each with its `parent` page URL.
  `navigate` takes one of those URLs (a URL enum) and refuses anything else; it goes through
  `RouteState.ChangeRoute` like the palette and asks for approval in Ask before editing. Both read
  `NavigationDestinations`, the list NavMenu and Ctrl-K render, so the four surfaces cannot drift;
  a guard test fails if a route literal or `typeof(…Page)` appears in the agent code. Ask's
  instructions carry the same site map, so it answers "where is X" from Home with a page link.
- A page's own executable actions come from its declaration:
  `[Page(…, AgentActions = [typeof(CredentialsState.AddPasskeyActionSet.Action), …])]`. The
  generator emits `PageRegistry.ActionScopes` (every route template of each such page), and
  `PageAgentScope` matches the current path against them and decides which palette commands only
  execute on their page. There is no hand-written route list. An anonymous principal gets
  `navigate` and `site_map` only.
- Read-only tools are an explicit allow-list of catalog names in `CatalogAgentApproval` (never a
  name-prefix rule). Every other tool is wrapped in `ApprovalRequiredAIFunction`. WebMCP uses the
  same split: the shell's confirmation bar (`AgentSurfaceState`) gates dispatch, one call at a time,
  correlated by call id; navigation cancels a pending page-bound call. A tool that is not page-bound
  is not cancelled only because the path changed. Both drivers re-select tools for the
  current principal and route when a tool runs. A global tool stays offered. A page-only tool is
  refused off its page. A page-bound palette tool off its page returns a navigate offer and does
  not run. Approval
  does not grant a permission the principal lacks.
- The ask UI is the derived Ctrl-K `AgentSurface.OpenAskPanel` command (signed in) and the top-bar
  "Ask {AssistantName}" button (always shown). `AssistantName` is the admin-set `SiteSettings` name
  (default Akili), read anonymously through `GetAssistantIdentity` with its Multiavatar. Every
  assistant label (button, Ctrl-K row via `AskAssistantDisplayOverride`, panel header, greeting,
  feedback titles) and the instructions' first line read `AgentSurfaceState.AssistantName`; never
  hard-code "Ask" or "Ask AI".
  It opens the `AgentAsk` side panel in `TimeWarpPage`'s FluentLayout aside
  (`UIAgent` over a `FunctionInvokingChatClient` whose inner client is the browser relay).
  The page reflows beside the 450px panel. Below 880px, and when expanded, the panel covers
  the viewport. While Ask is open it owns the aside column; a page `Aside` shows only when
  Ask is closed. web-server holds `XAI:ApiKey` and forwards completions to xAI. When the key
  is missing, the panel shows "AI not configured" and the environment's setup hint. Development
  shows the exact `dotnet user-secrets set "XAI:ApiKey" "<your-xai-key>" --id <UserSecretsId>`
  command in the copy block. Every other environment names `XAI__ApiKey` as an environment
  variable or a container-app secret and does not show the user-secrets id. When this host can
  store an admin key, a separate sentence points at Admin → AI. That sentence is not in the copy
  block, and it is omitted when there is no admin storage.

  The template registers the relay, not a key. WebMCP does not need a client.
- WebMCP registers the same tools (including `site_map` and `navigate`), plus `page_context`, through `document.modelContext.registerTool`
  (falling back to `navigator.modelContext`, then `provideContext`). A missing API registers nothing.
  Both paths execute with `ActionCatalogEntry.Execute`. Endpoints keep `[EndpointAuthorize]`.
- Page facts live in `PageAgentContext` on every route: path, title, purpose (the `[Page]`
  Description), a bounded text summary of the page body, and the offered tool names. A state that
  owns the page's `AgentActions` adds its own facts by implementing `IAgentPageFacts` (ids, server
  flags, and the records a replace-whole command must echo, including a Version token); there
  are no per-route cases.
  Components dispatch `SyncWebMcp` and `ResolveApproval`. They do not call the JS module.
- Screenshots: `ScreenshotState.CaptureScreenshot(selector)` is the only way the app draws its own
  page (DOM to PNG, `screenshot.ts`). It is a Ctrl-K command and an approval-gated agent tool.
  Mark anything a picture must never show with `data-no-capture` (password inputs are masked
  already); the capture draws a grey box in its place. Ask's camera button is
  `CaptureForAsk`, one action that captures the same way and attaches that capture to the next
  message; Feedback's Capture this page button is `CaptureForFeedback`, which records the capture
  it took, and the page uploads that capture through the normal attachment action. `page_context.screenshot` holds the
  capture's metadata only, never the picture.

Reference: `web-spa/features/application/agent/` and `web-spa/components/WebMcpAgentSurface.razor`.

# Action handlers and loading

1. A handler does one thing. It never sends/dispatches another action (no `await XState.Y()`
   inside `Handle` / `HandleSuccess` / `HandleError`). Pages and components sequence actions.
2. Loading UI comes from the shared `Section` component
   (`web-spa/components/composites/Section.razor`). Compose Card for chrome; do not duplicate
   border or title styles. It is driven by
   `ActionTrackingState.IsAnyActive(LoadingActionType)`; no hand-written loading markup.
   Grids may bind `FluentDataGrid Loading=IsLoading` to `IsAnyActive(FetchX)` instead.

Reference: `source/container-apps/web/projects/web-spa/features/design-system/samples/SectionSample.razor`
(Section card). Applied on PrincipalsPage, RolesListPage, RoleDetailPage, and the other
former `Loading…` pages.

# Server-sorted FluentDataGrid

A list that the server pages must also be sorted by the server. Sorting one page in the browser
shows the wrong order across pages. The pattern is grid sort → State action → `Query.SortBy`:

1. The query contract carries `SortBy` (a closed enum of the sortable columns) and
   `SortDescending`, with a default that matches the old fixed order. Stores sort the whole
   match set before `Skip`/`Take` and end with an id tiebreaker.
2. The state holds `SortBy` / `SortDescending`. A `SetSort` action changes them and resets
   `Page` to 1. The fetch handler copies them onto the query.
3. The grid gets `Items="@State.Items?.AsQueryable()"` (the current page) and `RefreshItems`.
   With `RefreshItems` set, the grid does not sort or page `Items` itself. `RefreshItems` maps
   the primary sort column (`ColumnId` = the enum name) and direction. When they differ from
   state, it dispatches `SetSort` and then the fetch. Otherwise it returns, so the first render
   and new rows dispatch nothing.
4. Each sortable column sets `Sortable="true"` and a `SortBy` `GridSort` (the grid needs one to
   mark a column sortable, but never runs it). `IsDefaultSortColumn` and `InitialSortDirection`
   follow state, so a re-created grid shows the stored sort.
5. `Loading` binds to `IsAnyActive(typeof(FetchX.Action))`. The grid's `Section` has no
   `LoadingActionType`: `Section` swaps its content for a spinner, which would destroy the grid.
6. Enum and date cells use `TemplateColumn` (`@context.Kind`). `PropertyColumn` renders enums
   lowercased. Every rendered date is ISO 8601 (TWA0036): `yyyy-MM-dd` for a date, and
   `yyyy-MM-dd'T'HH:mm:sszzz` (local) or `yyyy-MM-dd'T'HH:mm:ss'Z'` (UTC) for an instant.
7. Narrow widths: wrap the grid in `DataGridFrame` and size columns with
   `DisplayMode="DataGridDisplayMode.Grid"` plus `GridTemplateColumns` of `minmax` tracks. The
   primary column (the one with the row link) gets `minmax(14rem, 2fr)`; the others get smaller
   minimums and `fr` shares. No column sets `Width`: fixed widths leave an unsized column the
   leftover, which is zero at a narrow viewport or with the Ask panel open. Below the minimum
   total, the grid scrolls inside the frame and header text ends in an ellipsis. The integration
   test `DataGridFrameGuard` enforces this for every `FluentDataGrid`.

Reference: `web-spa/features/admin/feedback/pages/FeedbackAdminListPage.razor` with
`ListFeedback.Query` and `FeedbackAdminState.SetSort`; `web-spa/components/composites/DataGridFrame.razor`.

# Operation outcomes

No page, card, or feature component renders its own message bar for an operation outcome
(success or failure) — the shell's single notification region owns that (`tw-blazor-layout`, "The
shell owns the notification region"). Components and handlers only *report* outcomes:

- A component reports directly with the generated state methods `AddNotification(intent, title,
  body)` for a success/info message it composes itself, or `ReportProblem(problem)` when it already
  holds a `SharedProblemDetails`.
- A handler never dispatches another action; it publishes `OutcomeNotification(intent, title,
  body)` or `ProblemDetailsNotification(problem)` instead. `DefaultApiHandler` already publishes
  `ProblemDetailsNotification` on failure, so most failure paths need no extra code — only the
  success sentence is yours to add.
- One shape: `Intent`, `Title`, optional `Body`. For a `SharedProblemDetails`, `Title` is the
  problem's `Title` and `Body` is its `Detail` — never a generic "Error"/"Success" title, and never
  `Title: Detail` glued into one string. Success bars carry the operation's own sentence
  ("Passkey added.").
- Identical messages (same `Intent`, `Title`, `Body`) replace the existing bar instead of stacking.
- Field-level validation is a different class — it stays next to the field (`ValidationMessage`
  via Blazilla), never in the notification region.
- Static, contextual guidance that isn't an outcome (an inline Info/Warning such as "Add a passkey
  to continue") may stay inline. The analyzer `TWA0025` flags an `Error`/`Success` message bar
  outside the shell's host; opt out with `[PageLocalMessageBar("reason")]` on the component when a
  case is genuinely local.
- Every bar, shell or inline, is the shared `MessageBar` (`components/MessageBar.razor`), never a
  raw `FluentMessageBar` (TWA0037). It paints an icon-only Copy button that copies `Title`, plus a
  blank line and `Body` when there is one. Pass the text as `Title` and `Body`; `ChildContent`
  (such as an input) is not copied, and `ActionsTemplate` buttons sit beside Copy.

```razor
<MessageBar Intent="MessageBarIntent.Warning"
            Title="Session expired"
            Body="Sign in with Microsoft 365 again." />
```

Reference: `source/container-apps/web/projects/web-spa/components/MessageBars.razor` (host),
`components/MessageBar.razor` (the bar), and `features/notification/notification-state/` (state).

# Actions, navigation, and forms

Actions are `FluentButton` with a design system appearance (Primary / Outline / Subtle /
Transparent; danger via `--twe-danger`). Navigation is `FluentAnchor`; the menu is `NavMenu` over the page registry.
Persisting a model is `EditForm` + `FluentButton` submit (RoleForm). Never raw `<button>` or
page-local button/link classes; reuse `components/elements` and existing feature components
before writing markup. Multi-line text is the shared `TextArea` (`components/forms/TextArea.razor`),
never a raw `FluentTextArea` (TWA0032; opt out with `[DirectFluentTextArea("reason")]`).

Reference appearances: `source/container-apps/web/projects/web-spa/features/design-system/samples/ButtonsSample.razor`
(Primary, Outline, Subtle, Transparent, and Outline + `Class="twe-button-danger"`).

Form submit reference: `source/container-apps/web/projects/web-spa/features/admin/roles/components/RoleForm.razor`
(`EditForm` + `OnValidSubmit` + `FluentButton Type=ButtonType.Submit Appearance=ButtonAppearance.Primary data-qa="RoleSave"`).

# Forms

Forms use `FormSection` / `FormField` / `FormGrid` / `FormActions` from `components/forms`.
Controls are full width by default (a field takes the whole row unless `FormField.Span` pairs
short fields such as City / State / ZIP). Spacing via the `--twe-space-*` tokens
(`--twe-space-2` label→control, `--twe-space-6` field row gap, `--twe-space-12` section gap,
`--twe-space-3` action gap). No page-local margins.

Reference: `source/container-apps/web/projects/web-spa/features/design-system/samples/FormsSample.razor`
(Forms card). Applied on ProfilePage, RoleForm, and AuthenticationPage.

# Browser console logs in the Aspire dashboard

In Development and Testing the host page (`App.razor`) loads `js/browser-log-forwarder.js` before
`_framework/blazor.web.js`. The script forwards console errors and warnings, uncaught errors, and
unhandled promise rejections to `POST api/browser-logs`; web-server relays them into its
structured logs under category `Web.Spa.Browser`, so they show in the Aspire dashboard on the
web-server resource. A plain script is required because boot failures happen before the .NET
runtime starts, so a WASM `ILoggerProvider` cannot see them. The gate is
`BrowserLogForwarding.IsEnabled(IHostEnvironment)`: Production never loads the script and the
endpoint answers 404. Entries are rate limited and bearer tokens / JWTs are redacted.

Symptom: a Mono assertion such as `metadata/assembly.c ... assertion` in the browser console (now
visible in the dashboard) means stale `_framework` assets. Fix: `dev clean`, rebuild, then clear
site data (DevTools > Application > Clear site data) or hard refresh.

Dashboard history outlives the AppHost. The AppHost-launched dashboard (Aspire 13.6+) keeps
resource snapshots and telemetry for the last 10 runs on disk (SQLite) by default
(`Aspire:Dashboard:PersistenceMode` = `Run`, set by Aspire.Hosting when unset). After stopping
the AppHost, reopen the dashboard and pick the earlier run to read its browser errors and logs;
pin a run to keep it past the 10-run window. Aspire.Hosting.Testing suites start no dashboard, so
test runs never consume that history.

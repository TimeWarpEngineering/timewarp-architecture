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

Why: an action is the one seam that the action catalog (`[CatalogAction]` — the Ctrl-K palette
and agent tools), Redux DevTools, and headless tests all see. A method on a page is invisible to
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

When an action is something a person or agent would meaningfully run on its own, tag it
`[CatalogAction]` with a real `Description`, the same `Permissions` as the page that offers it,
and `Visibility` (`Human`, `Agent`, `Both`). An action that takes a URL or path re-validates it in
the handler (for example `LoginPage.GetSafeReturnUrl`): the page is not the only caller.

Reference: `features/identity/sign-in-state/` (ceremonies, challenge navigation, post-sign-in
navigation) and `features/identity/credentials-state/credentials-state.link-microsoft-365.cs`
(a cataloged full-page navigation) under `source/container-apps/web/projects/web-spa/`.

# Server-offered actions

When whether an action is valid right now depends on a server rule (last credential, already
linked, state of a record), the server says so: never compute validity on the client when the
server can offer it. The read the page already loads returns the actions valid now as
`{ name, label, subject, arguments }`, where `name` is a client `[CatalogAction]` name and
`arguments` are keyed by that action's constructor parameter names. The server computes the
offers from the same rule code its handlers enforce, and builds each one from a typed offer record
in the shared contracts, so it never spells a name or an argument key by hand.

The client stores the offers in the feature state with the data they describe, renders a button
only for an offer, and runs it through the catalog: `Find` the name, check the entry is
human-visible (`Human` / `Both`) and its `Permissions` pass `IAuthorizationService`, bind
`arguments` (plus user input for parameters the offer left unbound — input can never replace a
bound argument), then `Execute`. The page then refreshes the read the offer came from as a
separate, parameterless cataloged action run by the caller, never by the handler. An unknown name,
a failed check or a failed binding is refused with a notification, and nothing runs. The same rows
are the page's contextual Ctrl-K rows, except those that need input.

Why: a client copy of a server rule drifts and races (the button shows, the server answers 409),
and every surface that wants the rule needs its own copy. An offer is the server's answer for this
caller and this snapshot. The catalog stays the allow-list: the server can only ask for an action
the client already ships, and the endpoint still enforces the rule. Offers name actions rather than
URLs so the real action runs, with its state updates and notifications, and so a response can never
aim the user's token at an arbitrary route.

## When to offer

- An offer is not a different kind of action. It is the server saying "you may run this action
  now, with these arguments"; the action stays a normal TimeWarp.State action that pages, the
  palette and agents can also run directly, and its endpoint still enforces the rule.
- Offer an action when deciding whether it is available needs a server-owned rule: server data,
  other users' actions, or a business invariant. Examples: revoking a credential that is not the
  last one, linking Microsoft 365 when the site allows it and the account is not linked yet,
  approve / refund / cancel depending on a record's state, editing only what the caller owns. The
  test: if the client would have to copy a server rule to decide whether to show the button, use an
  offer.
- Use a plain action for purely local or UI actions (counter, theme, toggles, navigation, modals);
  for static permission checks, where `[CatalogAction(Permissions = …)]` plus `AuthorizeView` or the
  catalog's permission filter is enough; and for reads — a fetch is never offered.
- Anything an agent should run only when the server allows it should be an offer, so the agent's
  tool calls are server-checked the same way the buttons are.

Why: offers cost a server rule, a record and a refresh round-trip. They pay off only where the
client cannot know the answer; for local or statically-permitted actions they add latency and a
second place to look without removing any drift.

## How to add an offerable action

1. Add the catalog name constant to the slice's offered-names class in the shared contracts.
2. Add a typed offer record beside it, tagged `[ActionOffer(<constant>)]`. Its public properties
   are the arguments the server binds and must match the action's first constructor's parameters
   by camelCase name and type. A nullable property cannot feed a required parameter (the client
   treats null as missing). List any required parameter the user supplies (a nickname, a comment)
   in `UserInput`. Optional parameters may be left out only at the end: binding one after an
   optional parameter that is omitted, or fed by a nullable property, is refused.
3. Set `[CatalogAction(Name = <constant>)]` on the client action, so renaming the action set cannot
   change the offered name.
4. Have the server build offers only from the record (`OfferedAction.ForCredential` / `ForPage` /
   `Create`).

The SPA build checks the pairing: TWA0029 when no action, or more than one, declares that `Name`;
TWA0030 when the record's properties or `UserInput` do not match the action's parameters. Why: the server cannot
reference client action types, so the shared contracts are the one place both sides can agree, and
a build error replaces a run-time refusal the user would see.

Reference: `features/identity/get-credentials/` and `credential-offers-application.cs` (server),
`credential-action-offer-contracts.cs` (offer records),
`credential-offer-rows.cs` and `credentials-context-source.cs` (client) under
`source/container-apps/web/`; the runner is
`web-spa/features/application/command-palette/command-palette-runner.cs`.

# Action handlers and loading

1. A handler does one thing. It never sends/dispatches another action (no `await XState.Y()`
   inside `Handle` / `HandleSuccess` / `HandleError`). Pages and components sequence actions.
2. Loading UI comes from the shared `Section` component
   (`web-spa/components/composites/Section.razor`, port of COPIC `CopicSection`) driven by
   `ActionTrackingState.IsAnyActive(LoadingActionType)`; no hand-written loading markup.
   Grids may bind `FluentDataGrid Loading=IsLoading` to `IsAnyActive(FetchX)` instead.

Reference: `source/container-apps/web/projects/web-spa/features/style-guide/pages/StyleGuidePage.razor`
(Section card). Applied on PrincipalsPage, RolesListPage, RoleDetailPage, and the other
former `Loading…` pages.

# Operation outcomes

No page, card, or feature component renders its own `FluentMessageBar` for an operation outcome
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
  to continue") may stay inline. The analyzer `TWA0025` flags an `Error`/`Success` `FluentMessageBar`
  outside the shell's host; opt out with `[PageLocalMessageBar("reason")]` on the component when a
  case is genuinely local.

Reference: `source/container-apps/web/projects/web-spa/components/MessageBars.razor` (host) and
`features/notification/notification-state/` (state).

# Actions, navigation, and forms

Actions are `FluentButton` with a style-guide appearance (Primary / Outline / Subtle /
Transparent; danger via `--twe-danger`). Navigation is `FluentAnchor` / `TimeWarpNavLink`.
Persisting a model is `EditForm` + `FluentButton` submit (RoleForm). Never raw `<button>` or
page-local button/link classes; reuse `components/elements` and existing feature components
before writing markup.

Reference appearances: `source/container-apps/web/projects/web-spa/features/style-guide/pages/StyleGuidePage.razor`
(Primary, Outline, Subtle, Transparent, and Outline + `Class="twe-button-danger"`).

Form submit reference: `source/container-apps/web/projects/web-spa/features/admin/roles/components/RoleForm.razor`
(`EditForm` + `OnValidSubmit` + `FluentButton Type=ButtonType.Submit Appearance=ButtonAppearance.Primary data-qa="RoleSave"`).

# Forms

Forms use `FormSection` / `FormField` / `FormGrid` / `FormActions` from `components/forms`.
Controls are full width by default (a field takes the whole row unless `FormField.Span` pairs
short fields such as City / State / ZIP). Spacing via the `--twe-space-*` tokens
(`--twe-space-2` label→control, `--twe-space-6` field row gap, `--twe-space-12` section gap,
`--twe-space-3` action gap). No page-local margins.

Reference: `source/container-apps/web/projects/web-spa/features/style-guide/pages/StyleGuidePage.razor`
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

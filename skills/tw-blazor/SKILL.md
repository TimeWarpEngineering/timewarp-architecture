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
methods, and at most sequences several of them (`await CredentialsState.RevokeCredential(id);
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
not an interaction; prefer an action there too, since it keeps the page free of services.

When an action is something a person or agent would meaningfully run on its own, tag it
`[CatalogAction]` with a real `Description`, the same `Permissions` as the page that offers it,
and `Visibility` (`Human`, `Agent`, `Both`). An action that takes a URL or path re-validates it in
the handler (for example `LoginPage.GetSafeReturnUrl`): the page is not the only caller.

Reference: `features/identity/sign-in-state/` (ceremonies, challenge navigation, post-sign-in
navigation) and `features/identity/credentials-state/credentials-state.link-microsoft-365.cs`
(a cataloged full-page navigation) under `source/container-apps/web/projects/web-spa/`.

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

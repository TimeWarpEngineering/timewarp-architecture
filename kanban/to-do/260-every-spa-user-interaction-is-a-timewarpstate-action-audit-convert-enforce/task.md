# Every SPA user interaction is a TimeWarp.State action (audit, convert, enforce)

## Description

Rule (Steve, 2026-10-01): **all user interactions in web-spa are TimeWarp.State actions.** A
component event handler (`OnClick`, `OnSubmit`, `@onkeydown`, …) may only dispatch a generated
ActionSet method, and at most sequence several of them. The work itself lives in the action's
handler: navigation, full-page redirects to BFF endpoints, JS interop, API calls, and state
changes. Only purely presentational local UI state is exempt: hover, focus, whether a local
details panel is expanded, and binding the text in an input box.

Why: actions are the one seam that the action catalog (the Ctrl-K palette, and agents via
WebMCP later), Redux DevTools, and headless tests can all see. A method on a page is invisible to
all of them. Trigger case: **Link Microsoft 365** is a `SettingsPage` method that does a
forceLoad navigation to the BFF challenge (`SettingsPage.razor.cs`, RFC 219 D10), so it never
appears in the palette.

## Requirements

1. **Audit.** Walk every `.razor` / `.razor.cs` under `source/container-apps/web/projects/web-spa`
   and list each event handler that does more than dispatch actions. Look for
   `NavigationManager.NavigateTo`, `forceLoad`, `IJSRuntime` / JS modules, `HttpClient` / API
   services, and direct state mutation. Record the list in Notes with file:line and a
   disposition (convert / exempt-with-reason).
2. **Convert** every non-exempt interaction to an action on the owning feature's State. Page
   methods become thin dispatches.
   - **Link Microsoft 365** becomes `CredentialsState.LinkMicrosoft365`. Its handler performs the
     same forceLoad challenge navigation (`mode=link`, same return URL behavior).
   - Keep the existing sequencing rules (handlers do not nest dispatch; components sequence
     several actions), TWA0022 (no direct mediator `Send`), and TWA0025 (outcomes go through
     NotificationState).
3. **Catalog.** Add `[CatalogAction]` to converted actions that a person or agent would
   meaningfully run: Human / Agent / Both, the same permissions as the page gate, and a real
   Description. Link Microsoft 365 is `Visibility = Human`, `Permissions = CredentialManageSelf`.
   It is always listed for principals with that permission. When it doesn't apply (already
   linked, or Entra sign-in disabled site-wide), the challenge flow reports that. A catalog
   "is this available now" predicate is out of scope; note any case that would want one.
4. **Enforce.** Propose, in the task, an analyzer shaped like TWA0022 (gated on the Blazor WASM
   SDK) that flags razor event handlers or component methods calling `NavigationManager`,
   `IJSRuntime` or API clients directly, with an opt-out attribute that requires a reason.
   **Stop before implementing the analyzer.** Record the proposed rule, its false-positive risks
   and the opt-out shape, then leave it as an open question for Steve. Steve decides
   architecture rules.
5. **Skill.** State the rule, with its reasoning, in the owning SPA skill. Use the one that
   covers state/actions and pages (check `skills/`). Skills are public: write the rule and its
   reasoning only, no history.

## Checklist

- [x] Audit list recorded in Notes (file:line, disposition)
- [x] Link Microsoft 365 → `CredentialsState.LinkMicrosoft365` + `[CatalogAction]`; Settings
      button dispatches it; it appears in the Ctrl-K palette
- [x] All other non-exempt interactions converted; Purpose/Design regions reconciled
- [x] `[CatalogAction]` on converted user-meaningful actions
- [x] Analyzer proposal written up as an Open Question (not implemented)
- [x] Skill updated with the rule
- [x] Tests: co-located Jaribu or the existing web-spa integration suites. Each converted action
      is dispatched headless and asserts its effect (navigation target, state change); the
      catalog lists Link Microsoft 365 with the right visibility and permission
- [x] Gates: `dev build` 0/0, `dev test`, `dev template-smoke` (the template ships these pages),
      `ganda repo audit`
- [x] Do **not** start an AppHost (`dev run`, `aspire run`, or `dotnet run` of the AppHost);
      record the browser check as not performed
- [ ] Implementation review; host `open-pr`

## Session

- Created: 512235 (2026-09-30)
- 2026-10-01 implement (implementer-claude, headless `ganda task work`): audit, conversions,
  catalog, tests, skill, analyzer proposal. Conversion stayed on this task (no children): the
  non-exempt set is one Identity sign-in cluster plus four small edits.

## Notes

- If the audit finds many interactions, split conversion into children by feature (for example
  settings/credentials, admin, application shell) and keep this task as the parent. Parent-done
  requires every child done.
- Memory discipline: run builds and tests serially and call `dotnet build-server shutdown`
  before finishing.

### Audit (web-spa `.razor` / `.razor.cs` event handlers doing more than dispatch)

Line numbers are pre-change (commit 97278a59). Paths under
`source/container-apps/web/projects/web-spa/`.

| # | Handler (file:line) | Did | Disposition |
|---|---------------------|-----|-------------|
| 1 | `features/application/pages/SettingsPage.razor:112` `LinkMicrosoft365` | `NavigationManager.NavigateTo(challenge mode=link, forceLoad)` | **Converted** → `CredentialsState.LinkMicrosoft365` (+ `[CatalogAction]` Human, `CredentialManageSelf`) |
| 2 | `features/identity/pages/login-page/LoginPage.razor:70` `ContinueWithPasskey` | `PasskeyCeremonyClient.AuthenticateAsync` + `NavigateTo(returnUrl)` + page-local notifications | **Converted** → `SignInState.SignInWithPasskey(returnPath)` |
| 3 | `features/identity/pages/login-page/LoginPage.razor:94` `CreatePasskey` | `Ceremony.RegisterAsync` + `NavigateTo(returnUrl or /Settings)` | **Converted** → `SignInState.CreateAccountWithPasskey(returnPath)` |
| 4 | `features/identity/pages/login-page/LoginPage.razor:154` `ContinueWithMicrosoft365` | `NavigateTo(challenge mode=bootstrap, forceLoad)` | **Converted** → `SignInState.SignInWithMicrosoft365(returnPath)` |
| 5 | `features/identity/pages/microsoft-365-choose-page/ChooseMicrosoft365Page.razor:56` `CreateNewAccount` | `IWebServerApiService` CompleteEntraBootstrapCreate + auth provider notify + `NavigateTo` | **Converted** → `SignInState.CreateAccountFromMicrosoft365` |
| 6 | `…/ChooseMicrosoft365Page.razor:97` `AlreadyHaveAccount` | `Ceremony.CompleteEntraChoiceExistingAsync` + `NavigateTo` | **Converted** → `SignInState.UseExistingAccountForMicrosoft365` |
| 7 | `…/ChooseMicrosoft365Page.razor:147` inline "Sign in with Microsoft 365 again" | `NavigationManager.NavigateTo(/Login)` | **Converted** → `RouteState.ChangeRoute` |
| 8 | `features/identity/pages/passkeys-page/PasskeysPage.razor:26` `RegisterPasskey` | `Ceremony.RegisterAsync` + session re-read via API | **Converted** → `SignInState.CreateAccountWithPasskey(StayOnPage)`, then page sequences `FetchCredentials` + `SetPendingNickname` |
| 9 | `…/PasskeysPage.razor:62` `AuthenticateWithPasskey` | `Ceremony.AuthenticateAsync` + session re-read | **Converted** → `SignInState.SignInWithPasskey(StayOnPage)` |
| 10 | `features/identity/components/AddPasskeyPrompt.razor:97` `DismissAsync` | `ISessionStorageService.SetItemAsync` (JS-backed browser storage) | **Converted** → `CredentialsState.DismissPasskeySoftPrompt(rememberForSession: true)` writes storage in the handler |
| 11 | `features/admin/roles/components/RoleForm.razor:27` `HandleValidSubmit` | `NavigationManager.NavigateTo(RoleDetailPage)` after `CreateRole` | **Converted** → sequences `RouteState.ChangeRoute` |
| 12 | `features/to-do/components/TodoItemFormContainer.razor:7` `Back` | `NavigationManager.NavigateTo("/")` | **Converted** → `RouteState.ChangeRoute` |
| 13 | `features/counter/pages/CounterPage.razor:9` `IncrementCountViaJsInterop` | `IJSRuntime.InvokeVoidAsync("Spa.Counter.DispatchIncrementCountAction")` | **Exempt** — the demo's subject is JS dispatching an action into the store; the JS call *is* the dispatcher. Moving it into a handler would make a handler dispatch (nested dispatch is banned). Open question below |
| 14 | `features/application/modals/command-palette/CommandPalette.razor:175/180` focus restore / scroll-into-view | `IJSObjectReference` calls | **Exempt** — presentational focus/scroll |
| 15 | `…/CommandPalette.razor:129` `RunAsync` | `CloseModal` then `CommandPaletteRunner.RunAsync` (catalog `Execute` / `RouteState`) | **Exempt** — already dispatches; the runner is the catalog dispatcher |
| 16 | `features/chat/pages/ChatPage.razor:20` `SendMessage` | dispatches `ChatState.SendMessageToServer`, clears `Message` | **Exempt** — clearing the bound input is local input state |
| 17 | `features/profiles/pages/ProfilePage.razor:58` `HandleCancel` | resets form draft from state | **Exempt** — input binding |
| 18 | `components/MessageBars.razor:33` `ToggleShowAll`, `features/debugger/components/ServiceList.razor:13` `ToggleCollapse`, `features/identity/components/CredentialList.razor:96–140` Begin/Cancel rename/revoke | local expand/editor flags | **Exempt** — presentational local UI state |
| 19 | All other handlers (Profile menu, HomePage, Counter, TestPage, TimeWarpPage, ModalContainer, StyleGuidePage, PrincipalsPage, RolePermissionEditor, RoleDetailPage, AuthenticationPage, AgentLinksPage, Settings passkey buttons) | already dispatch / sequence actions only | Conforming |

Lifecycle (not interactions; noted because the proposed analyzer would see them):
- Converted along the way so the pages carry no API service: Login/Settings "Microsoft 365
  offered" load → `SignInState.FetchMicrosoft365Offered`; Login/Passkeys session read →
  `SignInState.FetchSession`; Choose page validity peek → `SignInState.FetchMicrosoft365Choice`.
- Left as-is: `LoginPage` already-signed-in redirect (`NavigationManager.NavigateTo` in
  `OnInitializedAsync`, runs during prerender — a `RouteState` dispatch there would route a
  `NavigationException` through the pipeline); `RedirectToLogin.razor` (render-time forceLoad
  redirect); `AddPasskeyPrompt` restoring the dismissal from sessionStorage on first interactive
  render; `ChatPage` hub connect. `Profile.razor`'s unused `NavigationManager` / `HttpClient`
  injects were removed (no calls).

Catalog: `Credentials.LinkMicrosoft365` is the only new `[CatalogAction]`. The `SignInState`
ceremonies are not cataloged: they run for signed-out visitors (the palette offers commands only
to signed-in principals and has its own Sign in row), need a browser authenticator, and agents
authenticate with agent keys. Cases that would want a catalog "available now" predicate:
`LinkMicrosoft365` (offered AND no active EntraAccount — `CredentialsState.CanLinkMicrosoft365`);
`AddExistingPasskey` / `AddPasskey` under site policy. Today Link is listed for every principal
with `credential.manage.self`; the challenge answers 404 (scheme not registered) / 403 (site
policy off) as a full-page problem response.

## Open Questions

### Enforcement analyzer — proposed, not implemented (Steve decides)

**Proposed rule (TWA00xx, next free id):** in SPA client code (gated on
`UsingMicrosoftNETSdkBlazorWebAssembly`, same gate as TWA0022/TWA0025; razor-generated trees
analyzed), report an invocation inside a type deriving from `ComponentBase` whose receiver or
target is one of:
- `NavigationManager.NavigateTo` / `NavigateToLogin` / `Refresh`
- `IJSRuntime` / `IJSObjectReference` `InvokeAsync` / `InvokeVoidAsync`
- `IApiService` and subtypes (`IWebServerApiService`, `IApiServerApiService`) `GetResponse`
- `HttpClient` send/get/post members
- `ISessionStorageService` / `ILocalStorageService` writes
- first-party ceremony / JS-module services (`PasskeyCeremonyClient`, `*JsModule` statics) —
  either by a list or by a marker attribute on those services

Handlers (`BaseHandler<T>` / `ActionHandler` descendants), services and non-component types are
out of scope — that is where the work belongs. Message: "Component calls {member} directly;
dispatch a TimeWarp.State action whose handler does it."

**Opt-out shape:** `[DirectComponentSideEffect("reason")]` (name open) on the component class or
the method; the reason string is required and non-empty (a second diagnostic for an empty
reason, like `[CrossSliceReference]`).

**False-positive risks:**
1. Lifecycle methods (`OnInitializedAsync`, `OnAfterRenderAsync`): render-time redirects
   (`RedirectToLogin`, Login's already-signed-in redirect during prerender) and presentational JS
   (focus, scroll, hotkey registration in `CommandPalette`). Options: exempt lifecycle overrides
   entirely (rule = event handlers only, harder to define — handler-ness is only visible from
   markup bindings), or flag them and let the opt-out carry the reason.
2. Presentational JS (focus / scroll / measure) is indistinguishable from side-effect JS by
   type; every such component needs the opt-out.
3. Demo components whose subject is interop (`CounterPage` JS → dispatch).
4. Dispatcher infrastructure that legitimately takes these services (`CommandPaletteRunner` is a
   static helper, not a component — not flagged; `ChatHubConnection` is a service — not flagged).
5. Detecting "handler" by markup binding would need the razor-generated tree's
   `EventCallback.Factory.Create` arguments — feasible (TWA0022 already walks razor trees) but
   adds complexity; flagging any component method is simpler and noisier.

Open question for Steve: adopt the rule? Scope = all component members, or event-handler
methods only (via `EventCallback.Factory.Create` targets)? Opt-out attribute name?

### CounterPage JS-interop demo

Keep it exempt (the demo shows JS dispatching into the store), or reshape the demo so JS is
called from an action handler that does NOT dispatch (e.g. JS returns a number, the handler adds
it)? The current shape cannot move into a handler without nested dispatch.

### Generator note

The TimeWarp.State ActionSet method generator drops nullable annotations on action constructor
parameters (`string? x = null` emits `string x = null`, CS8625/CS8604 under nullable), and only
emits one method per action. `SignInState` uses `StayOnPage = ""` instead of a nullable return
path for that reason. Worth an upstream timewarp-state issue.

## Results

Every web-spa interaction that did more than dispatch is now a TimeWarp.State action; pages
dispatch and read.

- **New `SignInState`** (`features/identity/sign-in-state/`): `FetchSession`,
  `FetchMicrosoft365Offered`, `FetchMicrosoft365Choice`, `SignInWithPasskey`,
  `CreateAccountWithPasskey`, `SignInWithMicrosoft365`, `CreateAccountFromMicrosoft365`,
  `UseExistingAccountForMicrosoft365`. Handlers own the ceremony, BFF calls, forceLoad challenge,
  post-sign-in navigation (return path re-collapsed by `LoginPage.GetSafeReturnUrl` in the
  handler) and outcome publishing. Ceremony actions are `[TrackAction]`; pages bind busy to
  `ActionTrackingState`.
- **`CredentialsState.LinkMicrosoft365`** — `[CatalogAction]` Human, `credential.manage.self`;
  handler forceLoads `/api/identity/entra/challenge?mode=link&returnUrl=%2FSettings`. Settings
  button dispatches it; it is a Ctrl-K palette command ("Credentials: Link microsoft 365").
- **`DismissPasskeySoftPrompt(rememberForSession)`** writes the sessionStorage key in the
  handler.
- LoginPage, ChooseMicrosoft365Page, PasskeysPage, SettingsPage no longer inject
  `IWebServerApiService` / `PasskeyCeremonyClient` (Login keeps `NavigationManager` only for its
  lifecycle redirect); RoleForm and TodoItemFormContainer navigate via `RouteState.ChangeRoute`.
- Palette display name splits a digit run into its own word (`LinkMicrosoft365` →
  "Link microsoft 365").
- Skill `tw-blazor`: new "User interactions are actions" section (rule, reasoning, exemptions,
  catalog guidance).
- Tests: `web-spa-integration-tests/features/identity/sign-in-state-tests.cs` (7 facts, headless:
  Link/bootstrap challenge targets + forceLoad, unsafe return collapsed, offered flag + fail
  closed, Microsoft 365 create success navigation, expired problem → choice invalid + no
  navigation + notification, passkey ceremony failure → CeremonyFailed + no navigation +
  notification, Later persistence only when asked); `action-catalog-tests` roster +
  LinkMicrosoft365 visibility/permission; `command-palette-tests` roster + display name.
- Gates: `dev build` 0 warnings / 0 errors; `dev test` all suites passed (1 pre-existing skip in
  web-server-integration-tests); `dev template-smoke` SUCCEEDED; `ganda repo audit` clean.
- Browser check: **not performed** (no AppHost started, per brief).

### How to validate

**Smoke:**

```bash
cd tests/container-apps/web/web-spa-integration-tests
dotnet test -c Release -- --filter-class SignInActions_Should_
dotnet test -c Release -- --filter-class ActionCatalog_Should
dotnet test -c Release -- --filter-class CommandPalette_Should_
grep -rnE 'NavigationManager|IWebServerApiService|PasskeyCeremonyClient|IJSRuntime' \
  source/container-apps/web/projects/web-spa --include='*.razor' | grep -v '/obj/'
```

**Expect:**

- `SignInActions_Should_`: 7/7 passed; `ActionCatalog_Should` and `CommandPalette_Should_` pass
  with `Credentials.LinkMicrosoft365` in the roster (Human, `credential.manage.self`).
- The grep lists only the exempt/lifecycle uses from the audit table: `LoginPage.razor`
  (`NavigationManager` lifecycle redirect), `RedirectToLogin.razor`, `CounterPage.razor`
  (`IJSRuntime` demo), `CommandPalette.razor` (`IJSRuntime` focus/scroll). No `SettingsPage`, `ChooseMicrosoft365Page`,
  `PasskeysPage`, `RoleForm` or `TodoItemFormContainer` hits.
- In a browser (manual, not performed here): Settings → "Link Microsoft 365" and Ctrl-K →
  "Credentials: Link microsoft 365" both leave for the Entra challenge with
  `mode=link&returnUrl=%2FSettings`.

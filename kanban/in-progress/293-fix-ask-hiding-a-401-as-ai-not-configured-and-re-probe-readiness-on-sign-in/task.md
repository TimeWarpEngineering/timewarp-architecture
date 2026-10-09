# Fix Ask hiding a 401 as AI not configured and re-probe readiness on sign-in

## Description

Steven reported on 2026-10-09 (22:52 ICT): on TWE-001 (Development, https://arch.timewarp.work)
the Ctrl-K Ask surface said **"AI not configured"** and printed the `dotnet user-secrets set` command,
although `XAI:ApiKey` **is** set in web-server's user-secrets (id
`0e53fdd3-6f93-4d5a-9c86-040621f7929e`, verified present). The browser console showed:

```text
api/agent-chat/configuration:1  Failed to load resource: the server responded with a status of 401 ()
... HttpClient.web-server ... GET https://arch.timewarp.work/api/identity/session ... 200
```

Two bugs, both ours:

1. **The 401 was masked.** `AgentSurfaceState.LoadChatConfiguration`'s `HandleError` and
   `HandleFileResponse` both called `MarkNotConfigured()`, so a 401, a 500, a network failure or a
   cancelled request all rendered "AI not configured" plus the key command. The user was told to set
   a key that was already set, and the real status was thrown away.
2. **The readiness probe ran once per app lifetime and never re-ran on sign-in.**
   `TimeWarpPage.OnInitializedAsync` dispatched the probe only while `ChatProbeCompleted` was false.

Steven's direction: fix the root cause, show the real state, do not hide or disable anything.

### Root cause (code reading 2026-10-09; Playwright proof is in Results)

- The configuration endpoint is `[EndpointAuthorize(Policy = identity-session-authenticated,
  AuthenticationSchemes = identity-session, mock-identity-session)]`. That is correct: the
  identity-session cookie is the only browser session (Entra is a named challenge that ends in
  `IBrowserSessionService.IssueAsync(identity-session)`; RFC 219 D10).
- The WASM `web-server` HttpClient already sends the cookie (`BrowserRequestCredentialsHandler`,
  SameOrigin), and `/api/identity/session` uses the same client. So (b) wrong scheme and (c) missing
  credentials are **ruled out**; a box Playwright run with a real passkey-issued identity-session
  cookie present at page load gets `200` from `api/agent-chat/configuration` in both InteractiveAuto
  (server circuit) and InteractiveWebAssembly, and Ask is configured.
- Cause (a): the probe fires on the first `TimeWarpPage` render. The home page is anonymous by
  design, so a signed-out visitor's probe gets an honest 401 and the old code stored NotConfigured.
  Sign-in then happens **inside the SPA without a reload**: Home "Sign in" → `ChangeRoute(/Login)` →
  `SignInWithPasskey` / `CreateAccountWithPasskey` / Entra bootstrap choices → `NotifySessionChanged()`
  (that is the `api/identity/session` 200 after the 401 in Steven's log) → `NavigateTo(returnUrl)`
  without `forceLoad`. `ChatProbeCompleted` was already true, so Ask kept the stale anonymous
  answer for the rest of the app's life, now worded as "no key".

## Requirements

- Readiness distinguishes **Configured**, **NotConfigured** (the server answered 200 and said no
  key), **Unauthenticated** (401: "Sign in to use Ask" with a sign-in button), and **Error** (any other
  failure: show the status, title and detail, plus Retry). The user-secrets command shows **only**
  for NotConfigured.
- The probe re-runs whenever `AuthenticationState` changes (sign-in, sign-out, session refresh), from
  the existing `AuthenticationStateListener`, which already reloads profile, credentials and site
  settings on the same event.
- The completion relay (`RelayChatClient`) reports the real status/title/detail on failure instead of
  a generic "The model request failed."
- No feature is disabled, nothing is hidden, the endpoint's auth stays as it is.

## Checklist

- [ ] Real-WASM Playwright test: anonymous Home → Ask, then in-SPA passkey sign-in (CDP virtual
      authenticator) → Ask. Fails on master (stale "AI not configured" after a real 401), passes after.
- [ ] Unit tests for the readiness mapping (200 configured / 200 no key / 401 / 403 / 500 / 499 /
      file response) and for the Ask markup per state
- [ ] `CatalogAgentReadiness` gains Unauthenticated and Error; `AgentSurfaceState` keeps the problem text
- [ ] `LoadChatConfiguration` maps outcomes through one pure function; no failure becomes NotConfigured
- [ ] Probe dispatched from `AuthenticationStateListener` (startup + every auth change), not once from
      `TimeWarpPage`
- [ ] `AgentAsk.razor` renders the four states; sign-in button and Retry
- [ ] `RelayChatClient` error text carries status and title
- [ ] Screenshots: signed-in configured, signed-out, signed-in no key (and the master failure)
- [ ] `ganda repo audit`, `dev build`, targeted tests green; PR with proof (not merged)

## Session

- Created: 2018678 (2026-10-09)

## Notes

- Reported in the Grok Bot chat 2026-10-09; Steven approved the fix at 22:52 ICT.
- Do not run the app on TWE-001 and do not touch Steven's running instance; browser proof runs on the
  box and in CI (same as task 290).
- Related: task 289 (Ask + probe), task 290 (prerender style flash, also changed the setup command
  to `--id`).
- After merge Steven pulls master and restarts his web-server; his key needs no change.

### 2026-10-09 23:58 ICT: interactive investigation handed to the ganda walk

Steven's rule (tw-ganda-walk): this task goes through `ganda task work`. Nothing below was
committed or pushed. It's context for the walk.

**Root cause (confirmed with a real-WASM Playwright run on the box):**

- (b) wrong scheme and (c) missing credentials are ruled out. With a real passkey-issued
  identity-session cookie present at page load, `GET api/agent-chat/configuration` returns 200 in
  both InteractiveAuto (server circuit) and InteractiveWebAssembly, and Ask is configured.
- (a) is the cause. The anonymous Home page renders TimeWarpPage, which probes once
  (`if (!ChatProbeCompleted)`). It gets an honest 401, and `HandleError` stored NotConfigured.
  Home "Sign in" → in-SPA `/Login` → CreateAccount/SignInWithPasskey (and Entra bootstrap choices)
  → `NotifySessionChanged()` (the `api/identity/session` 200 in Steven's log) → `NavigateTo` without
  forceLoad. The probe never runs again, so "AI not configured" sticks.
- On master the repro test logs `configuration statuses: [401]; signed out: AgentAskNotConfigured;
  signed in: AgentAskNotConfigured`. With the fix it logs `[401, 200]; signed out: AgentAskSignIn;
  signed in: Configured`. Both tests pass (2/2).

**Interactive work (uncommitted), as a patch:** `C:\Users\steve\ovn\ask401-handoff.tgz` on TWE-001
(= `/mnt/c/Users/steve/ovn/ask401-handoff.tgz`; the box copy is `/workspace/ask401/`). It holds
`293-interactive.patch` (applies to master 7834c9f75), the master and fixed screenshots
(`before/`, `after/`), and the Playwright logs (`pw-before.log`, `pw-after.log`). The walk may reuse
it or redo it:

- `features/application/agent/chat-readiness-probe.cs` (new): pure `ChatReadinessProbe`
  (FromResponse/FromProblem/FromFileResponse/Describe). 200 maps to Configured/NotConfigured, 401 to
  Unauthenticated, anything else (403/5xx/499/file) to Error with "status title: detail".
- `CatalogAgentReadiness` gains Unauthenticated and Error. `AgentSurfaceState.ChatProblem`
  holds the Error text. `CatalogAgentAvailability.Problem`.
- `LoadChatConfiguration` handler applies the mapper, so no failure becomes NotConfigured.
- `AuthenticationStateListener` dispatches `LoadChatConfiguration` at startup and on every
  AuthenticationState change (with a CrossSliceReference). TimeWarpPage no longer probes.
- `AgentAsk.razor` shows: NotConfigured with the command; Unauthenticated as "Sign in to use Ask"
  plus a Sign in button (closes the modal, `RouteState.ChangeRoute(LoginPage.GetLoginUrl(current))`,
  because TWA0026 forbids NavigateTo); Error with status/detail and Retry. Adds `.twe-agent-ask__actions`
  CSS.
- `RelayChatClient` throws `ChatReadinessProbe.Describe(problem)` instead of the generic text.
- `HomePage.razor`: `data-qa="HomeSignIn"` on the Sign in button (test hook).
- Tests: `web-spa-playwright-tests/ask-sign-in-playwright-tests.cs` (CDP virtual authenticator,
  no mock header; screenshots to `artifacts/playwright/293`), and
  `web-spa-integration-tests/features/application/agent-ask-readiness-tests.cs` (mapping and
  HtmlRenderer markup per state, plus re-probe replacing a 401). It builds; I did not get to run it.

**Still open for the walk:**

- Run the unit tests.
- Run the existing `ask-surface-playwright-tests` (mock header) to check for regressions.
- `dev build` and `ganda repo audit`.
- The PR body needs proof (failing-then-passing output, screenshots of the signed-in configured,
  signed-out and no-key states) and CI green.
- After merge Steven pulls and restarts his web-server. His key needs no change.

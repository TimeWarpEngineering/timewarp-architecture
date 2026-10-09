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

- [x] Real-WASM Playwright test: anonymous Home → Ask, then in-SPA passkey sign-in (CDP virtual
      authenticator) → Ask. Fails on master (stale "AI not configured" after a real 401), passes after.
- [x] Unit tests for the readiness mapping (200 configured / 200 no key / 401 / 403 / 500 / 499 /
      file response) and for the Ask markup per state
- [x] `CatalogAgentReadiness` gains Unauthenticated and Error; `AgentSurfaceState` keeps the problem text
- [x] `LoadChatConfiguration` maps outcomes through one pure function; no failure becomes NotConfigured
- [x] Probe dispatched from `AuthenticationStateListener` (startup + every auth change), not once from
      `TimeWarpPage`
- [x] `AgentAsk.razor` renders the four states; sign-in button and Retry
- [x] `RelayChatClient` error text carries status and title
- [x] Screenshots: signed-in configured, signed-out, signed-in no key (and the master failure)
- [x] `ganda repo audit`, `dev build`, targeted tests green. Proof is in Results. The PR is the host
      open-pr node (not opened from this oracle).

## Results

Ask tells the truth about the configuration probe, and it asks again when the session changes.

- `ChatReadinessProbe` is the one mapping. A 200 is Configured or NotConfigured. A 401 is
  Unauthenticated. 403, 500, the transport's synthetic 499, an empty problem, and a file body are
  Error with `status title: detail`. Nothing else becomes NotConfigured, so the user-secrets command
  shows only when the server said there is no key.
- `AuthenticationStateListener` dispatches `LoadChatConfiguration` at startup and on every
  `AuthenticationState` change. `TimeWarpPage` no longer probes. An in-app sign-in
  (`NotifySessionChanged`, no `forceLoad`) replaces the anonymous 401.
- `AgentAsk` renders the four states. Unauthenticated is "Sign in to use Ask" plus a Sign in button
  that closes the modal and `ChangeRoute`s to `LoginPage.GetLoginUrl` for the current path
  (TWA0026). Error shows the problem text and Retry. The Identity edge is
  `[CrossSliceReference]` on `AgentAsk`. The configuration endpoint's auth is unchanged.
- `RelayChatClient` throws `ChatReadinessProbe.Describe(problem)` (status, title, detail).

### Proof

On master (handoff `pw-before.log`, base `7834c9f75`), the same browser test logged:

```text
task-293 configuration statuses: [401]; signed out: AgentAskNotConfigured; signed in: AgentAskNotConfigured
```

The signed-in Ask still said "AI not configured" and printed the user-secrets command. That screenshot
is the handoff `before/master-ask-signed-in-stale.png`.

This branch, Release, real InteractiveWebAssembly, no mock principal, CDP virtual authenticator
(2026-10-10):

```text
task-293 configuration statuses: [401, 200]; signed out: AgentAskSignIn; signed in: Configured
```

`web-spa-playwright-tests --filter-class Ask`: 3 passed, 0 failed (the two new tests plus the existing
mock-header `AskSurface_Given_Wasm`). Screenshots under `artifacts/playwright/293/` (gitignored; CI
uploads `artifacts/playwright/`):

- `ask-signed-out.png` — "Sign in to use Ask" and a Sign in button. No key command.
- `ask-signed-in-configured.png` — the chat ("Ask for something this page can do.").
- `ask-signed-in-configured-answer.png` — a fake-upstream answer containing `page_context:`.
- `ask-signed-in-no-key.png` — "AI not configured" and
  `dotnet user-secrets set "XAI:ApiKey" "<your-xai-key>" --id 0e53fdd3-6f93-4d5a-9c86-040621f7929e`.

`AgentAskReadiness_Should_` (Release): 9 passed, 0 failed. Covers 200 configured, 200 no key, 401,
403, 500, 499, a file body, markup per state, and a second probe replacing a 401 with Configured.

`./bin/dev build`: succeeded, 0 warnings, 0 errors. `ganda repo audit`: 31 passed, 0 failed.

### How to validate

**Smoke:**

```bash
cd tests/container-apps/web/web-spa-integration-tests
dotnet test -c Release -- --filter-class AgentAskReadiness_Should_
cd ../web-spa-playwright-tests
dotnet test -c Release -- --filter-class Ask
```

**Expect:**

- Readiness tests: 9 passed, 0 failed. A 401 assertion contains `Sign in to use Ask` and does not
  contain `user-secrets`. A 500 assertion contains `500 Internal Server Error: upstream exploded`
  and `AgentAskRetry`.
- Playwright: 3 passed, 0 failed. The log line is
  `task-293 configuration statuses: [401, 200]; signed out: AgentAskSignIn; signed in: Configured`.
- `artifacts/playwright/293/ask-signed-out.png` shows Sign in. `ask-signed-in-configured.png` shows
  the chat box. `ask-signed-in-no-key.png` shows the user-secrets command.

### Review disposition

- **Effort / roster:** 3 (by-diff, 1115 lines); general reviewer. **Rounds:** 2.
- **Final counts:** bug 1 fixed; suggestion 1 fixed, 1 wontfix; nit 1 fixed; 0 open.
- **Disposition:** `accepted-exceptions`.
  - M1 (bug, fixed): `AuthenticationStateListener` ran the Ask probe before profile, credentials and
    site settings, so a probe exception skipped the identity work. The probe now runs last.
  - M3 (fixed): the Playwright tests now wait for the post-sign-in configuration probe before opening
    Ask. The race reproduced once locally; after the fix, 3 consecutive runs passed 3/3.
  - M4 (fixed): removed the dead `ChatProbeCompleted`.
  - M2 (wontfix): a transport exception, as opposed to a problem response, still does not become the
    Error state. `ApiHandler` rethrows those for every SPA handler by design, and changing that is
    outside this task.
- **After the fixes:** `dev build` 0/0; `AgentAskReadiness_Should_` 9/9; Playwright
  `--filter-class Ask` 3/3, three runs in a row; `ganda repo audit` passed.
- **Artifacts:** `review/review-framework.md`, `review/round-1/{general,merged}.md`,
  `review/round-2/{general,merged}.md`, `review/disposition.md`.

## Session

- Created: 2018678 (2026-10-09)
- Implementation: ganda task-work implementer (2026-10-10)
- Review: ganda task-work review oracle (Claude Opus 5.5) + general reviewer subagent (2026-10-10)
- Review oracle: review by implementer-claude (claude, model claude-opus-5-5), session not reported, max-turns 200 — 2026-10-09T17:15:41Z

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

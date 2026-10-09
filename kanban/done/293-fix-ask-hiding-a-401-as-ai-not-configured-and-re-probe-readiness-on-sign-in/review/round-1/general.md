# Round 1 — general
**Date:** 2026-10-10
**Scope reviewed:** branch vs master (git diff master...HEAD)

## Summary

The core fix is sound. `ChatReadinessProbe` is a pure mapping with good unit coverage, `AgentAsk`
renders all four states, `RelayChatClient` now reports status, title and detail, and the
Purpose/Design regions were updated across the files the change touched (TimeWarpPage, AgentSurfaceState,
CatalogAgentAvailability, the listener). I found no race between concurrent probes. `ApiHandler`'s
per-state semaphore serializes the request phase, and `Apply` runs synchronously after the release, so
a probe cannot overwrite a newer result. One real bug remains: the identity listener now runs the probe
first, so a transport-level failure (an exception, not a problem detail) breaks the identity pipeline
and never becomes Error. The Playwright test can also read the stale pre-sign-in state.

## Issues

### Issue 1 — Severity: bug
- File: source/container-apps/web/projects/web-spa/features/identity/AuthenticationStateListener.razor:28
- Description: `HttpApiService.GetResponse` catches only `OperationCanceledException` (mapped to 499)
  and JSON deserialization errors. A transport failure such as `HttpRequestException` (server
  unreachable, CORS/network error) propagates. `ApiHandler.Handle` logs it and rethrows. The listener
  now awaits `LoadChatConfiguration()` first, so this exception has two effects. (a) It aborts
  `HandleUserAuthentication` before `FetchProfileData` / `FetchCredentials` / `FetchSiteSettings` on
  sign-in, or before the Clear* calls on sign-out. At startup that happens in `OnInitializedAsync`; on
  a change it is unhandled inside the `async void` `HandleAuthenticationStateChanged`. An Ask probe
  failure now breaks the identity pipeline it was attached to. (b) Readiness stays `Unknown`, so Ask
  shows "Starting Ask…" with no Retry. Task requirement: "Error (any other failure)". The task
  description also lists "a network failure" as one of the masked cases.
- Suggestion: Map transport exceptions to Error inside the handler. For example, override or wrap so
  that a non-cancellation exception from the probe becomes `ChatProbeResult(Error, …, "Network error:
  <message>")`, or add a `ChatReadinessProbe.FromException` covered by a unit test. In the listener,
  also make sure the Ask probe cannot short-circuit the identity work: run it last, or isolate it so a
  throw does not skip profile, credentials and site settings.
- Status: open

### Issue 2 — Severity: suggestion
- File: tests/container-apps/web/web-spa-playwright-tests/ask-sign-in-playwright-tests.cs:67
- Description: After `SignInWithNewPasskeyAsync` the test waits for `/Settings` and the app bar, then
  calls `OpenAskAsync`. That method returns the first visible state among NotConfigured, SignIn, Error
  and the textarea. The re-probe is dispatched from the `async void` `AuthenticationStateChanged`
  handler, concurrently with the post-sign-in `NavigateTo`. The global store can still hold the
  previous `Unauthenticated` answer when Ask opens, so the test can capture `AgentAskSignIn` and report
  a false "stale readiness" failure. The same risk applies at line 116
  (`SignedIn_WithoutKey_Should_ShowTheSetupCommand`, `state.ShouldBe("AgentAskNotConfigured")`). The
  passing proof depends on the probe winning the race against navigation plus Ctrl-K.
- Suggestion: Before opening Ask after sign-in, wait deterministically for the re-probe. Options:
  `Page.WaitForResponseAsync` on `/api/agent-chat/configuration` with status 200, started before the
  Create-passkey click; or poll until `session.ConfigurationStatuses` gains an entry after
  `signedOutProbes`. Alternatively wait for the expected state locator rather than "any state". Keep
  the stale-state assertion: wait for the 200, then assert Ask is not SignIn.
- Status: open

### Issue 3 — Severity: suggestion
- File: source/container-apps/web/projects/web-spa/features/identity/AuthenticationStateListener.razor:28
- Description: The probe is awaited before the identity work. Every startup and auth change now delays
  profile, credentials and site-settings loading (and the sign-out clears) by one
  `api/agent-chat/configuration` round-trip. Before this change the shell probe ran independently of the
  listener. On sign-out the stale profile also stays visible until the probe answers.
- Suggestion: Dispatch the Ask probe after the identity branch (or alongside it), so Ask readiness
  never gates the identity pipeline. Update the listener's Design line to match.
- Status: open

### Issue 4 — Severity: nit
- File: source/container-apps/web/projects/web-spa/features/application/agent-surface-state/agent-surface-state.cs:38
- Description: `ChatProbeCompleted` no longer has any reader. Its only consumer was the deleted
  `TimeWarpPage.OnInitializedAsync` guard. It is still written in `Initialize` and `Apply`, but nothing
  in source or tests reads it. Dead state invites someone to reintroduce the once-per-app gate.
- Suggestion: Remove the property and its two writes. `ChatReadiness != Unknown` already conveys
  "a probe has answered".
- Status: open

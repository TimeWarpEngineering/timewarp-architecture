# Round 1 — merged findings
**Date:** 2026-10-10
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 1 | 0 |
| suggestion | 0 | 1 | 1 |
| nit | 0 | 1 | 0 |

## Issues

### M1 — Severity: bug — Status: fixed
- File: source/container-apps/web/projects/web-spa/features/identity/AuthenticationStateListener.razor:28
- Description: The listener awaited the Ask probe **before** the identity work. If the probe throws (for example a transport `HttpRequestException`), sign-in skips profile, credentials and site-settings loading, and sign-out skips the clears. Before this change the probe ran in TimeWarpPage, outside the identity pipeline. (Includes general Issue 3: the probe also added one request of latency in front of the identity work.)
- Suggestion: Run the probe after the identity branch.
- Source: general (Issues 1a, 3)
- Disposition notes: Fixed. `LoadChatConfiguration` is now the listener's last dispatch. The listener's Design region and inline comment were updated.

### M2 — Severity: suggestion — Status: wontfix
- File: source/container-apps/web/projects/web-spa/features/base/api-handler.cs:78
- Description: A transport exception (not a problem detail) never becomes `CatalogAgentReadiness.Error`, so Ask stays on "Starting Ask…" with no Retry.
- Suggestion: Map non-cancellation exceptions to Error.
- Source: general (Issue 1b)
- Disposition notes: wontfix (orchestrator). `ApiHandler.Handle` is sealed and rethrows transport exceptions for **every** SPA handler by design: the Design regions of ApiHandler and HttpApiService say unexpected failures surface rather than becoming a synthetic problem. Changing that policy is cross-cutting and outside task 293's scope, which is 401 masking and re-probe. Every masking path that reaches `HandleError` (401, 403, 5xx, 499, file body) now maps correctly. Once M1 is fixed, a probe exception no longer damages the identity pipeline. Downgraded from bug, since the part that was a regression is M1.

### M3 — Severity: suggestion — Status: fixed
- File: tests/container-apps/web/web-spa-playwright-tests/ask-sign-in-playwright-tests.cs:67,116
- Description: The test opened Ask after sign-in without waiting for the re-probe, so it could read the stale signed-out answer. Confirmed: the first local re-run failed `SignedIn_WithoutKey` with `AgentAskSignIn`.
- Suggestion: Wait for the post-sign-in configuration response before opening Ask.
- Source: general (Issue 2)
- Disposition notes: Fixed. `Session.WaitForConfigurationProbeAsync(priorCount)` is used after sign-in in both tests. The no-key test also waits for the signed-out probe before counting. It does not throw on timeout, so on old code the assertions still report the stale state. 3 consecutive runs: 3/3 passed.

### M4 — Severity: nit — Status: fixed
- File: source/container-apps/web/projects/web-spa/features/application/agent-surface-state/agent-surface-state.cs:38
- Description: `ChatProbeCompleted` had no readers.
- Suggestion: Remove it.
- Source: general (Issue 4)
- Disposition notes: Fixed. Removed the property, its write in `Initialize`, and its write in `Apply`.

## Duplicates / conflicts

- General Issue 3 (probe delays the identity work) shares M1's root cause and fix, so it is collapsed into M1.
- General Issue 1 is split: (a) the identity pipeline cut short is M1 (fixed); (b) the transport exception is not mapped to Error is M2 (wontfix).

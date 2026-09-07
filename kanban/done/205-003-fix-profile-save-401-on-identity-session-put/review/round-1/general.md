# Round 1 — general
**Date:** 2026-09-07
**Scope reviewed:** product commit `48460f9a` vs `origin/master` (framework file list + surrounding call sites)

## Summary

The change correctly splits the cookie hole by render host: server named `WebService` loopback gets `IdentitySessionCookieForwardingHandler` (Cookie + mock-principal header from `IHttpContextAccessor`), and WASM named clients get `BrowserRequestCredentialsHandler` (`SameOrigin`). Empty/non-JSON 401/403 map to honest `SharedProblemDetails` in `HttpApiService`, and UpdateProfile's 401 path toasts via `DefaultApiHandler` then `/Login?returnUrl=/Profile` without rethrowing through `ApiHandler` (no yellow-bar on that path). Risk is moderate around InteractiveServer relying on ambient `HttpContext` (Microsoft's own token-handler pattern), but wiring, auth markers, mock fail-closed, anonymous PUT 401, cookie-jar isolation, and required tests all check out; live `/Profile` Save was not re-run on this branch.

## Issues

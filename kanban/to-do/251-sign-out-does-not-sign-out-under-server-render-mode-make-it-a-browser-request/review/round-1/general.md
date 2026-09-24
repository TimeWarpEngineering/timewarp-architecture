# Round 1 — general
**Date:** 2026-09-24
**Scope reviewed:** same as framework

## Summary

The change moves SPA sign-out from a `IWebServerApiService` POST (a server loopback under
InteractiveServer) to a browser-side token fetch + hidden form POST to a new antiforgery-validated
`POST /identity/sign-out`. That endpoint dispatches the existing `EndBrowserSession.Command`, so
there is still one session-end implementation, and it answers 303 `/Login`. Antiforgery is checked
explicitly because neither Blazor's middleware nor FastEndpoints enforces it here. A missing or
invalid token returns 400 and clears nothing. The Design regions on the contract, handler, SPA
action, JS module and TS file match the code. The reviewer re-ran the targeted suites in this
worktree: web-server `SignOut_` 6/6, web-spa `SignOut_Should_` 2/2. Risk is low. The only finding
is one mis-wrapped comment line.

## Issues

### Issue 1 — Severity: nit
- File: source/container-apps/web/projects/web-spa/services/sign-out-js-module.cs:10
- Description: A Design region line runs past the wrap width and leaves a short orphan line ("the server endpoints.").
- Suggestion: Re-wrap the comment.
- Status: open

# Round 1 — general
**Date:** 2026-10-13
**Scope reviewed:** framework scope (product + tests), plus call sites: HttpApiService, web-spa program.cs IWebServerApiService registration

## Summary

The change routes the four session-issuing completions (CompletePasskeyRegistration,
CompletePasskeyAuthentication, CompleteEntraBootstrapExisting, CompleteEntraBootstrapCreate) through
a browser fetch, so the browser receives the Set-Cookie in both render modes, and it force-loads after success.
The transport stays in HttpApiService. Only the wire changes, and the handler is a thin
envelope adapter. Risk is low. Checked these points and found them sound:
- The same JsonSerializerOptions seam is used as WebServerApiService.
- Network failures surface as JSException, which every caller already catches.
- The 204 and problem mapping is preserved, and Content-Type is carried back.
- GetSafeReturnUrl still guards each navigation.
- No bodies are logged.
- The start calls still go through IWebServerApiService, so the mock-mode 501 path is unchanged for the passkey ceremonies.

## Issues

### Issue 1 — Severity: nit
- File: source/container-apps/web/projects/web-spa/features/identity/sign-in-state/sign-in-state.create-account-from-microsoft-365.cs:10
- Description: Design region reflow left a short broken line ("A problem whose" / next line).
- Suggestion: Rewrap the paragraph.
- Status: open

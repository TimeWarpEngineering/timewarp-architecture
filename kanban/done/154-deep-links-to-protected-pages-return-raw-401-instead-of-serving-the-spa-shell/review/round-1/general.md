# Round 1 — general
**Date:** 2026-08-05
**Scope reviewed:** commit 0067116b dual-mode cookie challenge

## Summary

The dual-mode identity-session cookie challenge matches the plan: pure helpers in `IdentitySessionCookieChallenge`, `OnRedirectToLogin` 302 vs 401, forbid always 403, and integration coverage for HTML deep-link redirect, Accept-less fallback, anonymous `/api/Roles` 401, and Member `/Admin/Roles` forbid-not-Login. Cross-checked `LoginPath` / `returnUrl` against `[Page("/Login")]` and `LoginPage` query binding; `BuildLoginRedirectTarget` emits path+query only (open-redirect stays on `GetSafeReturnUrl`). No product bugs found.

## Issues

### Issue 1 — Severity: nit
- File: source/container-apps/web/platform/identity-host/identity-session-cookie-challenge-server.cs:47-62
- Description: After the `/api` hard-stop, `ShouldRedirectToLogin` always returns `true`. The `Sec-Fetch-Dest: document` and `Accept: text/html` branches are behaviorally dead; runtime classification is path-only. Design documents multi-step classification plus a non-API fallback, which is consistent with intent but overstates what the code currently discriminates.
- Suggestion: Either simplify to “non-`/api` → redirect” (keep Design note on why Accept/fetch metadata are not required), or change the final fallback if a future policy should 401 non-document non-HTML non-API requests — and add a test for whichever rule is real.
- Status: fixed (path-only simplify + Design; round-1 disposition)

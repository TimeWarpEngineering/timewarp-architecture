# Round 4 — general
**Date:** 2026-10-10
**Scope reviewed:** CI-fix delta 5a37e7725..35b95c73b — `tests/container-apps/web/web-spa-integration-tests/features/application/action-catalog-tests.cs`, `tests/container-apps/web/web-spa-playwright-tests/feedback-attachment-playwright-tests.cs`

## Summary

The delta fixes the two CI failures on PR #458. The action-catalog roster now lists `Feedback.RemoveFeedbackAttachment` and `Feedback.UploadFeedbackAttachment` in sorted order, and asserts both are Human with `feedback.file.self`. The Playwright test drops the mock-principal header (which authenticates server API calls only) and signs in with a virtual passkey before opening `/Feedback`. The ceremony mirrors `AskSignIn_Given_Wasm` (same CDP authenticator options, `HomeSignIn` → `/Login` → `CreatePasskey` → `/Settings`). A new account gets `feedback.file.self` from `RolePermissionSeed.SelfServicePermissions`, so the `[Authorize]` page and the upload/download API calls (now cookie-authenticated) are reachable. Low risk; test-only change.

Re-verified: `ActionCatalog_Should` 10/10 (Release); `web-spa-playwright-tests` builds with 0 warnings, 0 errors. Browser run not executed (TWE-001); left to CI.

## Issues

None. Prior M1–M14 remain fixed; the delta touches no product code.

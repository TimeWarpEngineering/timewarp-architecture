# Round 1 — general
**Date:** 2026-10-15
**Scope reviewed:** same as framework, plus `tests/common/timewarp-testing/session-host-fixture.cs` and `web-spa-integration-tests/infrastructure/spa-session-fixture.cs` (exemplar).

## Summary

The change replaces six per-test `InstallChromium` + `Playwright.CreateAsync` + `LaunchAsync`
copies with one `PlaywrightBrowserSession : SessionHostFixture<IBrowser>`, registered from its own
ModuleInitializer exactly like `SpaSessionFixture`. Every test still opens and disposes its own
`IBrowserContext`, so cookies, storage, and the WebAuthn virtual authenticator stay per test. The
override `DisposeAsync` disposes the browser (base) then the driver; `CreateAsync` disposes the
driver if launch fails. No process-static `Lazy`. The passkey class moves its forced
InteractiveServer host to `SetupOnce`/`CleanUpOnce`; both tests only assert the sign-in cookie and
a signed-in reload, so a second account on the same host does not change any assertion. The
no-C-share rationale (fixed in-proc ports, Administrator claim on first account, exact row counts)
holds against the code. Assertions and the 351/356 waits are untouched. Risk is low; only two
comment nits.

## Issues

### Issue 1 — Severity: nit
- File: tests/container-apps/web/web-spa-playwright-tests/feedback-attachment-playwright-tests.cs:16
- Description: the edited Design comment line runs well past the wrap width of the surrounding region.
- Suggestion: rewrap.
- Status: open

### Issue 2 — Severity: nit
- File: tests/container-apps/web/web-spa-playwright-tests/playwright-browser-session.cs:18
- Description: Design says "Each class still owns its HostGraph", but most classes boot a host per test; only the passkey class owns one per class.
- Suggestion: say each test (or, for the passkey class, each class) owns its HostGraph.
- Status: open

# Round 1 — general
**Date:** 2026-08-06
**Scope reviewed:** commit c4c90779 (147-005 first-run chrome)

## Summary

Commit delivers the locked first-run chrome plan: reusable `TimeWarpFocusedPage` auth shell (Purpose + Design, logo + centered column, no product chrome), Login rebuilt as a passkey-only card (`Sign in with a passkey` / divider / “Don’t have an account?” + `Create account`) with preserved `data-qa` hooks and no session debug line, Home differentiated via outer `AuthorizeView` (anonymous CTA vs signed-in strip) with nested Admin gate on `Policies.CanViewAdminSidebarNavSection` and distinct `Context="adminAuth"`, Try-it relocated to Developer-gated `TestPage` with `TwoSecondTask` restored via ActionSet generator (callable from TestPage; present in built Web.Spa), optional Logout on focused shell, and `ChangePassword` fully removed from product/tests (only historical kanban text remains). Ceremony options remain hybrid-safe (registration omits `authenticatorAttachment`; auth uses empty `allowCredentials`). Colors use `--twe-*` tokens; no hard-coded brand colors in new CSS.

Matches the plan brief. One non-blocking markup concern on the anonymous Home CTA.

## Issues

### Issue 1 — Severity: suggestion
- File: source/container-apps/web/projects/web-spa/features/application/pages/HomePage.razor:38
- Description: Anonymous Home CTA wraps a primary `FluentButton` inside `NavLink` (`a` > interactive button). That is invalid HTML nesting and can make the primary first-run path click-unreliable depending on Fluent/web-component event handling, unlike Login (direct `OnClick` ceremony) and Profile (RouteState navigation).
- Suggestion: Prefer a single navigation control — e.g. `FluentButton` `OnClick` → `NoSubRouteState.ChangeRoute(LoginPage.GetPageUrl())` (same pattern as Profile sign-in), or a button `Href`/`NavLink` styled as the primary CTA without nesting a button inside an anchor. Keep routing via `LoginPage.GetPageUrl()`.
- Status: open

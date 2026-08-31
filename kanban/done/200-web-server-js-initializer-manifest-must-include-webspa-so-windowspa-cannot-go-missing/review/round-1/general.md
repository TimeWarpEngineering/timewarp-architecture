# Round 1 — general
**Date:** 2026-08-31
**Scope reviewed:** branch vs origin/master (task 200 host JS initializer + passkey import)

## Summary

The change fixes the host-side stale initializer hole (Content re-glob after tsc + MSBuild assert + UpToDateCheckInput) and moves Login/Settings passkey C# off `Spa.WebAuthn.*` onto `IJSRuntime` `import()` of `./js/features/web-authn.js`. Risk is low: Release `jsmodules.build.manifest.json` already lists `js/web.spa.foa7bin14p.lib.module.js`, no remaining product C# `Spa.WebAuthn` string identifiers, and both Jaribu gates are host-free. Dominant themes are belt-and-suspenders host gating and an accurate (but slightly overstated) Design claim about how the import specifier resolves.

## Issues

### Issue 1 — Severity: nit
- File: source/container-apps/web/projects/web-spa/services/web-authn-js-module.cs:10
- Description: Design says fingerprinted emit is remapped by the Blazor import map, but `App.razor` has no `<ImportMap />`. Nested routes are still safe for another reason: Blazor's `import` interop rewrites `./…` against `document.baseURI` (and the app sets `<base href="/" />`), and MapStaticAssets still exposes the unfingerprinted `js/features/web-authn.js` endpoint (`no-cache`) alongside the fingerprinted route. The passkey path works; the Design line overstates the mechanism.
- Suggestion: Reconcile Design to cite the `./` → `document.baseURI` rewrite (and dual SWA endpoints), or add `<ImportMap />` if immutable fingerprinted imports are the intended story.
- Status: open

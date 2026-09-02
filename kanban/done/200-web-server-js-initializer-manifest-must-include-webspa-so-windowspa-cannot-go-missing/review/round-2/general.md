# Round 2 — general
**Date:** 2026-08-31
**Scope reviewed:** M1 Design-region fix on web-authn-js-module.cs; fix delta only

## Summary

Re-checked the Design region on `web-authn-js-module.cs` against `App.razor`. The prior ImportMap claim is gone; Design now correctly cites `./` resolution via `<base href="/" />`, MapStaticAssets dual endpoints (unfingerprinted + fingerprinted twin), and explicitly states there is no `<ImportMap />` in App.razor. App.razor still has `<base href="/" />` and no ImportMap. The fix delta introduces no new defects.

## Resolved prior

### M1 — Severity: nit — Status: fixed
- File: source/container-apps/web/projects/web-spa/services/web-authn-js-module.cs (Design region)
- Disposition: Design now matches the actual resolution story (`<base href="/" />` + MapStaticAssets dual endpoints; no ImportMap). Verified against App.razor.

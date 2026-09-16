# Round 1 — general
**Date:** 2026-09-16
**Scope reviewed:** branch `task/225-move-site-authentication-policy-from-settings-to-a` vs `origin/master` (commits `f439bbdb` feat + `1fd78a43` Results). Moves site Entra policy to `/Admin/Authentication`, surfaces config-vs-store drift (boot Warning + banner + add-tenant), Development-only reseed + `dev entra reseed`.

## Summary

The change matches Requirements A and B. Site authentication policy left `/Settings` for an Admin page gated by `settings.write` inside the `admin.access` nav category; personal Settings keeps passkeys and Link Microsoft 365. Boot drift uses `LoggerMessage.Define` Warnings (Enabled, AllowBootstrap, untrusted TenantId) with `/Admin/Authentication` / `dev entra reseed` remediation; `ReseedSiteSettings` is honoured only when the hosted service passes `IHostEnvironment.IsDevelopment()`; add-tenant goes through normal `UpdateSiteSettings` version CAS. Permission decision in `permission-ids-contracts.cs` is honest and consistent with the seed (Administrator gets `settings.write`; Member does not). Razor order, `[Page]` placement, and CrossSliceReference edges look correct. Risk is low; no defects found against the brief.

## Issues

_(none)_

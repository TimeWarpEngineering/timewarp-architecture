# Round 1 — general
**Date:** 2026-09-13
**Scope reviewed:** branch vs origin/master (commit 40d3c3bb); deleted profile-menu state + web-spa.csproj leftover excludes; surrounding Profile.razor / TimeWarpPage.razor / _Imports.razor / tests / skills call sites

## Summary

Commit 40d3c3bb deletes the unused `ProfileMenuState` leftover (four files under `features/profile-menu/`) and removes stale `_ContentIncludedByDefault` excludes for already-gone profile-menu components in `web-spa.csproj`. Risk is low: the live header menu remains FluentMenu in `Profile.razor`, which was not edited; `TimeWarpPage.razor` still composes `<Profile />`. All nine verification claims held — no remaining product/test/skills consumers of `ProfileMenuState` or `Features.ProfileMenus`, no leftover ActionSet/DevTools/Design-region references in source (the task-211 Design region lived only on the deleted Toggle file), and the NavMenu "Login is profile-menu" comment is descriptive only.

## Issues


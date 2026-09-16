# Round 1 — general
**Date:** 2026-09-16
**Scope reviewed:** `task/231-admin-authentication-page-use-the-editform-and-flu` vs merge-base `origin/master` (`09993048`); product commit `e8f75802`

## Summary

Admin `/Admin/Authentication` now persists site policy through `EditForm` bound to `UpdateSiteSettings.Command` / `ISiteSettingsDetails` (including concurrency `Version` on the command), `OnValidSubmit` → existing `SiteSettingsState.UpdateSiteSettings`, and a primary `FluentButton` submit with `data-qa="AuthenticationSave"` disabled while busy. The read-only tenant line and Enabled/AllowBootstrap drift banner stay outside the form; layout matches RoleForm (`FluentStack` vertical gap 16); co-located `<style>` stays isolation-first hybrid Exception B with no new `::deep` or inline `style=` dumps. The repo `skills/tw-blazor/SKILL.md` Form vs inline-action rule and the 225 deep-link `data-qa` updates are in place; personal `/Settings` row actions are untouched. Overall risk is low — the change is a targeted UI-pattern alignment with established RoleForm/ProfilePage call sites.

## Issues


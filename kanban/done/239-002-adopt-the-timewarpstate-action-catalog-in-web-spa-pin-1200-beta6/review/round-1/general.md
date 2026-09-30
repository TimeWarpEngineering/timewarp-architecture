# Round 1 — general
**Date:** 2026-09-30
**Scope reviewed:** same as framework (14 files; product + test + CPM pin)

## Summary

The change pins TimeWarp.State/.Plus to 12.0.0-beta.6 (both pins, no stragglers), registers
`AddActionCatalog` for the web-spa assembly in `Program` and mirrors it in `AspireSpaTestApplication`,
and tags nine actions with `[CatalogAction]` using `PermissionIds` constants and per-action
`Visibility`. Verified: Counter's `DeveloperAccess` permission matches `CounterPage`'s
`[Page(Policy = PermissionIds.DeveloperAccess)]`; excluded actions named by the test
(`Chat.ServerToClientMessage`, `Application.FiveSecondTask`, `Profile.ClearProfileData`, …) exist in
source and are untagged; Design/Purpose regions were reconciled on edited files. The full-roster
equality test makes any roster drift a deliberate edit. Risk is low; no defects found.

## Issues

<!-- None. -->

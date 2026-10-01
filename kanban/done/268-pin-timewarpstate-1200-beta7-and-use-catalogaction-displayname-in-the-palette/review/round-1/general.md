# Round 1 — general
**Date:** 2026-10-01
**Scope reviewed:** branch vs master (Directory.Packages.props, command-palette-roster.cs, credentials-state.link-microsoft-365.cs, program.cs comment, action-catalog-tests.cs, command-palette-tests.cs)

## Summary

Pins TimeWarp.State / TimeWarp.State.Plus to 12.0.0-beta.7 (the only `TimeWarp.State.*` pins) and makes
`CommandPaletteRoster.Label` prefer the authored `ActionCatalogEntry.DisplayName`, keeping the "Owner: " prefix
for both authored and generated labels via a shared `WithOwner`. The ranker is untouched and still matches the
shown row name plus description. Empty DisplayName is rejected upstream by TWS0008, so `is { }` is sufficient.
Design regions reconciled. Tests cover authored label, `microsoft` / `link` ranking and the null fallback.
Re-verified: CommandPalette 33/33, ActionCatalog 11/11, `ganda repo audit` passes. Low risk.

## Issues

None.

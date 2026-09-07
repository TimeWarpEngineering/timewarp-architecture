# Round 1 — general
**Date:** 2026-09-07
**Scope reviewed:** branch `task/205-002-full-iso-language-and-region-catalogs-with-english` vs `origin/master` (commit `33aae301`); product files listed in `review/review-framework.md`

## Summary

The change replaces the 205-001 handwritten Language/Region allow-lists with BCL ISO catalogs (`CultureInfo.GetCultures(SpecificCultures)` + distinct alpha-2 regions), keeps Theme closed, leaves SPA UI culture on `en-US`, and switches Language/Region to searchable `FluentCombobox`. Risk is low: validator and domain use the same BCL checks (domain does not reference contracts), junk still fails, Thai validates and persists, and the smoke count bump matches the two new runfile tests (13→15 → expected 134). Fluent UI v5.0.0-rc.5-26219.1 was decompiled: `FreeOption` defaults to null and the `freeform` attribute is emitted only when it is set, so the ProfilePage Design claim holds.

## Issues

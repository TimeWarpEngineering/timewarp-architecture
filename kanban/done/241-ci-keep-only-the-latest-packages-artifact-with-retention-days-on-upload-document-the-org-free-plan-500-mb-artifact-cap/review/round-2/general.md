# Round 2 — general
**Date:** 2026-09-20
**Scope reviewed:** re-verify M1 + fix delta `33b35648` on documentation/developer/guides/releasing.md

## Summary

Post-fix `33b35648` correctly scopes the expired/missing `Packages-*` subsection: this repo’s cut packs on `release:published` (cutting >3 days after merge is fine; a merge CI rerun still would not upload nupkgs), and `gh run rerun` is reserved for the org-wide `tw-release` locate-run → download-artifact path. Free-plan / 3-day retention prose and the `tw-release` cross-link remain. The fix delta is docs-only and introduces no new defects.

## Prior findings

### M1 — Severity: suggestion — Status: fixed
- File: documentation/developer/guides/releasing.md
- Description: Verified against HEAD `33b35648` lines 52–70. Subsection now states this repo does not download a merge `Packages-*`, packs fresh on `release:published`, and that a merge CI rerun here still would not produce `Packages-*` (`if-no-files-found: ignore`; PR/merge is clean → build → test only). The `gh run rerun <ci-run-id>` remedy is present and scoped to org-wide `tw-release` locate-run → download-artifact (siblings / future promote). Free-plan 500 MB / `retention-days: 3` prose above the subsection is unchanged.
- Status: fixed

## Issues

<!-- New findings only. Omit if none. Do not invent. -->

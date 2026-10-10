# Round 2 — general
**Date:** 2026-10-11
**Scope reviewed:** fix delta on `feedback-attachment-playwright-tests.cs` plus re-check of M1 and M2

## Summary

M1 is resolved. `ScreenshotPath` no longer asserts the repo root or the kanban folder and writes to the test
output folder when they are absent. In this repo it still resolves `295-*` and `302-*`. M2 is resolved. The
attachment test uses `OpenFeedbackAsync`, and the wasm-download check still runs after it. `./bin/dev build`
reports 0 warnings. The Playwright project passed 7 of 7. `ganda repo audit` passed. No new issues.

## Issues

<!-- none -->

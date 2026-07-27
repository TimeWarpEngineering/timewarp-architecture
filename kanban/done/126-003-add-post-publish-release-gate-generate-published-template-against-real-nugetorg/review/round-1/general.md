# Round 1 — general
**Date:** 2026-07-27
**Scope reviewed:** commit 76d38327 (template-publish-smoke gate)

## Summary

Post-publish gate matches task intent: `template-publish-smoke` waits on nuget.org flatcontainer, installs the published template into an isolated hive, generates apps named ≠ sourceName, asserts CPM platform pins == release version (plus always-on synthetic self-check), then restore+builds with nuget.org-only `NuGet.config`. `workflow` runs the gate only after a real push (API key present); pack-only skips it; failure sets `Environment.ExitCode` and blocks via `RunStepAsync`. `template-smoke` is not modified; docs cover automation and manual fallback. Existing dry-run under `artifacts/template-publish-smoke/` shows package-mode restore of platform packages at `2.0.0-beta.7` from nuget.org.

## Issues

No issues.

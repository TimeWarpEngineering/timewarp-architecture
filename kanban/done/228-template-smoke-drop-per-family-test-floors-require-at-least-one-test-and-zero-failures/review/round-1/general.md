# Round 1 — general
**Date:** 2026-09-16
**Scope reviewed:** branch `task/228-template-smoke-drop-per-family-test-floors-require` vs `origin/master` — `jaribu-aggregator-summary-gate.cs`, `template-smoke-harness.cs`, `jaribu-aggregator-summary-gate-tests.cs`, `skills/tw-feature-placement/SKILL.md`, `skills/tw-feature-placement/references/co-located-jaribu-runfiles.md`

## Summary

The change drops per-family `MinimumSucceeded` floors and the 2× stale-floor warning, and gates MTP aggregator summaries on `failed == 0`, `total > 0`, and `total == succeeded + skipped` (exit 0 remains a harness precondition outside `Decide`). Call sites, tests, and contributor-facing skills match the requirements; leftover floor language appears only in kanban history (`task.md` / `kanban/done/`). Risk is low. Re-verified: `dotnet test -c Release -- --filter-class Decide_Given_` → 7 passed / 0 failed.

## Issues


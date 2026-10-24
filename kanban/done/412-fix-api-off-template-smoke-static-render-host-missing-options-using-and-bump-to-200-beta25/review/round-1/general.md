# Round 1 — general
**Date:** 2026-10-24
**Scope reviewed:** branch vs `master`: `tests/container-apps/web/web-spa-integration-tests/infrastructure/static-render-host.cs`, `Directory.Packages.props`, `source/Directory.Build.props`, `timewarp-templates/Directory.Build.props`

## Summary

The change adds `using Microsoft.Extensions.Options;` to `static-render-host.cs`. That file's only Options references are `IOptions<>` and `Options.Create` at lines 45–46. The `#if(api)` block in `global-usings.cs` is unchanged, and the pattern matches `features/application/ask-starter-prompts-tests.cs:25`. A Design-region line explains why the file imports the namespace itself. The version bump moves `<Version>` in both props files and all 11 task-124 platform pins to `2.0.0-beta.25`, in the same commit as the fix (fab70bda9). `git grep 2.0.0-beta.24` outside `kanban/` finds no other references. Risk is low.

Gates re-run by the reviewer in this worktree: `./bin/dev build` gave 0 warnings and 0 errors. `./bin/dev check-version` reported source one prerelease increment ahead of NuGet `2.0.0-beta.24` and printed "safe to release". The reviewer did not re-run template smoke. `template-smoke` has no option to run one variant, and the full matrix does not fit this review's budget. The `SmokeNoApi` result comes from the implementer's matrix run. On inspection the fix is correct: the file-level using does not depend on any template symbol, and it covers the only references.

Observation, not a finding on this diff: the implementer's matrix exited 1 on one SmokeNoYarp Playwright test, `Open_Should_MoveFocusIn_TrapTab_And_ReturnFocusOnEscape` (`paintHitsDialog` false). It passed 27/27 when rerun right away and passed on the other four web variants. The task's required outcome accepts `SmokeNoApi` plus the default variant, and both passed. If the miss comes back in nightly, it is worth tracking as a separate flake.

## Issues

None.

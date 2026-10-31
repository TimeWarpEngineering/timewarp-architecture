# Round 4 — general
**Date:** 2026-10-31
**Scope reviewed:** delta since round 3, `018ef652d..6143384cd` (plus the working tree)

## Summary

Since round 3, only kanban files have changed. `0c072435c` records the operator decision to wait for
timewarp-nuru task 562. `6143384cd` retargets the Requirements to 4.0.0-beta.10 and says to delete the
local `RepoConfigService` copy. No fix pass has touched the product tree. `Directory.Packages.props`
still pins beta.9. `tools/dev-cli/Directory.Build.targets` and
`tools/dev-cli/services/repo-config-service.cs` are unchanged. Both beta.10 nupkgs return HTTP 200 on
the nuget.org flat container (`timewarp.nuru` and `timewarp.nuru.devcli`), so the fix pass is not
waiting on anything.

## Issues

### Issue 1 — Severity: bug
- File: Directory.Packages.props:18
- Description: `TimeWarp.Nuru` and `TimeWarp.Nuru.DevCli` are pinned at `4.0.0-beta.9`. The
  Requirements now need `4.0.0-beta.10`.
- Suggestion: Move both pins to `4.0.0-beta.10`. Rebuild `bin/dev` and re-run every gate.
- Status: open

### Issue 2 — Severity: bug (carried M4, M5)
- File: tools/dev-cli/Directory.Build.targets:7, tools/dev-cli/services/repo-config-service.cs:1
- Description: The local `RepoConfigService` copy and the `OmitPackagedRepoConfigService` target are
  still in the tree. The operator chose option (b) on 2026-10-31: wait for Nuru, then delete them.
  Requirements now say to delete both and to leave no NoWarn entry and no copied package files.
- Suggestion: Delete `tools/dev-cli/services/repo-config-service.cs`. Delete the
  `OmitPackagedRepoConfigService` target. If that leaves only the parent import, delete
  `tools/dev-cli/Directory.Build.targets` too. Confirm that `dotnet build tools/dev-cli/dev.cs`
  builds clean against beta.10 and that CA2263 does not fire.
- Status: open

### Issue 3 — Severity: suggestion
- File: kanban/in-progress/457-take-timewarpnuru-400-beta8/task.md (Goal, Checklist, Results)
- Description: The Goal restatement, Checklist, Results, and How to validate still describe beta.9
  and the local copy.
- Suggestion: Rewrite them for beta.10 with no local copy, and record the new gate output.
- Status: open

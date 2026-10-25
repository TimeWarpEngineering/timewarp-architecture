# Round 2 — general
**Date:** 2026-10-25
**Scope reviewed:** fix-pass delta `f384c75b7..4ccd816aa` (product commit `70a5819ed`), kanban/ excluded:
`tools/dev-cli/services/client-name-scan.cs`, `tests/tools/dev-cli-tests/client-name-scan-tests.cs`,
`tests/tools/dev-cli-tests/entra-tenant-tests.cs`

## Summary

The fix pass resolves both round-1 findings. The two Entra case tests now use
`aaaaaaaa-0000-0000-0000-000000000001` against `AAAAAAAA-0000-0000-0000-000000000001`, so an
ordinal compare would fail them, and the union still asserts the first casing (M1). The scan
file set is now built from the pack csproj: `Content` items (unless `Pack=false`) and `None`
items with `Pack=true` (attribute or child element), minus each item's `Exclude`, with the
existing `Skip` rules on top (M2). An include or exclude that uses `$(…)` or escapes the repo
fails the scan instead of being silently skipped. That fails closed, which matches the
"add a build check" rule.

I checked the real csproj against the parser: every item uses `Include`/`Exclude` attributes with
`..\..\..\` prefixes. `readme.md` uses a `<Pack>True</Pack>` child, and `logo.png` uses a
`Pack="true"` attribute. `timewarp-templates/Directory.Build.props` defines no items. The
new tests pin the real set (`RealPack_Should_CoverAllowListPaths`) and the parser
semantics on a fixture (`AllowList_Should_ListIncludedFilesAndSkipTheRest`,
`PropertyInclude_Should_Fail`).

Re-ran in this worktree:
- `dotnet run tools/dev-cli/dev.cs -- check-client-names` printed `Client-name scan passed.`, exit 0.
- `tests/tools/dev-cli-tests` (`dotnet run`): 353 total, 353 succeeded, 0 failed.

Observations that are not findings:
- `FindHits` (root-directory walk) now has no production caller. Only the four `FindHits_Given_`
  tests use it. The skip rules they cover (`analysis/`, binary files) live in the shared `Skip` and
  `Collect`, which the production path `FindHitsInFiles` also calls, so the behavior is still
  covered. Removing the method later is optional.
- The task.md Results text calls the test cases `ClientNameScan_`. The classes are
  `FindHits_Given_`, `Unpack_Given_`, `PackedFiles_Given_`, `Names_Given_`, `Scan_Given_`, and
  `Ci_Given_` in `client-name-scan-tests.cs`. This is a kitchen wording slip, and the Results
  update below corrects it.

## Issues

None new on the fix delta.

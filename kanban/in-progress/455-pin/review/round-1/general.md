# Round 1 — general
**Date:** 2026-10-29
**Scope reviewed:** branch `task/455-pin` vs `master` (same as framework)

## Summary

The product change is one line in the root `Directory.Packages.props`. It replaces `Version="5.0.0"` with `Version="[5.0.0]"` for `Ardalis.GuardClauses`, so NuGet can no longer select a later (commercial) release. Risk is very low. This was checked against the repo:

- Only `Directory.Packages.props:68` declares the version. The two consumers, `foundation-contracts.csproj:12` and `web-spa.csproj:43`, carry versionless `PackageReference`s and have no `VersionOverride`.
- No other `PackageVersion` changed. The merge from `origin/master` added no product delta to the branch.
- `dotnet nuget why source/foundation/foundation-contracts/foundation-contracts.csproj Ardalis.GuardClauses`, re-run by the reviewer, prints `Ardalis.GuardClauses@5.0.0 (= 5.0.0)`.
- The resolved version stayed at 5.0.0, so compile inputs did not change. The reviewer did not re-run the full build and relies on the implementer's `./bin/dev build --quiet` result for that.

The implementer's `## Goal (implementer restatement)` matches the task: exact `[5.0.0]` pin, no other package moves, restore resolves 5.0.0, clean build. No misread.

## Issues

<!-- None raised. -->

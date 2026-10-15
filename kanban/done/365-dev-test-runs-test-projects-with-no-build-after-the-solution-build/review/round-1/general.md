# Round 1 — general
**Date:** 2026-10-15
**Scope reviewed:** same as framework (6 files, 184 insertions / 8 deletions)

## Summary

The change adds an opt-in `--no-build` flag to `dev test`. The flag is wired into `dev workflow`
right after Clean and the Release solution build. Argument construction is a pure helper
(`DotNetTestArguments`) with Jaribu coverage. Only projects listed in the .slnx skip the build.
Unlisted projects (the Jaribu aggregators and timewarp-testing-tests) still build inside their own
`dotnet test`, so stale or missing binaries cannot be run. Risk is low.

Verified against the repo:
- The solution path `timewarp-architecture.slnx` matches `build-command.cs:65/78`. The template
  `sourceName` is `TimeWarp.Architecture`, so generated apps keep this filename.
- The build uses `Release`, the same as the test arguments.
- The .slnx has no `<Build Project="false"/>` entries, so every listed project is built.
- Template conditionals are XML comments around `Project` elements, so parsing the monorepo
  .slnx lists every project.
- `Directory.GetFiles` returns full paths under the git root, and `Path.GetFullPath` normalizes both
  sides of the lookup.
- `dev-cli-tests`: 231/231 passed. `ganda repo audit` passed.

## Issues

None.

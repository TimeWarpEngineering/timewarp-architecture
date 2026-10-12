# Round 1 — general
**Date:** 2026-10-13
**Scope reviewed:** branch vs master (`Directory.Packages.props`, task.md)

## Summary

The change moves the two central Nuru pins (`TimeWarp.Nuru`, `TimeWarp.Nuru.DevCli`) from 3.0.0-beta.79 to 3.0.0-beta.80 in `Directory.Packages.props`. `git grep` finds no other Nuru version pins (runfile `#:package` directives, template packs, `msbuild/timewarp-platform-packages.props`) and no leftover `beta.79` references, so nothing else has to change with it. Re-checked: `ganda repo audit` passes all checks (nuru up to date), and `dotnet build tools/dev-cli/dev.cs -c Release` succeeds. The task.md Results cover gates, the consumer-visible delta (`--prerelease` on `dev release`), and validation steps. Risk is low.

## Issues

None.

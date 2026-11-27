# Round 1 — general
**Date:** 2026-11-27
**Scope reviewed:** branch `task/535-compile-tools-dev-cli-dev-cs-non-aot-during-dev` vs `master` (three-dot), plus `## Goal (implementer restatement)`, the original task, and `## Results` / `### How to validate`.

## Summary

`BuildCommand` now compiles `tools/dev-cli/dev.cs` with `dotnet build -c Release` as its last step and cleans that runfile under `--clean`, through the same `ExecuteAsync` capture/stream path as the solution and the unlisted test projects. The path and the step line live on `BuildOutput`; template smoke reads that path constant, and `dev-cli-tests` pin that the path is the real entry point and that the step line names the runfile and says there is no NativeAOT link.

The task's example `-p:PublishAot=false` is the allowed equivalent: `tools/dev-cli/Directory.Build.props` keeps `PublishAot` true so the AOT and trim analyzers still run, and a plain build does not publish, so it does not pay the NativeAOT link or write `./bin/dev`. The restatement matches the original task. Recorded gates cover the change: `./bin/dev build` (0 warnings, `./bin/dev` inode and mtime unchanged), a removed-after fail-fast probe (`CS0029` and `Build failed: tools/dev-cli/dev.cs`), warnings-as-errors and `TWA0004` on a direct runfile build, `./bin/dev build --clean`, `./bin/dev test --project dev-cli-tests` (469/469), `./bin/dev template-smoke` (generated `dev build` included), and a passing `ganda repo audit`. No product defect in the diff.

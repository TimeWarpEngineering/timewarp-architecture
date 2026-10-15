# Round 1 — general
**Date:** 2026-10-15
**Scope reviewed:** `.github/workflows/workflow.yml` diff in 2bd5031d9 vs master, plus call sites in `tools/dev-cli` (template-smoke) and root `Directory.Build.props`.

## Summary

The change makes every checkout shallow and adds an exact-key NuGet cache to the ci-group and template-smoke jobs, with a read-only restore in release. Checked claims:
- detect-paths fetches `$BASE` and `$HEAD` by SHA at depth 1. `git diff` of two commits needs only their trees. A failed fetch falls back to enable-all, the same path as an unknown base.
- The `2.0.0-smoke` constant matches `SmokePackageVersion` in `tools/dev-cli/endpoints/template-smoke-command.cs:56`. NuGet's lowercase `<id>/<version>` layout matches the `~/.nuget/packages/*/2.0.0-smoke` glob.
- The eviction step runs before the cache post-step. `actions/cache` saves only when the job succeeds.
- The monorepo build uses ProjectReference for the platform packages (`Directory.Build.props` Use*Packages detection). The local-feed packs are not restored into the ci-group global cache, so the cache holds only nuget.org packages.
- With no `restore-keys`, a CPM change cannot layer onto a stale cache.
- Concurrent ci-group saves on the same key race harmlessly: the losers log a warning and skip.

Risk is low. No issues found.

## Issues

<!-- none -->

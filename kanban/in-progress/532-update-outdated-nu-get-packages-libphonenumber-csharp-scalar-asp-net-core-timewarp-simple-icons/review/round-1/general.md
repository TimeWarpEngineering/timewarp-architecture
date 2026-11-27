# Round 1 — general
**Date:** 2026-11-27
**Scope reviewed:** `Directory.Packages.props` vs `master`, the Scalar / phone / simple-icons call sites, the Nuru 4.0.0-beta.18 nuspec and DevCli content delta, and `## Results` / `### How to validate` on task 532.

## Summary

The three pins the task names are current, the Scalar comment names 3.3.0, and `source/Directory.Build.props` `<Version>` is still `2.0.0-beta.32`. No call site changed. `UseScalarApiReference` still calls `WithTitle`, `AddDocument`, `SortTagsAlphabetically`, `ExpandAllTags`, and `AddPreferredSecuritySchemes`, and those methods are on `Scalar.AspNetCore` 3.3.0. `timewarp-simple-icons` is only a package reference. The Nuru beta.18 bump is the audit boy-scout the Results describe: its nuspec floors match the pins that beta.17 already required, and the DevCli content delta is the audit pass-cache, not the release pipeline. The recorded gates cover the change. No findings.

## Issues

<!-- None. -->

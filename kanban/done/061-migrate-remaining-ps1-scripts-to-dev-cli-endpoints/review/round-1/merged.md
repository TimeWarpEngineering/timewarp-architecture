# Round 1 — merged findings
**Date:** 2026-09-21
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 1 | 0 |
| suggestion | 0 | 0 | 0 |
| nit | 0 | 0 | 0 |

## Issues

### M1 — Severity: bug — Status: fixed
- File: tools/dev-cli/endpoints/template-install-command.cs:74-137
- Description: `template-install` creates `artifacts/template-install/` but never clears it before pack. Selection then does `OrderByDescending(path, Ordinal)` and claims the "newest" match. Lexicographic order is not NuGet SemVer: with both `TimeWarp.Architecture.2.0.0-beta.9.nupkg` and `TimeWarp.Architecture.2.0.0-beta.10.nupkg` present, `beta.9` sorts after `beta.10` and wins, so a later `dev template-install` after a `Directory.Build.props` bump can silently `dotnet new install` the stale package. Sibling `template-smoke-command.cs` avoids this by deleting `PackagesDir` before packing; the deleted `build-and-install-template.ps1` had the same Sort-Object-Name flaw, and this port was the moment to fix it. Analyzers/Generators/Attributes filtering via `TemplateNupkg.IsArchitectureTemplateNupkgFileName` is fine; leftover same-id version packs are the failure mode. Handler selection is also untested (tests only cover the file-name predicate).
- Suggestion: Before pack, wipe `PackagesDir` (or delete matching `TimeWarp.Architecture.*.nupkg` / `.snupkg` there) like `template-smoke`, then install the sole remaining architecture nupkg (or select by `LastWriteTimeUtc` / parsed `NuGetVersion` if leftovers must stay). Add a small unit test over a temp dir with `beta.9` + `beta.10` fixtures asserting the just-packed / highest SemVer file wins.
- Source: general
- Disposition notes: Wiped `PackagesDir` before pack (same as template-smoke). Moved selection to `TemplateNupkg.FindArchitectureTemplateNupkg` using LastWriteTimeUtc so a just-packed file still wins if leftovers remain. Tests: missing dir → null; beta.9 + newer beta.10 + Analyzers → beta.10. dev-cli-tests 68 passed.

## Duplicates / conflicts

- None (single reviewer).

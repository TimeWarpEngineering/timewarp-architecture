# Round 1 — general
**Date:** 2026-09-21
**Scope reviewed:** Product files in `review-framework.md` (db-ef / db-add-migration / db-group, template-nupkg / template-install, tests, skill, deleted `scripts/**` + two template `.ps1`), plus sibling comparison to `db-update` and `template-smoke`.

## Summary

The change correctly ports design-time EF scaffolding to `dev db add-migration` (kebab `source/container-apps/...` paths, C# identifier gate, `--dry-run` that skips restore/ef) and local template install to `dev template-install` (pack under `artifacts/template-install/`, digit-prefix nupkg filter so Analyzers/Generators/Attributes cannot win). Zero `.ps1` remain; deleted scripts look dead or superseded by existing AppHost `db` verbs; BannedSymbols/`ITerminal`+Amuru usage holds; helper tests cover paths/identifier/`BuildAddMigrationArguments` and the nupkg filter. Overall risk is low. The main gap is that `template-install` accumulates packs and then picks by lexicographic path order without wiping the output dir the way `template-smoke` does, so a leftover older nupkg can be installed after a version bump.

## Issues

### Issue 1 — Severity: bug
- File: tools/dev-cli/endpoints/template-install-command.cs:74-137
- Description: `template-install` creates `artifacts/template-install/` but never clears it before pack. Selection then does `OrderByDescending(path, Ordinal)` and claims the "newest" match. Lexicographic order is not NuGet SemVer: with both `TimeWarp.Architecture.2.0.0-beta.9.nupkg` and `TimeWarp.Architecture.2.0.0-beta.10.nupkg` present, `beta.9` sorts after `beta.10` and wins, so a later `dev template-install` after a `Directory.Build.props` bump can silently `dotnet new install` the stale package. Sibling `template-smoke-command.cs` avoids this by deleting `PackagesDir` before packing; the deleted `build-and-install-template.ps1` had the same Sort-Object-Name flaw, and this port was the moment to fix it. Analyzers/Generators/Attributes filtering via `TemplateNupkg.IsArchitectureTemplateNupkgFileName` is fine; leftover same-id version packs are the failure mode. Handler selection is also untested (tests only cover the file-name predicate).
- Suggestion: Before pack, wipe `PackagesDir` (or delete matching `TimeWarp.Architecture.*.nupkg` / `.snupkg` there) like `template-smoke`, then install the sole remaining architecture nupkg (or select by `LastWriteTimeUtc` / parsed `NuGetVersion` if leftovers must stay). Add a small unit test over a temp dir with `beta.9` + `beta.10` fixtures asserting the just-packed / highest SemVer file wins.
- Status: open

# Round 1 — general
**Date:** 2026-07-26
**Scope reviewed:** commit 70a45d80

## Summary

Implementation matches the task brief. Re-verified against the tree (not the commit message alone):

1. **Symbols gone.** `.template.config/template.json` declares only `grpc` / `api` / `web` / `yarp` / `postgres`. No `foundationPackages` / `analyzerPackages` / `identityPackages` keys or conditions. Residual mentions are intentional (smoke assert allowlist, Design region, kanban history).

2. **Excludes complete and unconditional.** A single `(true)` modifier excludes `source/foundation/**`, `source/libraries/timewarp-modules/**`, `source/libraries/timewarp-identity/**`, `source/analyzers/**`, and the matching `tests/foundation/**`, `tests/libraries/timewarp-identity-tests/**`, `tests/analyzers/**` trees. Matches `VendoredPlatformRelativeTrees` in template-smoke.

3. **Monorepo `Use*Packages` intact.** Root `Directory.Build.props` still auto-detects all three switches from source-tree existence; dual-mode `ProjectReference` / `PackageReference` / MSBuild `<Using>` consumers remain. No CPM pins or `timewarp-platform-packages.props` composition values were disturbed. Product csproj dual-mode comments now correctly say generated apps are always package-mode.

4. **Docs.** `AGENTS.md` platform section is package-mode-only for generated apps + monorepo dogfood. `HowToUpgradeToAnalyzerPackages.md` remains a migration guide for pre-existing vendored apps (appropriate). No live product/docs under `documentation/` still instruct `--*Packages false`.

5. **Scan design (RFC D4).** New independent pass `AssertNoUnsafePlatformNamespaceLiterals` — does **not** reuse `AssertPackageIdsNotRewritten` (which still deliberately omits `.cs`). Regex `TimeWarp\.Architecture\.(Analyzers|Generators|Attributes|TypedIds)\b` would catch `using TimeWarp.Architecture.TypedIds.Ef;` (a251980f class) under `source/container-apps/**`. Product namespaces such as `TimeWarp.Architecture.Features.*` do not match. Platform trees (`source/analyzers/**` with legitimate `namespace TimeWarp.Architecture.Analyzers` and the generator’s baked `EfNamespace` constant) are not scanned and are template-excluded — the adversarial scoping risk is closed by always-package-mode. Composed props (`$(_TwPlatformVendor).Architecture.*`) do not match the continuous-literal pattern. Post-generate `AssertGeneratedAppPackageMode` belts vendored trees, removed symbols (if `.template.config` lands in output), and rewritten `{appName}.(Analyzers|Generators|Attributes|TypedIds)` including `.cs`.

6. **slnx dual-use.** Platform Project entries were fully removed (not `#if (false)`-wrapped). Monorepo `dev build` still reaches foundation/analyzers/identity via product `ProjectReference`s; `dev test` globs `tests/` independent of the solution. Generated apps correctly receive a package-mode-only slnx. See Issue 1 for the residual monorepo DX cost.

7. **Residual `bin/dev`.** `./bin/dev` AOT binary can lag `tools/dev-cli/endpoints/template-smoke-command.cs`. CI and the workflow path use `dotnet run tools/dev-cli/dev.cs` (fresh). No stale product doc residual; local AOT users need re-self-install to exercise the new gates. See Issue 2.

No correctness bugs found in symbol removal, exclude completeness, monorepo dual-mode preservation, or the a251980f scan path.

## Issues

### Issue 1 — Severity: suggestion
- File: `timewarp-architecture.slnx`
- Description: Platform projects (foundation, analyzers, timewarp-modules, timewarp-identity, and the test projects that previously lived under the same template conditionals) were deleted from the dual-use slnx rather than retained under the plan’s dual-use fallback (`<!--#if (false) -->` … `<!--#endif -->`). Template conditionals are comments to the slnx parser, so `#if (false)` would keep monorepo solution membership while always stripping on generate. Full removal is consistent with Phase 0 when “monorepo doesn’t break,” and build/test still work (transitive refs + test glob), but monorepo IDE/solution-centric workflows no longer list platform packages as first-class projects.
- Suggestion: Prefer `#if (false)` wrappers for platform Project lines if monorepo solution membership matters; otherwise document that platform work is project-path / transitive only. Do not reintroduce `*Packages` symbols.
- Status: open

### Issue 2 — Severity: nit
- File: `tools/dev-cli/endpoints/template-smoke-command.cs` (runtime via `bin/dev`)
- Description: New smoke assertions live only in the runfile sources. A previously installed AOT `./bin/dev` will not run `AssertNoUnsafePlatformNamespaceLiterals` / `AssertRemovedPackageSymbolsGoneFromTemplateConfig` / `AssertGeneratedAppPackageMode` until re-self-install. CI is safe (`dotnet run tools/dev-cli/dev.cs -- template-smoke`).
- Suggestion: When validating locally after this change, use the runfile path or re-run `self-install` before trusting `./bin/dev template-smoke`.
- Status: open

### Issue 3 — Severity: suggestion
- File: `tools/dev-cli/endpoints/template-smoke-command.cs` (`SourceNameLiteralScanRelativeRoots` / `SourceNameLiteralScanRelativeFiles`)
- Description: Monorepo pre-scan roots follow the plan (`source/container-apps`, `tests/common`, `tests/container-apps`, `msbuild`, plus listed root files) but omit two packed dual-mode wiring files that every generated app inherits: `source/Directory.Build.props` and `tests/Directory.Build.props` (repo-wide analyzer `PackageReference` / `ProjectReference`). Both currently use `$(TwArchitectureAnalyzersPackageId)` — no live hit. A literal regression there would still fail post-generate `AssertPackageIdsNotRewritten` (`.props` in scope), so this is fail-fast completeness, not a silent miss of the package-id class. The a251980f `.cs` path under container-apps is covered.
- Suggestion: Add `source/Directory.Build.props` and `tests/Directory.Build.props` to `SourceNameLiteralScanRelativeFiles` so the pre-scan surface matches template-packed consumer wiring.
- Status: open

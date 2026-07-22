# Round 1 — general
**Date:** 2026-07-22
**Scope reviewed:** a3cecd6d..02e62ac3 template sourceName + Identity smoke fix

## Summary

Implementation matches the locked plan. Claimed ship items check out against the tree:

1. **sourceName-safe platform package IDs** — `msbuild/timewarp-platform-packages.props` composes IDs/namespaces as `$(_TwPlatformVendor).Architecture.*` (no continuous `TimeWarp.Architecture` package-id literal). Imported from both root `Directory.Build.props` and `Directory.Packages.props` (guarded). CPM `PackageVersion` and all template-relevant `PackageReference` / `PackageId` consumers use `$(TwArchitecture*PackageId)`. Template pack includes `msbuild/**`.

2. **Dual-mode Attributes / TypedId usings** — `global using TimeWarp.Architecture.Attributes` removed from web/api contracts; MSBuild `<Using>` switches on `UseAnalyzerPackages` (package → `$(TwArchitectureAttributesNamespace)`, source → `$(RootNamespace).Attributes`). TypedId root namespace dual-mode on `web-domain` / `timewarp-identity` (package → `$(TwArchitectureRootNamespace)`, source → sourceName-rewritten `TimeWarp.Architecture` literal — correct for identity, which has no product `RootNamespace`).

3. **identityPackages dual-mode default false** — `template.json` symbol default `"false"`; exclude of identity source only when `identityPackages` is true (no longer folded under foundation excludes). `UseIdentityPackages` existence-detect in `source/Directory.Build.props`. web-contracts / web-application / web-infrastructure gate Identity on `UseIdentityPackages`. slnx keeps a single `/source/libraries/` folder with independent `#if (!identityPackages)` nesting.

4. **template-smoke + CI** — `dev template-smoke` packs monorepo platform packages @ `2.0.0-smoke`, installs template, generates `SmokeDefault` / `SmokeNoPostgres` (names ≠ sourceName), asserts no `AppName.{Analyzers,Generators,Attributes}` rewrites in csproj/props/targets/slnx/json, rewrites CPM pins, local NuGet.config with packageSourceMapping, restore+build. Workflow path-filters cover template content + dev-cli.

5. **feature-membership.targets** — Error no longer depends on a condition the engine strips; intermediate `_HasUnmatchedFeatureFiles` uses raw `>` (valid XML attribute) then simple property equality on `<Error>`.

**Leftover continuous `TimeWarp.Architecture.*` package-id strings in template-packed MSBuild:** none in PackageReference/PackageVersion/PackageId. Remaining hits are (a) intentional product namespaces / sourceName-rewritable C# under `source/analyzers/**` (excluded when `analyzerPackages` default true), (b) one csproj comment under analyzers, (c) cosmetic `page-mixin.md` prose, (d) docs outside the template pack. Template package’s own `PackageId` (`TimeWarp.Architecture`) is the template identity, not generated-app content.

**Residual risk (ops, acknowledged in task notes — not a code defect in this change):** smoke packs monorepo platform bits so CI is green while published nuget.org pins can lag monorepo API surface (`Entity<TId>`, `EndpointAllowAnonymous`). Vanilla greenfield restore-from-nuget.org still needs republish follow-through.

## Issues

None.

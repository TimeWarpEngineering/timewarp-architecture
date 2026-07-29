# Extract shared template-smoke harness; derive rewrite-scan suffixes from props SSOT

## Parent

131

## Description

Collapse duplication between `template-smoke-command.cs` and
`template-publish-smoke-command.cs` (task 131 F-007). Release gates must share one harness
and derive rewrite-scan suffixes from `msbuild/timewarp-platform-packages.props` (126-006
style), not hand-maintained lists.

## Requirements

- One shared smoke-harness (assert helpers + generate/restore/build skeleton) consumed by
  both commands.
- All suffix / forbidden-rewrite lists derived from props SSOT (including InstallTemplate
  nupkg filter and post-generate checks) — not hand `ForbiddenRewrittenPackageFragments`
  in four places.
- Port namespace-literal scan to publish-smoke (today smoke-only).
- Use `IsBinObjOrArtifacts` consistently (both miss `artifacts` in inline skips today).
- Optional: fold F-012 shared analyzer-wiring props extract if convenient (detection already
  fixed under 131).

## Checklist

- [ ] Extract shared harness file
- [ ] Derive all rewrite/suffix lists from props
- [ ] Port namespace-literal scan to publish-smoke
- [ ] `dev template-smoke` (or equivalent) green
- [ ] `dev` publish-smoke path green when packages available

## Notes

Parent: F-007. Highest-stakes tooling — publish gate must not pass what smoke would fail.

### Implementation plan (2026-07-29)

#### Defaults
- New `tools/dev-cli/services/template-smoke-harness.cs` (`DevCli.Services`)
- Wire `services/**/*.cs` in `tools/dev-cli/Directory.Build.props` (today endpoints-only)
- Props derivation fail-closed; minimal set must include Analyzers/Attributes/Generators/TypedIds
- Pin fragments (`TwArchitecture*`, Foundation., Modules, Identity) one harness constant (not full props-derived)
- Port monorepo namespace pre-scan + rewrite asserts to publish; leave vendored-tree package-mode assert smoke-only
- Skip F-012 MSBuild extract

#### Shared API sketch
- `PlatformPackagePropsSsot.DeriveArchitectureSuffixes` → ToForbiddenRewrittenFragments / ToNupkgExcludeFragments
- `TemplateSmokePaths.IsBinObjOrArtifacts` for all walks
- `TemplateSmokeHarness`: AssertNoUnsafePlatformNamespaceLiterals, AssertPackageIdsNotRewritten,
  AssertNoRewrittenPlatformTokens…, RewriteCpmPins / TryEvaluatePlatformPins, SmokeOneAsync with callbacks

#### Steps
1. Scaffold harness + Directory.Build.props include
2. Move pure SSOT + path helpers
3. Move asserts + skeleton; rewire smoke then publish
4. Self-check minimal suffixes; Design regions
5. Verify: compile CLI; prefer `dotnet run tools/dev-cli/dev.cs -- template-smoke`; publish pre-scan path without packages if needed

#### Success criteria
- Zero hand ForbiddenRewritten arrays
- One PlatformPinIncludeFragments
- Publish runs monorepo namespace pre-scan
- All walks use IsBinObjOrArtifacts

## Session

- Created: 2026-07-28 — from task 131 disposition
- Plan: 2026-07-29 — tw-orchestrate-task Phase 2/3

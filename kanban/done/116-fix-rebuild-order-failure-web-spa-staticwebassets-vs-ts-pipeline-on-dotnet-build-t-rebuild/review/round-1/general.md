# Round 1 — general
**Date:** 2026-07-24
**Scope reviewed:** web-spa StaticWebAssets vs TS Rebuild fix

## Summary

The change in `source/container-apps/web/web-spa/web-spa.csproj` is a correct, minimal local workaround for the known Static Web Assets + `Microsoft.TypeScript.MSBuild` ordering hole.

**Upstream guidance match:** ASP.NET’s documented customer fix (javiercn on [microsoft/TypeScript#60538](https://github.com/microsoft/TypeScript/issues/60538)) is exactly:

1. Prepend TypeScript emit targets to `PrepareForBuildDependsOn` so outputs exist before SWA discovery.
2. `RemoveDuplicateTypeScriptOutputs` with `BeforeTargets="GetTypeScriptOutputForPublishing"` that does `<Content Remove="@(GeneratedJavascript)" />` so re-add does not create duplicate `Content` items.

The implemented snippet is **stricter than the minimal issue sample** (which only listed `CompileTypeScript; CompileTypeScriptWithTSConfig; GetTypeScriptOutputForPublishing`). Javier explicitly noted that sample was likely incomplete relative to `CompileDependsOn`. This fix mirrors the package’s full `CompileDependsOn` chain from `Microsoft.TypeScript.MSBuild` 6.0.3 (`tools/Microsoft.TypeScript.targets`):

- `CheckFileSystemCaseSensitive`
- `FindConfigFiles`
- `TypeScriptDeleteOutputFromOtherConfigs`
- `CompileTypeScript`
- `CompileTypeScriptWithTSConfig`
- `GetTypeScriptOutputForPublishing`

No TS targets from that chain are missing. Including the setup/delete-other-config steps is the right hardening.

**Content Remove correctness:** Item name is `GeneratedJavascript` (package spelling; also used by `GetTypeScriptOutputForPublishing` when it re-`Include`s into `Content`). Matches package + #60538 intent. MSBuild item names are case-insensitive, so the issue body’s `GeneratedJavaScript` typo would also work; the repo spelling is the accurate one.

**Double-compile cost:** Targets remain on both `PrepareForBuildDependsOn` (this change) and package `CompileDependsOn`. Within one Build evaluation, MSBuild skips targets already run successfully (“Previously built successfully”), so this should not double-invoke `tsc` on a normal build. Incremental TS targets still honor Inputs/Outputs. Residual cost is early scheduling + skip bookkeeping, not a second full compile. Acceptable for a temporary workaround.

**Design comment accuracy:** Root cause description is right for Rebuild = Clean;Build in one evaluation: evaluation-time `wwwroot/**` → `Content`, Clean’s `TypeScriptDeleteCompilerOutput` deletes emit, SWA/`DefineStaticWebAssets` then fails on missing files before late Compile re-emit. Citations to TypeScript#60538 and sdk#52301 are appropriate; sdk#52301’s proposed `Microsoft.NET.Sdk.StaticWebAssets.TypeScript.targets` and 11.0.1xx milestone match the “drop when SDK ships… .NET 11” note. `wwwroot/js` is gitignored at root (`.gitignore` line for web-spa). Do-not-commit and out-of-scope constraints align with the task plan.

**Ordering vs SWA:** PrepareForBuild is the ASP.NET-prescribed hook so emit lands before discovery; implementer verification (3× `dotnet build -t:Rebuild` on web-spa and web-server, JS re-emitted) supports that the hook is early enough in this SDK graph. Early emit also helps clean-build SWA discovery of TS outputs (sdk#52301 problem 1), not only Rebuild.

**Residual risks (not defects):**

- Local copy of the TS target list can drift if a future `Microsoft.TypeScript.MSBuild` changes `CompileDependsOn` — low probability; remove when SDK integration lands.
- Still emits into `wwwroot` (SWA input folder). That is the documented TypeScript + ASP.NET pattern and was out of scope vs obj/ re-homing.
- Until .NET 11 SWA+TS targets ship, this remains a project-local workaround that must travel with the template.

## Issues

None.

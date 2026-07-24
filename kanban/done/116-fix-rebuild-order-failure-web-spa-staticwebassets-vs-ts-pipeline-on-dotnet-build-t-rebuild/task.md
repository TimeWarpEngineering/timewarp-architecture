# Fix rebuild-order failure web-spa StaticWebAssets vs TS pipeline on dotnet build -t Rebuild

## Description

`dotnet build -t:Rebuild` on any graph including web-spa intermittently/reliably fails:
Rebuild cleans wwwroot/js outputs (TS pipeline), and Microsoft.NET.Sdk.StaticWebAssets'
DefineStaticWebAssets then throws `No file exists for the asset ... wwwroot/js/features/counter.js`
before the TS compile re-emits it. Found 2026-07-22 during 114-002 review; retroactively
explains the 114-001 spike's 'intermittent -t:Rebuild 1 Error' mystery (previously suspected
output race — wrong). Pre-existing, unrelated to axis-1 work. Fix direction: ensure TS-compile
target runs before StaticWebAssets collection after clean (BeforeTargets ordering), or exclude
the generated js from Clean, or emit to obj/ and map as static web asset properly (relates to
the web-spa TS pipeline notes in memory/058-001 hardening).

## Checklist

- [x] Reproduce deterministically (dotnet build -t:Rebuild web-spa / web-server graph)
- [x] Fix target ordering (or asset mapping) so Rebuild is reliable
- [x] Verify: 3 consecutive -t:Rebuild runs green; note in web-spa csproj Design comments

## Notes

Captured log: StaticWebAssets.targets(706,5) InvalidOperationException on counter.js. Coordinates
with 058-001 (test-host hardening) but is a build-graph issue, not test infra.

### Implementation plan (Phase 2, 2026-07-24)

**Root cause (reproduced):** Rebuild = Clean;Build in one evaluation. SWA globs `wwwroot/**` into
`Content` at evaluation; Clean deletes `wwwroot/js` via `TypeScriptDeleteCompilerOutput`;
`ResolveProjectStaticWebAssets` runs before Compile/TS re-emit → `DefineStaticWebAssets` throws.

**Fix:** Prepend full TypeScript chain to `PrepareForBuildDependsOn` +
`RemoveDuplicateTypeScriptOutputs` (ASP.NET TypeScript#60538 / sdk#52301). Design comment in
`web-spa.csproj`. Dropped unused `TypeScriptInputs`.

## Results

**Completed 2026-07-24** — web-spa `-t:Rebuild` reliable; StaticWebAssets no longer races TS Clean.

### What was implemented
- `PrepareForBuildDependsOn` runs TS pipeline before SWA discovery
- `RemoveDuplicateTypeScriptOutputs` de-dupes Content before re-publish
- Design comment documents root cause + upstream links + .NET 11 exit path

### Files
- `source/container-apps/web/web-spa/web-spa.csproj`

### Tests / verification
- Repro before fix: Rebuild failed on missing `wwwroot/js/features/counter.js`
- After: 3× web-spa Rebuild green; 3× web-server Rebuild green; normal build green
- Outputs present: counter.js, spa.js, web.spa.lib.module.js

### Phase 4b review
- Effort 1 (general); 1 round; **0 open**
- Disposition: **clean** (`review/disposition.md`)

### Commit
- `cde14f75` fix(web-spa): run TypeScript before StaticWebAssets on Rebuild

## Session

- Created: 2026-07-22 (from 114-002 review)
- Plan + implement + review: 2026-07-24 (orchestrator)

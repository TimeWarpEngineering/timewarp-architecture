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
- [ ] Fix target ordering (or asset mapping) so Rebuild is reliable
- [ ] Verify: 3 consecutive -t:Rebuild runs green; note in web-spa csproj Design comments

## Notes

Captured log: StaticWebAssets.targets(706,5) InvalidOperationException on counter.js. Coordinates
with 058-001 (test-host hardening) but is a build-graph issue, not test infra.

### Implementation plan (Phase 2, 2026-07-24)

**Root cause (reproduced):** Rebuild = Clean;Build in one evaluation. SWA globs `wwwroot/**` into
`Content` at evaluation; Clean deletes `wwwroot/js` via `TypeScriptDeleteCompilerOutput`;
`ResolveProjectStaticWebAssets` runs before Compile/TS re-emit → `DefineStaticWebAssets` throws.
`dotnet clean` then `dotnet build` re-evaluates and often works (JS not in Content).

**Repro evidence (2026-07-24):**
```
dotnet build web-spa.csproj -t:Rebuild
→ InvalidOperationException: No file exists for … wwwroot/js/features/counter.js
```

**Fix (lean):** ASP.NET guidance (TypeScript#60538 / sdk#52301):
1. Prepend full TypeScript chain to `PrepareForBuildDependsOn` so emit runs before SWA discovery
2. `RemoveDuplicateTypeScriptOutputs` BeforeTargets=`GetTypeScriptOutputForPublishing`
3. Design comment in csproj; optional drop dead `TypeScriptInputs`

**Out of scope:** emit to obj/, commit wwwroot/js, disable SWA, wait for SDK 11, npm return.

## Session

- Created: 2026-07-22 (from 114-002 review)
- Plan + repro: 2026-07-24 (orchestrator)

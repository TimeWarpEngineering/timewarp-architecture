# Review framework — task 126-007

**Date:** 2026-07-27
**Host task:** kanban/in-progress/126-007-generate-assemblymarker-and-internalsvisibleto-via-msbuild-kill-checked-in-boilerplate/
**Diff scope:** commit `6b1d8c3c` — feat(build): generate IAssemblyMarker via MSBuild
**Plan / brief:** MSBuild GenerateAssemblyMarker; AssemblyMarkerNamespace maps; delete 26 markers; IVT SDK items; SPA normalize to interface; pack Directory.Build.targets in template; AGENTS + ADR-0002
**Effort:** 1 (general only)
**Reviewer roster:** general
**Session IDs:** orchestrator grok-build 2026-07-27

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`

## Claims to verify

- Cannot use RootNamespace alone → AssemblyMarkerNamespace maps correct for all 26
- Generated file uses auto-generated header / .g.cs; TWA0004 skip
- Semicolons escaped for MSBuild item separator
- Template packs Directory.Build.targets; smoke generates SmokeDefault.Web.Server markers
- IVT uses real AssemblyNames
- SPA consumers use IAssemblyMarker; State.AssemblyMarker untouched
- Aspire / convention-analyzers opted out

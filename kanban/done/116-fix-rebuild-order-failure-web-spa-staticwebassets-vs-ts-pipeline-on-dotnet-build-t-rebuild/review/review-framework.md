# Review framework — task 116

**Date:** 2026-07-24
**Host task:** kanban/in-progress/116-fix-rebuild-order-failure-web-spa-staticwebassets-vs-ts-pipeline-on-dotnet-build-t-rebuild/
**Diff scope:** `web-spa.csproj` PrepareForBuildDependsOn + RemoveDuplicateTypeScriptOutputs + Design comment
**Plan / brief:** task.md — early TS before SWA discovery; no obj/ emit; no commit of wwwroot/js
**Effort:** 1 (general only)
**Reviewer roster:** general
**Session IDs:** orchestrator 2026-07-24

## Ground rules

- Reviewers are read-only on product code; write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues; zero issues is valid
- Re-verify claims against the repo

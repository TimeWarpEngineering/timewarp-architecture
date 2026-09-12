# Review framework — task 006-001

**Date:** 2026-09-12
**Host task:** kanban/in-progress/006-001-fastendpoint-scan-only-marked-contracts-assemblies/
**Diff scope:** branch `task/006-001-fastendpoint-scan-only-marked-contracts-assemblies` vs `origin/master` (product commit `1be4a803`)
**Plan / brief:** `FastEndpointSourceGenerator` (and ingress / mock-registry) must walk only assemblies stamped `[assembly: ApiEndpointsEmbedded]`, not the full MSBuild reference closure. `ApiEndpointContractAssemblies` is a required AssemblyName allow-list (`web-contracts` / `api-contracts`); empty, mistyped, or unmarked names report **TWE008** (Error) and emit nothing. Equatable `GenerationOptions` + content-equal `EndpointEmitModel` so `.Collect()` does not rebuild every `*Endpoint.g.cs` on unrelated host edits. Ingress and mock-registry share the same marked-assembly filter (no fourth participate-rule). Docs use real AssemblyName, not `TimeWarp.Architecture.Web.Contracts`. Out of scope: mixin SyntaxProvider, route parser, fail-closed auth (task 110).
**Effort:** 1 (general only)
**Reviewer roster:** general
**Session IDs:** Review oracle Grok session `01a0948a-fd36-7f60-9452-18d38f9c349a` (2026-09-12)

Round 2 re-review (same effort/roster): verify M1/M2 against the post-fix delta (TWA0019 marked-name check + unmarked ingress test; mock comment). Prior `round-1/` is frozen.

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`

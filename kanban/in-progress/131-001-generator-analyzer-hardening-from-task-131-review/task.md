# Generator analyzer hardening from task 131 review

## Parent

131

## Description

Implement the generator/analyzer cluster from the task 131 full-repo code review
(findings F-003, F-004, F-005, F-008, F-014). See parent
`disposition.md` and `findings.md`.

## Requirements

- **F-003:** Delete static `RouteRegistry`; per-compilation `.Collect()` + in-batch
  route+verb duplicate reporting (also fixes IDE incremental self-conflict).
- **F-004:** Shared hosted-route discovery as **linked source** into convention-analyzers
  and generators packages (flag-parameterized); fix live
  `[ApiEndpoint]`+`[ClientOnlyContract]` contradiction; dedupe `GetAllNamespaces`.
- **F-005:** Delete `ApiEndpointAttribute.EndpointType` and dead extraction/emission;
  fix `ApiEndpointSourceGenerator.md` that teaches the dead API. Do **not** implement
  the override (YAGNI).
- **F-008:** Fail closed on unrecognized `HttpVerb` (no default to GET); cover
  `ResolveHttpVerbName` fallback; Head/Options already exist on the enum.
- **F-014:** Document TWE/SG IDs in AGENTS.md; consolidate TWE registry (or drop false
  authority claim); wire-or-delete unused TWE001/002/004; dedupe dual SG001.

## Checklist

- [ ] F-003 per-compilation route conflict detection
- [ ] F-004 shared discovery + ClientOnly×ApiEndpoint fix
- [ ] F-005 delete EndpointType + docs
- [ ] F-008 fail-closed verbs
- [ ] F-014 document/consolidate diagnostic IDs
- [ ] `dev build` 0/0; analyzer/generator tests green

## Notes

Parent review: `kanban/in-progress/131-complete-repo-code-review-by-kimi-k3/`.
Verification: `review/round-1/claude-verification.md`, `grok-verification.md`.

### Implementation plan (2026-07-29)

#### Defaults (accepted without further ballot)

- **TWA0020** for `[ApiEndpoint]` × `[ClientOnlyContract]` (Warning, auth-posture family).
- ClientOnly = outer **or** nested Query/Command for generators and TWA0006.
- **TWE001/TWE004 deleted**; **TWE002 wired** (missing Query/Command); **TWE007** unknown verb.
- Route conflict: TWE003 on **all** parties; generate **none** of the group.
- No EndpointType reimplementation; no TWE→TWA renumber.

#### Ordered steps

0. **Linked shared source:** create `source/analyzers/shared/hosted-route-discovery.cs`;
   `<Compile Include="..\shared\..." Link="shared\..."/>` on both analyzers + convention-analyzers
   csprojs. Namespace `TimeWarp.Architecture.Analyzers`, internal helpers.
1. **F-005:** delete `EndpointType` from attribute, metadata, generator emission; fix
   `ApiEndpointSourceGenerator.md` (and generator package doc if needed).
2. **F-008:** fail-closed verb resolve/convert; allow-list includes Head/Options; report
   TWE007; update generator harness enum.
3. **F-003:** delete `route-registry.cs`; rewrite FastEndpoint pipeline to equatable emit
   models + `.Collect()` + batch TWE003; no symbols in collected model; SG002 once per batch.
4. **F-004:** wire HostedRouteDiscovery into FastEndpoint, ingress, coverage; TWA0020 in
   endpoint-auth-posture analyzer (or sibling); rewrite ingress Design prose.
5. **F-014:** consolidate TWE/SG into `diagnostic-descriptors.cs`; delete unwired TWE001/004;
   move TWE005/006/SG010/011/SG002; one SG001; AGENTS.md tables; Unshipped releases.
6. **Tests + gates:** update conflict tests (all parties, zero sources); ClientOnly skip;
   TWA0020; TWE002/007; Head/Options; dual-run stability. Full rebuild; both analyzer
   test projects green; `dev build` 0/0.

#### Critical paths

- `generators/fast-endpoint-source-generator.cs`, `models/endpoint-metadata.cs`
- `shared/hosted-route-discovery.cs` (new)
- `convention/endpoint-coverage-analyzer.cs`, `endpoint-auth-posture-analyzer.cs`
- `attributes/api-endpoint-attribute.cs`, `diagnostics/diagnostic-descriptors.cs`
- `ingress-route-prefix-generator.cs`, `AGENTS.md`, reference docs
- `tests/analyzers/timewarp-architecture-{sourcegenerator,analyzers}-tests/`

#### Suggested commits

1. shared HostedRouteDiscovery skeleton + csproj Link  
2. F-005 remove EndpointType  
3. F-008 fail-closed verbs  
4. F-003 Collect route conflicts  
5. F-004 discovery + TWA0020  
6. F-014 taxonomy + AGENTS  
7. remaining tests if not folded earlier  

#### Non-goals

Identity de-dup (131-002), smoke harness (131-003), transport core (131-004), custom
endpoint bases, cross-package ProjectReference between Analyzers and Generators.

## Session

- Created: 2026-07-28 — from task 131 disposition
- Plan: 2026-07-29 — tw-orchestrate-task Phase 2/3

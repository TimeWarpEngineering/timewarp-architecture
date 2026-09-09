# FastEndpoint scan only marked contracts assemblies

## Parent

006

## Description

`FastEndpointSourceGenerator` (and ingress / mock-registry) walk **every
namespace of every referenced assembly** looking for `[ApiEndpoint]`, then
`.Collect()`. That is `IIncrementalGenerator` in name only: any host
compilation change rescans the world.

TypedId already avoids this with an assembly marker
(`TypedIdsEmbedded`). Mirror that for hosted contracts.

web-server today sets `ApiEndpointContractAssemblies=web-contracts`
(the **AssemblyName**, not the docs string
`TimeWarp.Architecture.Web.Contracts`). Empty filter (api-server)
scans **all** refs — spa transitive api-contracts can emit foreign
endpoints.

## Requirements

- Contracts assemblies that host `[ApiEndpoint]` emit an assembly marker
  (TypedId pattern). Host generators walk **only marked** refs.
- Keep / require `ApiEndpointContractAssemblies` as an allow-list of
  **AssemblyName** values; miss should fail closed (ingress already has
  TWA0019-style — FastEndpoint should too), not silently emit nothing.
- Equatable options + content-equal collected model so `.Collect()` does
  not rebuild every `*Endpoint.g.cs` on unrelated host edits.
- Ingress + mock-registry should share the same “marked assembly”
  filter (task **131-001** already linked `HostedRouteDiscovery` — do not
  grow a fourth participate-rule).
- Docs: real AssemblyName (`web-contracts`), not the fictional
  `TimeWarp.Architecture.Web.Contracts`.

### Not in scope

- Mixin attributes / SyntaxProvider (**053-004**, **053-005**)
- Route parser (**053-003**)
- Changing fail-closed auth (task **110**)

## Checklist

- [ ] Assembly marker on contracts projects that should host endpoints
- [ ] FastEndpoint (and ingress/mock if they share discovery) skip unmarked refs
- [ ] Wrong/missing `ApiEndpointContractAssemblies` fails closed
- [ ] Docs match AssemblyName
- [ ] Results + How to validate

## Session

- Created: 2418679 (2026-09-09)
- Cockpit: timewarp-flow Grok `01a03d38-9611-7620-aae5-848e15dafa94`

## Notes

- File: `source/analyzers/timewarp-architecture-analyzers/generators/fast-endpoint-source-generator.cs`
- Pattern: `source/analyzers/.../typed-id-source-generator.cs` assembly marker.
- Related done: **131-001** (equatable EndpointEmitModel + Collect, still
  walks all namespaces), **054** (opt-in EnableApiEndpointGeneration),
  **107** (ingress scan).

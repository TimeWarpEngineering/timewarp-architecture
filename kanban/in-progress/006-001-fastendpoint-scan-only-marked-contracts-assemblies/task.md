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

- [x] Assembly marker on contracts projects that should host endpoints
- [x] FastEndpoint (and ingress/mock if they share discovery) skip unmarked refs
- [x] Wrong/missing `ApiEndpointContractAssemblies` fails closed
- [x] Docs match AssemblyName
- [x] Results + How to validate

## Session

- Created: 2418679 (2026-09-09)
- Cockpit: timewarp-flow Grok `01a03d38-9611-7620-aae5-848e15dafa94`
- Implementer: Grok session `01a09476-7d7f-74c0-b0d5-d7ed52639824` (2026-09-12)

## Notes

- File: `source/analyzers/timewarp-architecture-analyzers/generators/fast-endpoint-source-generator.cs`
- Pattern: `source/analyzers/.../typed-id-source-generator.cs` assembly marker
  *filter* (walk only stamped refs). Product contracts stamp via the public
  attributes-package type rather than attaching Generators to contracts
  (that package also ships TWA0001 / PartialClassDeclarationAnalyzer).
- Related done: **131-001** (equatable EndpointEmitModel + Collect, still
  walked all namespaces), **054** (opt-in EnableApiEndpointGeneration),
  **107** (ingress scan).

## Results

Host generators walk only assemblies stamped `[assembly: ApiEndpointsEmbedded]`.
`ApiEndpointContractAssemblies` is a required AssemblyName allow-list when
generation is enabled; empty, mistyped, or unmarked names report **TWE008**
(Error) and emit nothing.

### What landed

- Public `ApiEndpointsEmbeddedAttribute` in TimeWarp.Architecture.Attributes.
  `web-contracts` and `api-contracts` apply `[assembly: ApiEndpointsEmbedded]`.
- `HostedRouteDiscovery.HasApiEndpointsEmbedded` /
  `GetMarkedReferencedAssemblies` — shared filter for FastEndpoint, ingress,
  and mock-registry (no fourth participate-rule).
- FastEndpoint: skip unmarked refs; require allow-list; **TWE008** fail-closed.
  Generator still stamps an internal copy when the current compilation
  declares `[ApiEndpoint]` (test harness).
- Equatable `GenerationOptions`; `EndpointEmitModel` SequenceEquals `Tags`
  so `.Collect()` does not rebuild `*Endpoint.g.cs` on identical content.
- `api-server` sets `ApiEndpointContractAssemblies=api-contracts`.
- Docs use AssemblyName `web-contracts`, not `TimeWarp.Architecture.Web.Contracts`.

### Files changed

- `source/analyzers/shared/hosted-route-discovery.cs`
- `source/analyzers/timewarp-architecture-analyzers/generators/fast-endpoint-source-generator.cs`
- `source/analyzers/timewarp-architecture-analyzers/generators/ingress-route-prefix-generator.cs`
- `source/analyzers/timewarp-architecture-analyzers/generators/mock-response-factory-registry-generator.cs`
- `source/analyzers/timewarp-architecture-analyzers/models/endpoint-metadata.cs`
- `source/analyzers/timewarp-architecture-analyzers/diagnostics/diagnostic-descriptors.cs`
- `source/analyzers/timewarp-architecture-attributes/api-endpoints-embedded-attribute.cs`
- `source/container-apps/web/projects/web-contracts/api-endpoints-embedded.cs`
- `source/container-apps/api/projects/api-contracts/api-endpoints-embedded.cs`
- `source/container-apps/api/projects/api-server/api-server.csproj`
- tests + AGENTS.md + api-endpoint-source-generator.md + ADR-0007 + skill

### Key decisions

- Do **not** attach `TimeWarp.Architecture.Generators` to contracts: that
  package includes TWA0001, which is not a contracts-file rule (verified:
  attaching it produced 76 TWA0001 errors on web-contracts).
- Marker match is simple name, so the public attributes-package type and the
  generator's internal test-harness copy are equivalent (TypedId pattern).
- TWE008 is Error (generation contract), not Warning like ingress TWA0019:
  a missing FastEndpoint allow-list ships zero endpoints.

### Test outcomes

- `cd tests/analyzers/timewarp-architecture-sourcegenerator-tests && dotnet test -c Release` — **83 passed**, 0 failed.
- `web-server` Release build contains `CreateRoleEndpoint` / `GetAgentIdentityEndpoint`, not `GetWeatherForecastsEndpoint`.
- `api-server` Release build contains `GetWeatherForecastsEndpoint`, not `CreateRoleEndpoint`.
- `web-contracts.dll` / `api-contracts.dll` contain `ApiEndpointsEmbedded`.

### How to validate

**Automated**

```bash
cd tests/analyzers/timewarp-architecture-sourcegenerator-tests && dotnet test -c Release
# expect: all passed (83), including FastEndpointSourceGenerator_ContractAssemblies_Tests
#   (TWE008 empty/typo/unmarked; allow-list; marker stamp)
```

**Smoke**

```bash
dotnet build source/container-apps/web/projects/web-server/web-server.csproj -c Release
dotnet build source/container-apps/api/projects/api-server/api-server.csproj -c Release
# expect: 0/0

python3 -c "from pathlib import Path
print('marker web', 'ApiEndpointsEmbedded' in Path('source/container-apps/web/projects/web-contracts/bin/Release/net10.0/web-contracts.dll').read_bytes().decode('latin1','ignore'))
print('marker api', 'ApiEndpointsEmbedded' in Path('source/container-apps/api/projects/api-contracts/bin/Release/net10.0/api-contracts.dll').read_bytes().decode('latin1','ignore'))
web=Path('source/container-apps/web/projects/web-server/bin/Release/net10.0/web-server.dll').read_bytes().decode('latin1','ignore')
api=Path('source/container-apps/api/projects/api-server/bin/Release/net10.0/api-server.dll').read_bytes().decode('latin1','ignore')
print('web CreateRole', 'CreateRoleEndpoint' in web)
print('web no weather', 'GetWeatherForecastsEndpoint' not in web)
print('api weather', 'GetWeatherForecastsEndpoint' in api)
"
# expect: marker web/api True; web CreateRole True; web no weather True; api weather True
```

**Expect**

- Docs example is `<ApiEndpointContractAssemblies>web-contracts</ApiEndpointContractAssemblies>`
  (`documentation/developer/reference/api-endpoint-source-generator.md`).
- Empty `ApiEndpointContractAssemblies` with `EnableApiEndpointGeneration=true` is TWE008, no `*Endpoint.g.cs`.

**Not in scope:** mixin SyntaxProvider, route parser, fail-closed auth (task 110).

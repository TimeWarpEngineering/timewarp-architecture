# Adopt shared TimeWarp.Contracts: neutral operation contracts for HTTP, CLI and MCP

## Description

Define a thin, runtime-free package (working name `TimeWarp.Contracts`) that holds the transport-neutral part of an operation contract, and make this template's contracts and generators consume it. The same package is consumed by TimeWarp.Nuru (CLI and MCP bindings, Nuru task 508) so that one contract class, written in this template's style, is at once an HTTP endpoint, a CLI route and an MCP tool, with no second schema.

What is neutral today is already here in spirit: the outer `public static partial class Operation` with nested `Query`/`Command`, `Response`, `Validator`; `IRequest<OneOf<Response, SharedProblemDetails>>` on TimeWarp.Mediator; `I*Details` interfaces with composed validators; `SharedProblemDetails` kept free of ASP.NET Core so the WASM client can share it. What is missing is a transport-neutral operation identity and member metadata, a catalog, and a home that neither Nuru nor this template owns.

## Package contents

- `[Operation(string name, Description = ...)]` on the request class: canonical name and description. Kind is not an attribute; it comes from the mediator interface (`IQuery<T>`, `ICommand<T>`, `IIdempotentCommand<T>` from `TimeWarp.Mediator.Contracts`).
- Member metadata is derived, not declared: name from the property, required from nullability and `required`, default from the initializer, allowed values from enums, description from `System.ComponentModel.DescriptionAttribute`. The package documents these rules; generators in each consumer implement them.
- `SharedProblemDetails` moves here from `TimeWarp.Foundation.Contracts` (`source/foundation/foundation-contracts/types/shared-problem-details.cs`), with a type-forward left behind so nothing breaks.
- Catalog DTOs: operation, kind, members (name, type, required, default, allowed values, description), return type, and a bindings section (HTTP route and verb, CLI pattern, MCP tool name). Generalizes Nuru's `CapabilitiesResponse`; `--capabilities`, OpenAPI and MCP `tools/list` are projections.
- `IResponseRenderer<TResponse>`: optional text rendering a CLI or MCP host consults before JSON. HTTP ignores it.
- Dependencies: `TimeWarp.Mediator.Contracts`, `OneOf`, nothing else. AOT-compatible. No FluentValidation, no HTTP, no guard clauses (those stay in `TimeWarp.Foundation.Contracts`, which gains a dependency on this package).

## Template changes

- The contracts generator (`source/foundation/foundation-contracts-generators/contracts-generator.cs`) keeps emitting `ApiRouteAttribute`, `AuthApiRequestAttribute` and `OpenDataQueryParametersAttribute` into `TimeWarp.Foundation.Features` as the HTTP binding; it additionally reads `[Operation]` and, where a contract carries one, includes it in the generated endpoint metadata (`source/analyzers/timewarp-architecture-analyzers/models/endpoint-metadata.cs`).
- The FastEndpoint generator and analyzers (TWA0013, TWA0014, TWA0024 and friends) are unaffected by `[Operation]`; an analyzer suggests `[Operation]` on a hosted contract without one (info level) and a later task can make it required.
- Contracts adopt `IQuery<...>` / `ICommand<...>` in place of bare `IRequest<...>` where the kind is clear (mechanical; behavior unchanged). Decide whether `IIdempotentCommand` maps to PUT/DELETE by convention.
- `tw-web-api-contracts` skill and `documentation/` describe the neutral layer and the three bindings; the roles slice (`web/features/admin/roles/`) gets `[Operation]` as the living example.

## Home for the package

Decision to record here before scaffolding. Options: (a) a new repo `timewarp-contracts`, cleanest since Nuru, this template and Roslynk are all consumers and none should depend on another; (b) `source/foundation/` in this repo, publishing `TimeWarp.Contracts` as its own PackageId, fastest to start, creates a Nuru dependency on a package built from this repo; (c) beside `TimeWarp.Mediator.Contracts` in timewarp-mediator, since it is the layer directly above it. Recommendation: start as (b) to get a version Nuru 508-001 can build against, extract to (a) before Nuru's first release that references it.

## Checklist

- [ ] Home decision recorded
- [ ] Package scaffold: `[Operation]`, `SharedProblemDetails` (moved, type-forwarded), catalog DTOs, `IResponseRenderer`
- [ ] Derivation rules document
- [ ] Contracts generator reads `[Operation]`; endpoint metadata carries it
- [ ] Info analyzer for hosted contracts missing `[Operation]`
- [ ] Roles slice annotated as the living example; skill and docs updated
- [ ] First package version published for Nuru 508-001

## Notes

- Nuru side: timewarp-nuru 508 (parent), 508-001 (consume the package, derive endpoints, handler placement and mediator dispatch), 508-002/003 (public catalog and binder), 508-004 (MCP binding `TimeWarp.Nuru.McpHost`), 508-005 (daemon, backlog).
- Driving external consumer: Roslynk, which wants one contract per operation, a daemon, a Nuru CLI forwarding to it and an MCP server derived from the same contracts.
- Conflicts the neutral model resolves on the Nuru side (handler placement, mediator dispatch, rendering) are Nuru's; this template already has them the right way round.

## Session

- Created: 1624988 (2026-10-17)

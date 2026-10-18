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

## Versioning

`TimeWarp.Contracts` ships as **1.0.0-beta.1** and stays prerelease until proven. Proven means: Roslynk consumes it for both its CLI and MCP surfaces, the template's roles slice consumes it, and neither needed a breaking change to the package. Then promote to 1.0.0 (step 11 of the serial order recorded on timewarp-nuru 508). Breaking changes during beta bump the beta number, not the major.

## Implementation order (serial, decided 2026-10-17)

One item at a time across the three repos:

1. timewarp-nuru 506 spike (decision data for 507 and 508-005)
2. timewarp-architecture 384: `TimeWarp.Contracts` 1.0.0-beta.1 published
3. Roslynk: typed request/response/outcome refactor, no surface change
4. timewarp-nuru 508-002 public catalog
5. timewarp-nuru 508-003 public binder and invoke
6. timewarp-nuru 508-001 consume the package, derive endpoints, handler placement, mediator dispatch
7. Roslynk: CLI written directly with `[Operation]`, forwarding handlers, daemon over the existing loopback `tools/call`; CLI-first skill
8. timewarp-nuru 508-004 McpHost
9. Roslynk: host McpHost in the daemon, delete reflection registration and the `multi_query` binder
10. timewarp-nuru 507 gate: archive or narrow
11. timewarp-architecture: roles slice, info analyzer, skill and docs; promote `TimeWarp.Contracts` to 1.0.0
12. timewarp-nuru 508-005 decided from step 1 timing

## Scope of this task

This task is step 2 only. The info analyzer, `IQuery`/`ICommand` adoption, roles slice, skill
update and the 1.0.0 promotion are step 11 and live in child **384-001** (backlog). Do not do
them here.

## Goal (implementer restatement)

Ship a runtime-free `TimeWarp.Contracts` package at `1.0.0-beta.1` that holds the
transport-neutral operation surface, and make this template's contract pipeline consume it.
The package defines `[Operation]` (canonical name and description; kind stays on the mediator
interface), catalog DTOs (operation, kind, members, return type, HTTP/CLI/MCP bindings),
`IResponseRenderer<TResponse>`, and `SharedProblemDetails` moved out of
`TimeWarp.Foundation.Contracts` with a type forward so existing references keep compiling.
Member metadata is documented for generators to derive (name, nullability/`required`,
initializer, enum constants, `DescriptionAttribute`); this package does not reflect over
requests. The contracts generator copies `[Operation]` onto the generated HTTP partial, and
the FastEndpoint emit model stores the name and description. Endpoint class emission stays
the same. The info analyzer, `IQuery`/`ICommand` adoption, the roles example, skill and docs,
and the 1.0.0 promotion stay on child 384-001. Publishing the nupkg is the human `tw-release`
gate after merge.

## Home decision

Option (b). The package lives at `source/foundation/timewarp-contracts` and publishes as
`TimeWarp.Contracts` on its own `1.0.0-beta` line, separate from the repo version in
`source/Directory.Build.props`. Nuru 508-001 can reference that package. A standalone
`timewarp-contracts` repository (option a) is the later home, before a stable Nuru release
takes the dependency. Colocating it with `TimeWarp.Mediator.Contracts` (option c) is
rejected: this layer is the shared contract for HTTP, CLI, and MCP, not a mediator primitive.

## Checklist

- [x] Home decision recorded (recommendation: (b) now, extract to (a) later)
- [x] Package scaffold: `[Operation]`, `SharedProblemDetails` (moved, type-forwarded), catalog DTOs, `IResponseRenderer`
- [x] Derivation rules documented (package Design region / README, not `documentation/`)
- [x] Contracts generator reads `[Operation]`; endpoint metadata carries it
- [x] Package versioned 1.0.0-beta.1 and packable; `dev build` clean
- [ ] 1.0.0-beta.1 released for Nuru 508-001 (release cut is a human gate after merge, `tw-release`)

## Notes

- Nuru side: timewarp-nuru 508 (parent), 508-001 (consume the package, derive endpoints, handler placement and mediator dispatch), 508-002/003 (public catalog and binder), 508-004 (MCP binding `TimeWarp.Nuru.McpHost`), 508-005 (daemon, backlog).
- Driving external consumer: Roslynk, which wants one contract per operation, a daemon, a Nuru CLI forwarding to it and an MCP server derived from the same contracts.
- Conflicts the neutral model resolves on the Nuru side (handler placement, mediator dispatch, rendering) are Nuru's; this template already has them the right way round.

## Session

- Created: 1624988 (2026-10-17)
- Versioning and serial order recorded: 1479989 (2026-10-17)
- Step-11 items split to child 384-001: 2022499 (2026-10-18)
- Package, generator, and pack wiring implemented (2026-10-18)
- Implement oracle: implemented by implementer-grok (grok, model grok-4.7), session not reported, max-turns 500, Done — 2026-10-18T01:45:33Z
- Review oracle round 1: general reviewer (claude-opus-5-5, headless, session id not exposed), effort 3, turn cap 200. Findings M1 (bug) and M2 (suggestion) open, fix pass requested — 2026-10-18

## Results

`TimeWarp.Contracts` 1.0.0-beta.1 packs from `source/foundation/timewarp-contracts`.
`SharedProblemDetails` now lives in that assembly, still in `TimeWarp.Foundation.Types`.
`TimeWarp.Foundation.Contracts` type-forwards it. The contracts generator writes
`OperationName` and, when the description is non-empty, `OperationDescription` on the
HTTP partial. `EndpointEmitModel` stores both values. FastEndpoint class text leaves
them out. Member derivation rules are in the package README. The info analyzer, roles
slice, `IQuery`/`ICommand` adoption, skill, docs, and the 1.0.0 promotion stay on 384-001.

`dev release` packs this project without `-p:Version`, so the nupkg stays `1.0.0-beta.1`
while the rest of the repo stays on `source/Directory.Build.props` (`2.0.0-beta.20`).
Template smoke passes `-p:Version=2.0.0-smoke`, which overrides the project version for
the smoke feed. `dev check-version` reports that shared props version for every packable
id, including `TimeWarp.Contracts`. It exited 0 because `2.0.0-beta.20` is one prerelease
ahead of nuget `2.0.0-beta.19`. After the other packages publish as `2.0.0-beta.20`, a
later check can report `TimeWarp.Contracts` unpublished at that version. The package that
ships is `1.0.0-beta.1`.

NuGet publish of `1.0.0-beta.1` is the human `tw-release` gate after merge. That checklist
item stays open.

### How to validate

Smoke:

```bash
dev build
cd tests/analyzers/timewarp-architecture-sourcegenerator-tests && dotnet test -c Release --no-build --no-restore
cd tests/foundation/foundation-contracts-tests && dotnet test -c Release --no-build --no-restore
dotnet pack source/foundation/timewarp-contracts/timewarp-contracts.csproj -c Release --no-build
dotnet pack source/foundation/foundation-contracts/foundation-contracts.csproj -c Release --no-build
```

Expect:

- `dev build` ends with 0 warnings and 0 errors. This run did (Release, 20.66s).
- Source generator tests: 129 passed. Foundation contracts tests: 25 passed. Covered here: operation constants emitted once across `ApiRoute` and `AuthApiRequest`, escaped name and description literals, a foreign `OperationAttribute` ignored, FastEndpoint text unchanged when `[Operation]` is present, endpoint metadata carrying the name and description, and `SharedProblemDetails` resolving from the `timewarp-contracts` assembly.
- `TimeWarp.Contracts.1.0.0-beta.1.nupkg` depends on `OneOf` 3.0.271 and `TimeWarp.Mediator.Contracts` 14.0.0-beta.5.
- `TimeWarp.Foundation.Contracts.2.0.0-beta.20.nupkg` depends on `TimeWarp.Contracts` 1.0.0-beta.1.
- `ganda repo audit`: 32 passed, 0 failed.

### Review

Round 1, effort 3, roster general. The reviewer re-ran `dev build` (0 warnings, 0 errors),
both test projects (129 and 25 passed), and `ganda repo audit` (pass); all matched the
claims above. Final counts so far: bug 1 open, suggestion 1 open, nit 0. Disposition is
pending; a fix pass is needed for M1 and M2.

- M1 (bug): the separate `TimeWarp.Contracts` version has no gate. Release pushes with
  `--skip-duplicate`, and `dev check-version` checks the package at the repo version.
- M2 (suggestion): the two `[Operation]` readers disagree on blank values, and no test
  ties the attribute FQN to the real type.

Paths: `review/review-framework.md`, `review/round-1/general.md`, `review/round-1/merged.md`.

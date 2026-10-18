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

`TimeWarp.Contracts` versions with this repository (`source/Directory.Build.props`), like every other foundation package, so the existing `dev check-version` covers it and no package-specific gate is needed. First release is **2.0.0-beta.20**. It stays prerelease with the repo until proven. Proven means: Roslynk consumes it for both its CLI and MCP surfaces, the template's roles slice consumes it, and neither needed a breaking change to the package. An independent version line is for when it moves to its own repository (option a).

Decided 2026-10-18 (Steven) in review round 1 M1: use the repo version instead of the original 1.0.0-beta.1 line plus a new version check.

## Implementation order (serial, decided 2026-10-17)

One item at a time across the three repos:

1. timewarp-nuru 506 spike (decision data for 507 and 508-005)
2. timewarp-architecture 384: `TimeWarp.Contracts` 2.0.0-beta.20 published (originally 1.0.0-beta.1; see Versioning)
3. Roslynk: typed request/response/outcome refactor, no surface change
4. timewarp-nuru 508-002 public catalog
5. timewarp-nuru 508-003 public binder and invoke
6. timewarp-nuru 508-001 consume the package, derive endpoints, handler placement, mediator dispatch
7. Roslynk: CLI written directly with `[Operation]`, forwarding handlers, daemon over the existing loopback `tools/call`; CLI-first skill
8. timewarp-nuru 508-004 McpHost
9. Roslynk: host McpHost in the daemon, delete reflection registration and the `multi_query` binder
10. timewarp-nuru 507 gate: archive or narrow
11. timewarp-architecture: roles slice, info analyzer, skill and docs (384-001)
12. timewarp-nuru 508-005 decided from step 1 timing

## Scope of this task

This task is step 2 only. The info analyzer, `IQuery`/`ICommand` adoption, roles slice, skill
update are step 11 and live in child **384-001** (backlog). Do not do
them here.

## Goal (implementer restatement)

Ship a runtime-free `TimeWarp.Contracts` package at the repo version that holds the
transport-neutral operation surface, and make this template's contract pipeline consume it.
The package defines `[Operation]` (canonical name and description; kind stays on the mediator
interface), catalog DTOs (operation, kind, members, return type, HTTP/CLI/MCP bindings),
`IResponseRenderer<TResponse>`, and `SharedProblemDetails` moved out of
`TimeWarp.Foundation.Contracts` with a type forward so existing references keep compiling.
Member metadata is documented for generators to derive (name, nullability/`required`,
initializer, enum constants, `DescriptionAttribute`); this package does not reflect over
requests. The contracts generator copies `[Operation]` onto the generated HTTP partial, and
the FastEndpoint emit model stores the name and description. Endpoint class emission stays
the same. The info analyzer, `IQuery`/`ICommand` adoption, the roles example, skill and docs
stay on child 384-001. Publishing the nupkg is the human `tw-release`
gate after merge.

Review round 1: M1 is fixed by removing the package's own `<Version>`, so it takes the repo
version and `dev check-version` covers it (human decision; no new check). M2: both
`[Operation]` readers treat a whitespace name as no operation and a whitespace description as
absent, and the generator and endpoint-metadata tests compile against the real
`TimeWarp.Contracts.OperationAttribute`. Generated apps restore `TimeWarp.Contracts` as a
dependency of `TimeWarp.Foundation.Contracts`. The smoke-local feed names that package id
explicitly (`TimeWarp.Foundation.*` does not match it), and publish-smoke waits for it on
nuget.org before restore.

## Home decision

Option (b). The package lives at `source/foundation/timewarp-contracts` and publishes as
`TimeWarp.Contracts` at the repo version from `source/Directory.Build.props`. Nuru 508-001 can reference that package. A standalone
`timewarp-contracts` repository (option a) is the later home, before a stable Nuru release
takes the dependency. Colocating it with `TimeWarp.Mediator.Contracts` (option c) is
rejected: this layer is the shared contract for HTTP, CLI, and MCP, not a mediator primitive.

## Checklist

- [x] Home decision recorded (recommendation: (b) now, extract to (a) later)
- [x] Package scaffold: `[Operation]`, `SharedProblemDetails` (moved, type-forwarded), catalog DTOs, `IResponseRenderer`
- [x] Derivation rules documented (package Design region / README, not `documentation/`)
- [x] Contracts generator reads `[Operation]`; endpoint metadata carries it
- [x] Package versioned with the repo (2.0.0-beta.20) and packable; `dev build` clean
- [x] Review M1: repo version instead of an own line; `dev check-version` covers it
- [x] Review M2: operation readers share the blank-value rule; tests use the real attribute
- [ ] 2.0.0-beta.20 released for Nuru 508-001 (release cut is a human gate after merge, `tw-release`)

## Review

- Round 1: Blocked — M1, M2; both fixed in fix pass 1

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
- Review oracle: review by reviewer-claude (claude, model claude-opus-5-5), session not reported, max-turns 200 — 2026-10-18T01:53:33Z
- Fix pass 1: Grok started an M1 version-check command against the human decision; stopped, check discarded, M1 and M2 finished by Claude (claude-opus-5-5) in the cockpit at Steven's request — 2026-10-18
- Fix pass 2: implement-fix-2 (grok, model grok-4.7). M1 and M2 were already in the tree. The smoke-local feed and publish-smoke wait list now name `TimeWarp.Contracts`, because `TimeWarp.Foundation.*` does not match that id — 2026-10-18

## Results

`TimeWarp.Contracts` packs from `source/foundation/timewarp-contracts` at the repo version.
`SharedProblemDetails` now lives in that assembly, still in `TimeWarp.Foundation.Types`.
`TimeWarp.Foundation.Contracts` type-forwards it and depends on `TimeWarp.Contracts` at the
same version. The contracts generator writes `OperationName` and, when the description is
not whitespace, `OperationDescription` on the HTTP partial. `EndpointEmitModel` stores both
values. FastEndpoint class text leaves them out. Member derivation rules are in the package
README. The info analyzer, roles slice, `IQuery`/`ICommand` adoption, skill and docs stay
on 384-001.

Generated apps restore `TimeWarp.Contracts` because `TimeWarp.Foundation.Contracts` depends
on it. Template smoke maps that id to the local feed (`TimeWarp.Foundation.*` does not match
it). Publish-smoke waits for it on nuget.org before restore. `dev check-version` lists
`TimeWarp.Contracts` with the other packages at `2.0.0-beta.20` (nuget latest
`2.0.0-beta.19`) and passes. NuGet publish is the human `tw-release` gate after merge. That
checklist item stays open.

### How to validate

Smoke:

```bash
dev build
cd tests/analyzers/timewarp-architecture-sourcegenerator-tests && dotnet test -c Release --no-build --no-restore
cd tests/foundation/foundation-contracts-tests && dotnet test -c Release --no-build --no-restore
dotnet pack source/foundation/timewarp-contracts/timewarp-contracts.csproj -c Release --no-build
dotnet pack source/foundation/foundation-contracts/foundation-contracts.csproj -c Release --no-build
dev check-version
dev template-smoke
ganda repo audit
```

`dotnet test` is run from the project directory. Passing the csproj path uses the VSTest target, which this SDK rejects.

Expect:

- `dev build` ends with 0 warnings and 0 errors. This run did (Release, 24.80s).
- Source generator tests: 135 passed. Foundation contracts tests: 26 passed. Covered here: operation constants emitted once across `ApiRoute` and `AuthApiRequest`, escaped name and description literals, a whitespace name or description omitted, a foreign `OperationAttribute` ignored, FastEndpoint text unchanged when `[Operation]` is present, endpoint metadata carrying the name and description, and `SharedProblemDetails` resolving from the `timewarp-contracts` assembly.
- `TimeWarp.Contracts.2.0.0-beta.20.nupkg` depends on `OneOf` 3.0.271 and `TimeWarp.Mediator.Contracts` 14.0.0-beta.5.
- `TimeWarp.Foundation.Contracts.2.0.0-beta.20.nupkg` depends on `TimeWarp.Contracts` 2.0.0-beta.20.
- `dev check-version` exits 0 and lists `TimeWarp.Contracts` among the packages checked at `2.0.0-beta.20`.
- `dev template-smoke` ends with `Template smoke SUCCEEDED`. SmokeDefault built with 0 warnings and 0 errors and restored `TimeWarp.Contracts` `2.0.0-smoke` from the local feed.
- `ganda repo audit`: passes (32 checks).

### Review

Round 1, effort 3, roster general. Counts after fix pass 2: bug 0 open / 1 fixed, suggestion 0 open / 1 fixed, nit 0. Disposition waits for review round 2.

- M1 (bug, fixed): the package takes the repo version. `dev check-version` covers it. No new version check.
- M2 (suggestion, fixed): both `[Operation]` readers use `IsNullOrWhiteSpace`, and the tests compile against the real attribute.

Paths: `review/review-framework.md`, `review/round-1/general.md`, `review/round-1/merged.md`.

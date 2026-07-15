# AGENTS.md

Guidance for all coding agents in this repository. CLAUDE.md includes this file (`@AGENTS.md`);
other tools read it directly.

## What this repo is

- **This repo IS the `dotnet new timewarp-architecture` template.** Root `source/` + `tests/` are
  the template content (defined by root `.template.config/`); `timewarp-templates/` is the NuGet
  packaging + docfx tree. Changes here ship to every generated app.
- Feature flags (`api`, `grpc`, `web`, `yarp`, `postgres`) are template preprocessor switches —
  keep `<!--#if (flag)-->` / `#if flag` regions intact when editing near them. Demo features
  (counter, event-stream) ship unconditionally; see HowToRemoveDemoFeatures.md.

## Build / run / test

Run from the repo root (the `dev` CLI resolves the root via git):

- `dev run` — Aspire orchestrator (Development)
- `dev build` — full solution; **warnings are errors, 0/0 is the only acceptable result**
- `dev test` — every project under `tests/` (globbed, run one at a time — fixed ports)
- `dotnet fixie tests/<project> [--tests Class[.Method]]` — one project/class/method
- More commands: `dev --capabilities` (see the `dev-cli` skill)

## Stack

- **.NET 10**, C# latest, `Nullable` enabled repo-wide, central package management
- Blazor WebAssembly + **TimeWarp.State**; **TimeWarp.Mediator** (NOT MediatR):
  `IRequest<OneOf<Response, SharedProblemDetails>>`
- Server endpoints: web-server = hand-written MVC `BaseEndpoint<TRequest, TResponse>` shims;
  api-server = FastEndpoints **generated from contracts**
- Tests: **Fixie + Shouldly** (NOT MSTest/xUnit; do not introduce FluentAssertions — v8+ is
  commercially licensed)
- Blazor form validation: **Blazilla** (explicit validator instance — supports `I*Details` binding)
- **FluentUI v5 + plain CSS** design tokens (`wwwroot/css/tokens.css`); no Tailwind — do not
  reintroduce it (see `blazor-css-strategy` skill)
- .NET Aspire orchestration; EF Core (postgres behind its flag)

## Layout (kebab-case paths everywhere; namespaces PascalCase)

```
source/
  foundation/        # shared contracts/application/domain/server layers -> TimeWarp.Foundation.* packages
  analyzers/         # Roslyn analyzers + source generators -> TimeWarp.Architecture.{Analyzers,Generators,Attributes}
  container-apps/
    web/             # web-spa (WASM), web-contracts, web-application, web-server, ...
    api/  grpc/  aspire/  yarp/
tests/               # mirrors source/; includes web-contracts-tests (host-free serialization round-trips)
```

## Platform packages (foundation + analyzers)

Greenfield `dotnet new timewarp-architecture` apps reference **published NuGet packages** for the
shared platform (template symbols `foundationPackages` / `analyzerPackages`, both default **true**).
This monorepo keeps the source and dogfoods it via `ProjectReference`.

| PackageId | Contents |
|-----------|----------|
| `TimeWarp.Foundation.*` / `TimeWarp.Modules` | Runtime foundation layers (task 051) |
| `TimeWarp.Architecture.Analyzers` | Convention DiagnosticAnalyzers only (TWPA0002–0010) — safe repo-wide |
| `TimeWarp.Architecture.Generators` | Source generators + TWPA0001 — attach only where gens should run |
| `TimeWarp.Architecture.Attributes` | Runtime attributes (e.g. `[ApiEndpoint]`) — public library |

MSBuild dual-mode (auto-detects missing source trees): `UseFoundationPackages` /
`UseAnalyzerPackages`. CPM `PackageVersion` pins lag the last **published** version (may trail
`source/Directory.Build.props` `<Version>`). Upgrade path for apps that still vendored
`source/analyzers/**`: see `documentation/developer/how-to-guides/HowToUpgradeToAnalyzerPackages.md`.

## Key patterns

- **Endpoint-centric contracts**: `public static partial class Operation` with nested
  `Query`/`Command`, `Response`, `Validator`; `[ApiRoute("api/…", HttpVerb.X)]` (+
  `[AuthApiRequest]`, `[OpenDataQueryParameters]`) source-generate route members onto the partial.
  Full spec: **`web-api-contracts` skill** — invoke it before touching contracts.
- **Prefer analyzers/source generators over convention-by-memory**: when two things must agree,
  generate one from the other or add a build-time check. Existing generators: contract attributes,
  FastEndpoints, `[Page]`, `[StateAccess]`, the SPA mock-factory registry.
- **AssemblyMarker**: every assembly declares one.
- Serializer options for the contract seam come from `ContractSerializationDefaults` — never
  declare seam options inline.

## Enforcement — conventions are compiler-checked (build-breaking)

| ID | Rule |
|----|------|
| TWPA0001 | partial-class primary/secondary file declaration shape |
| TWPA0002/0003 | contract property nullability must agree with FluentValidation presence rules |
| TWPA0004 | every source file carries `#region Purpose` (one honest line minimum) |
| TWPA0005/0006 | endpoint verb matches the contract's `[ApiRoute]`; every routed contract has an endpoint or `[ClientOnlyContract(reason)]` |
| TWPA0007 | Aspire `AddProject` resource names are `ServiceNames` constant values |
| TWPA0008 | no template-conditional tokens in comments/strings (the dotnet-new engine misreads them and truncates generated files); escape hatch: the `cnd:noEmit` comment-marker pair |
| TWPA0009 | product slices (`…Features.<Id>` under SliceRoot) must not reference other product slices (share via Components/contracts); platform `Applications` is one-way free; opt-out: `[CrossSliceReference(typeof(T), reason)]` |
| TWPA0010 | a directive naming a template.json flag requires that flag in DefineConstants (else the region silently vanishes from the repo build) |

## Agent Context Regions — maintenance rule

Every source file carries a `#region Purpose` block (enforced by TWPA0004); files with design
decisions also carry `#region Design`, and optionally `#region Open Questions`. These are part of
the code, not decoration:

- **When you edit a file that has regions, reconcile them with your change before finishing.**
  A Design region describing the old approach is a bug you just introduced.
- **When you create a source file, add `#region Purpose`** (one honest line minimum) at the top,
  before the namespace — plus `Design` where there are genuine decisions to record.
- **When you read an unanswered question in `#region Open Questions` that you can answer,**
  answer it (or implement the answer and remove the pair).

Formats and lifecycle: the `agent-context-regions` skill.

## Definition of Done

- **API endpoint**: contract per the skill (Request/Response/Validator, shared `I*Details` where
  bindable) + server Endpoint + Handler + integration tests (happy path AND validation rejection).
  Backend validation comes from the mediator's `FluentValidationBehavior` — do not re-validate in
  handlers.
- **Client feature**: State/Actions/Components + serialization round-trips in `web-contracts-tests`
  for non-trivial shapes (ctor+Guard, envelopes, generated route properties).

## Task management

`ganda kanban` over the `kanban/` tree (`backlog/`, `to-do/`, `in-progress/`, `done/`):
**always `ganda kanban create "title"`** (it assigns the number — never hand-number), then
`move`/`done` to transition. Do not create perpetual/never-closing tasks. See the `kanban` skill.

## Documentation

`documentation/` (developer + conceptual guides, ADRs) ·
https://timewarpengineering.github.io/timewarp-architecture/

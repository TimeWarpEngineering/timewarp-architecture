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
- Server endpoints: **both** web-server and api-server host **FastEndpoints generated from
  contracts** (`[ApiEndpoint]` + `[ApiRoute]`; every hosted contract carries exactly one of
  `[EndpointAuthorize]` (policies) or `[EndpointAllowAnonymous(reason)]` — the generator is
  fail-closed, so a contract with neither marker emits no auth config at all rather than defaulting
  to anonymous; TWA0013/TWA0014 enforce the pairing at build time). No hand-written `BaseEndpoint`
  shims in the template. Validation stays on the mediator's `FluentValidationBehavior` — do not
  adopt FastEndpoints' validator integration.
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
    web/
      features/      # product slices (feature-cohesive): all layers together under <slice>/
                     # files named <name>[-<function>]-<layer>.cs; layer projects glob by suffix
      web-spa/       # WASM UI (features stay conventional under web-spa/features — not rehomed)
      web-contracts/ web-application/ web-server/ web-domain/ web-infrastructure/
      msbuild/       # feature-filename-grammar.g.props + feature-membership.targets
    api/  grpc/  aspire/  yarp/
tests/               # mirrors source/; includes web-contracts-tests (host-free serialization round-trips)
```

**Axis-1 filename grammar (web product code):** files under `web/features/` use
`<name>[-<function>]-<layer>.cs` (`handler`→application, `endpoint`→server,
`feature-annotations`→server; contracts drop the function segment:
`create-role-contracts.cs`). Escape hatch: `<name>-<layer>.cs` with no function
(`role-store-application.cs`). Registry SSOT:
`source/analyzers/timewarp-architecture-convention-analyzers/feature-filename-grammar.json`
(generates analyzer constants + `web/msbuild/feature-filename-grammar.g.props`). **Registry edit
⇒ full rebuild** (analyzer DLLs can go stale under pure incremental builds). Namespaces do **not**
track folders — TWA0009 still keys off `…Features.<Id>`. Full workflow (worked examples,
registry extension, TWA0015/0016 fixes, membership-guard errors, SPA exception, per-module
assembly-split note): **`feature-placement` skill** (`skills/tw-feature-placement/SKILL.md`).

## Platform packages (foundation + analyzers + identity)

Greenfield `dotnet new timewarp-architecture` apps reference **published NuGet packages** for
foundation and analyzers (template symbols `foundationPackages` / `analyzerPackages`, both default
**true**). **Identity** defaults the other way: `identityPackages` default **false** ships
`source/libraries/timewarp-identity` into generated apps until `TimeWarp.Identity` is published on
nuget.org. This monorepo dogfoods all three via `ProjectReference` when source trees are present.

| PackageId | Contents |
|-----------|----------|
| `TimeWarp.Foundation.*` / `TimeWarp.Modules` | Runtime foundation layers (task 051) |
| `TimeWarp.Architecture.Analyzers` | Convention DiagnosticAnalyzers only (TWA0002–0016) — safe repo-wide |
| `TimeWarp.Architecture.Generators` | Source generators + TWA0001, TWA0017/0018 (ingress route generation) — attach only where gens should run |
| `TimeWarp.Architecture.Attributes` | Runtime attributes (e.g. `[ApiEndpoint]`) — public library |
| `TimeWarp.Identity` | Principal identity (passkeys / agent keys); dual-mode until first publish |

MSBuild dual-mode (auto-detects missing source trees): `UseFoundationPackages` /
`UseAnalyzerPackages` / `UseIdentityPackages`. CPM `PackageVersion` pins lag the last
**published** version (may trail `source/Directory.Build.props` `<Version>`). Upgrade path for
apps that still vendored `source/analyzers/**`: see
`documentation/developer/how-to-guides/HowToUpgradeToAnalyzerPackages.md`.

**sourceName-safe platform package IDs:** template `sourceName` is `TimeWarp.Architecture`, so a
literal `TimeWarp.Architecture.Analyzers` in csproj/CPM would rewrite to `AppName.Analyzers` on
generate. IDs and the Attributes namespace are composed in
`msbuild/timewarp-platform-packages.props` (`$(_TwPlatformVendor).Architecture.*` → properties
like `$(TwArchitectureAnalyzersPackageId)`). Import that props from both root
`Directory.Build.props` and `Directory.Packages.props` (CPM does not inherit DBP). Contracts use
dual-mode MSBuild `<Using>` for the Attributes namespace (package → platform namespace property;
source → `$(RootNamespace).Attributes`). Regression gate: `dev template-smoke` (also
`.github/workflows/template-smoke.yml`).

## Key patterns

- **Endpoint-centric contracts**: `public static partial class Operation` with nested
  `Query`/`Command`, `Response`, `Validator`; `[ApiRoute("api/…", HttpVerb.X)]` (+
  `[AuthApiRequest]`, `[OpenDataQueryParameters]`) source-generate route members onto the partial.
  Hosted operations also carry `[ApiEndpoint]` (generation opt-in) plus exactly one of
  `[EndpointAuthorize(Policy=…)]` or `[EndpointAllowAnonymous(reason)]` so the FastEndpoint
  generator emits the HTTP shim's auth config (fail-closed: no marker means no auth config emitted,
  not anonymous). `IAuthApiRequest` is a client/mock-mode identity signal only — it never secures
  the server; `[EndpointAuthorize]` is the sole server-auth marker (TWA0014 enforces the pairing).
  Full spec: **`web-api-contracts` skill** — invoke it before touching contracts.
- **Prefer analyzers/source generators over convention-by-memory**: when two things must agree,
  generate one from the other or add a build-time check. Existing generators: contract attributes,
  FastEndpoints, `[Page]`, `[StateAccess]`, the SPA mock-factory registry.
- **AssemblyMarker**: every assembly declares one.
- Serializer options for the contract seam come from `ContractSerializationDefaults` — never
  declare seam options inline.

## Enforcement — conventions are compiler-checked (build-breaking)

Diagnostic IDs use the prefix **TWA** = **T**ime**W**arp **A**rchitecture (not the generic
`TimeWarp.SourceGenerators` package, which will use a different prefix).

| ID | Rule |
|----|------|
| TWA0001 | partial-class primary/secondary file declaration shape |
| TWA0002/0003 | contract property nullability must agree with FluentValidation presence rules |
| TWA0004 | every source file carries `#region Purpose` (one honest line minimum) |
| TWA0005/0006 | endpoint verb matches the contract's `[ApiRoute]`; every routed contract has an endpoint or `[ClientOnlyContract(reason)]` |
| TWA0007 | Aspire `AddProject` resource names are `ServiceNames` constant values |
| TWA0008 | no template-conditional tokens in comments/strings (the dotnet-new engine misreads them and truncates generated files); escape hatch: the `cnd:noEmit` comment-marker pair |
| TWA0009 | product slices (`…Features.<Id>` under SliceRoot) must not reference other product slices (share via Components/contracts); platform `Applications` is one-way free; opt-out: `[CrossSliceReference(typeof(T), reason)]` |
| TWA0010 | a directive naming a template.json flag requires that flag in DefineConstants (else the region silently vanishes from the repo build) |
| TWA0011/0012 | an `IAggregateRoot` must declare a nested `Invariants : AbstractValidator<T>`, and it must be `private` (kept out of `AddValidatorsFromAssemblyContaining`) |
| TWA0013 | an `[ApiEndpoint]` contract must carry `[EndpointAuthorize]` or `[EndpointAllowAnonymous(reason)]` — the generator is fail-closed and emits no auth config for neither |
| TWA0014 | an `[ApiEndpoint]` contract's auth posture must not be contradictory: not both markers, and not `[EndpointAllowAnonymous]` paired with a nested `Query`/`Command` that declares `IAuthApiRequest` |
| TWA0015 | feature filename: registered function segment pairs with the wrong layer (see feature-filename-grammar.json) |
| TWA0016 | feature filename: unregistered or mis-spelled function segment used as archetype (escape hatch `<name>-<layer>.cs` stays valid) |
| TWA0017 | a generated ingress web prefix (`WebServerApiRoutePrefixes`) shadows another server's route space — it equals/parents a hosted route in another contracts assembly, or collides with an `IngressReservedPathPrefixes` entry (grpc) |
| TWA0018 | a web-contracts route cannot be collapsed to a top-level ingress prefix (bare `api` or a parameterized second segment like `api/{id}`) |

**Slice isolation (TWA0009):** product code under SliceRoot must not reach other product
slices. Placement, platform `Applications`, sharing, and `[CrossSliceReference]` opt-out:
skill **`slice-isolation`** (`skills/tw-slice-isolation/SKILL.md`).

## Agent Context Regions — maintenance rule

Every source file carries a `#region Purpose` block (enforced by TWA0004); files with design
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
  bindable; `[ApiEndpoint]` when the host should generate the FastEndpoint; exactly one of
  `[EndpointAuthorize]` or `[EndpointAllowAnonymous(reason)]`, always) + Handler + integration tests
  (happy path AND validation rejection). Backend validation comes from the mediator's
  `FluentValidationBehavior` — do not re-validate in handlers.
- **Client feature**: State/Actions/Components + serialization round-trips in `web-contracts-tests`
  for non-trivial shapes (ctor+Guard, envelopes, generated route properties).

## Task management

`ganda kanban` over the `kanban/` tree (`backlog/`, `to-do/`, `in-progress/`, `done/`):
**always `ganda kanban create "title"`** (it assigns the number — never hand-number), then
`move`/`done` to transition. Do not create perpetual/never-closing tasks. See the `kanban` skill.

## Documentation

`documentation/` (developer + conceptual guides, ADRs) ·
https://timewarpengineering.github.io/timewarp-architecture/

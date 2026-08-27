# AGENTS.md

Guidance for all coding agents in this repository. CLAUDE.md includes this file (`@AGENTS.md`);
other tools read it directly.

## Agent communication — no calendar estimates

**Never give temporal estimates** (hours, days, weeks, sprints, quarters, “multi-quarter,”
“quick win this afternoon,” etc.). Calendar duration is meaningless for agent work: parallelism,
model choice, and available context/tokens are unknown to the estimator and change per session.

- Prefer **scope** (what / how many surfaces / which blockers) over **when**.
- Prefer **dependencies and proof gates** over “should take about…”
- Do **not** substitute “tokens” or “agent-hours” as a fake precision unit either — they are not
  stable or comparable across runs. If magnitude helps, use countable work (files, suites,
  decisions, blockers), not time or budget guesses.
- Kanban tasks must not carry Estimate fields (see **`tw-kanban`**).

## What this repo is

- **This repo IS the `dotnet new timewarp-architecture` template.** Root `source/` + `tests/` are
  the template content (defined by root `.template.config/`); `timewarp-templates/` is the NuGet
  packaging tree. Changes here ship to every generated app.
- Feature flags (`api`, `grpc`, `web`, `yarp`, `postgres`) are template preprocessor switches —
  keep `<!--#if (flag)-->` / `#if flag` regions intact when editing near them. Demo features
  (counter, event-stream) ship unconditionally; see how-to-remove-demo-features.md.

## Build / run / test

Run from the repo root (the `dev` CLI resolves the root via git):

- `dev run` — Aspire orchestrator (Development)
- `dev build` — full solution; **warnings are errors, 0/0 is the only acceptable result**
- `dev test` — every project under `tests/` (globbed, run one at a time — fixed ports); includes
  family `JARIBU_MULTI` aggregators that compile co-located `source/**/*-tests.cs` runfiles
- one suite: `cd tests/<project> && dotnet test -c Release` (MTP — the csproj-path form of
  `dotnet test` is unsupported on .NET 10). Selection: `-- --filter-class <substring>` /
  `-- --filter-method <substring>` / `-- --filter-tag <tag>` (also honors `JARIBU_FILTER_TAG`;
  CLI wins), or `--list-tests` + `-- --filter-uid <uid>` for a specific discovered node
  (`TimeWarp.Jaribu.TestingPlatform` ≥ 1.0.0-beta.15, timewarp-jaribu#23; see
  how-to-filter-tests-by-name.md / how-to-filter-tests-by-tags.md).
- `dotnet run source/<family>/features/…/<name>-tests.cs` — one co-located Jaribu runfile
  standalone (local dev loop; CI uses family aggregators via `dev test`)
- More commands: `dev --capabilities` (see the `dev-cli` skill)

## Before opening a PR

Use the **`tw-pr`** skill (`/tw-pr`) — do not open a PR until its gates pass.
Mandatory for this repo:

1. **`ganda repo audit`** (blocking). On failure, prefer `ganda repo audit --fix`
   (or `--fix --checks <id>`) then re-run audit and commit any fixes.
2. **`dev check-version`** when shipping packages/template — source version must
   be new vs the latest GitHub release tag; platform CPM pins equal `<Version>`
   (task 124).
3. **`dev build`** (0/0) for code changes; add tests / `dev template-smoke` when
   the change type warrants it (see skill for scope table).

Branch naming, commits, and merge policy: **`tw-git`**.

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
- Tests — **single-framework Jaribu** (zero Fixie and zero xUnit; epic 145 / decision task 143 §6;
  Fixie retired task **145-007**). Assertions: **Shouldly** only (do not introduce FluentAssertions
  — v8+ is commercially licensed). **Do not reintroduce Fixie or xUnit.**
  - **New product-slice tests** are co-located Jaribu runfiles (`<name>[-<function>]-tests.cs`
    under `features/` / `platform/`), standalone via `dotnet run`. Preamble and C-create host
    rules: skill **`tw-feature-placement`**; Jaribu itself: cross-repo **`tw-jaribu`**. Exemplars:
    `create-role-tests.cs` (web, host-free), `get-weather-forecasts-tests.cs` (api, SetupOnce).
  - **Host-level / topology** suites stay suite-shaped under `tests/` on **Jaribu MTP** (project-local
    `global.json` test.runner). Closed-box topology: `aspire-tests` (145-003). In-proc HostGraph:
    HostGraphFactory C-create (145-002).
  - **Fixture lifetime — C-create is the default; C-share is the exception (145-008):** every
    test class owns and disposes its own host graph (C-create, `HostGraphFactory` /
    `SessionHostFixture<TInner>` subclass's `CreateAsync`) unless the suite is genuinely
    **expensive AND multi-class closed-box** — then it may opt into a Jaribu session-scoped
    fixture (`TimeWarp.Jaribu` ≥ 1.0.0-beta.15: `RegisterSessionFixture<T>` +
    `SessionFixture.GetAsync<T>()`) via a `SessionHostFixture<TInner>`
    (`tests/common/timewarp-testing`) subclass that delegates to the SAME per-class factory —
    no duplicated boot logic. Exemplar: `web-spa-integration-tests`' `SpaSessionFixture`
    (~109s → ~20s wall for its 6 previously-booting classes). Full rules and the anti-pattern
    warning (never a process-static `Lazy`/bare static for sharing): skill
    `tw-feature-placement` (C-share host lifetime).
  - **CI:** family **`JARIBU_MULTI` aggregators** under
    `tests/container-apps/<family>/<family>-jaribu-tests/` (web + api; Microsoft.Testing.Platform;
    not in `.slnx` — task 136). Each aggregator's project-local `global.json` must **mirror the
    root SDK pin** on SDK bumps (timewarp-jaribu#20). Standalone `dotnet run <file>.cs` is the
    local dev loop.
  - Playwright e2e is unaffected.
- **Test host lanes (Aspire vs in-proc):** two lanes, no wholesale Aspire migration —
  - **In-proc** (`WebApplicationHost` / timewarp-testing, fixed ports web=7000 api=7255 yarp=8443):
    DI substitution, mediator/pipeline, BFF mocks — **only place fixed ports live**; `dev test`
    stays serialized for those projects. Auth: `MockAccessTokenProvider` DI override and real
    passkey-ceremony cookies remain first-class.
  - **Closed-box** (`Aspire.Hosting.Testing` / AppHost): topology, ingress, multi-resource, and
    process-isolation cases (e.g. FastEndpoints discovery pollution across AppDomain). No DI
    mock/substitution across the process wall; dynamic Aspire ports. Auth: Development/Testing +
    `Authentication:UseMock` enables fail-closed mock principal header
    (`X-TimeWarp-Mock-Principal-Id`) for authenticated ingress→web BFF coverage (task 145-009);
    Production never activates mock auth even when the flag is set.
- Blazor form validation: **Blazilla** (explicit validator instance — supports `I*Details` binding)
- **FluentUI v5 + plain CSS** design tokens (`wwwroot/css/tokens.css`); no Tailwind — do not
  reintroduce it (see `blazor-css-strategy` skill)
- .NET Aspire orchestration; EF Core (postgres behind its flag)

## Layout (kebab-case paths everywhere; namespaces PascalCase)

**File naming:** kebab-case for files and folders (map `user-service.cs` → `UserService`). Full
agent rules and exception table: **`tw-csharp`** (File and directory naming). Human SSOT sketch:
`documentation/developer/standards/file-naming.md`. Axis-1 product grammar
(`name[-function]-layer.cs`): **`tw-feature-placement`**.

**Do not kebab-force:** `.razor` / paired `.razor.cs` / `.razor.css` (Blazor type-matching names);
MSBuild well-known props/targets; ASP.NET `Properties/`, `launchSettings.json`,
`appsettings.<Environment>.json`; `_Imports.razor` / `App.razor` where the host requires them.

**`.cs` enforcement:** `TimeWarp.SourceGenerators` diagnostic **`TW0001`** (`TW*` package family —
not Architecture `TWA*`). Package is referenced repo-wide from root `Directory.Build.props`;
`.editorconfig` sets `dotnet_diagnostic.TW0001.severity = warning` (build-breaking via
TreatWarningsAsErrors). Requires SourceGenerators **≥ 1.0.0-beta.10** (multi-dot partials + skip
`obj/`/`bin/` generated trees). Non-`.cs` / folder basenames: **`ganda repo audit`**
**`kebab-path-names`** (Ganda task **188**, shipped).

```
source/
  foundation/        # shared contracts/application/domain/server layers -> TimeWarp.Foundation.* packages
  analyzers/         # Roslyn analyzers + source generators -> TimeWarp.Architecture.{Analyzers,Generators,Attributes}
  container-apps/
    web/
      features/      # product slices (feature-cohesive): all layers together under <slice>/
                     # files named <name>[-<function>]-<layer>.cs; layer projects glob by suffix
      platform/      # host/platform clusters (postgres, identity-host): same -layer suffix grammar
                     # as features/, NOT …Features.* namespaces (TWA0009 platform, not product)
      projects/      # artifact folders (csproj homes): web-contracts/ web-application/
                     # web-domain/ web-infrastructure/ web-server/ web-spa/
                     # (SPA features stay conventional under web-spa/features — not rehomed)
      msbuild/       # feature-filename-grammar.g.props + feature-membership.targets
    api/             # same axis-1 shape as web (features/ + platform/ + msbuild/);
                     # features/weather-forecast/ (demo slice); platform/ tree absent (no content yet)
      projects/      # api-contracts/ api-application/ api-domain/
                     # api-infrastructure/ api-server/
    grpc/            # same axis-1 shape as web (features/ + platform/ + msbuild/);
                     # features/hello/ superhero/ greeter/ (demo slices); platform/codegen/
      projects/      # grpc-contracts/ grpc-application/ grpc-domain/
                     # grpc-infrastructure/ grpc-server/ (protos/ stays out of grammar scope)
    aspire/projects/ # aspire-app-host/ aspire-service-defaults/
    yarp/            # single-project family (IS the artifact; left flat)
tests/               # mirrors source/; includes web-contracts-tests (host-free serialization round-trips)
```

**Where a file goes:** all logic lives in a concern folder under one of the two shared trees
above — `features/` for product concerns, `platform/` for platform concerns — named by the
filename grammar below; an artifact folder (`web-server/`, `web-infrastructure/`, …) holds only
its own definition (csproj, global-usings) and entry-point bootstrap (program.cs, appsettings,
host-config exemplars). Litmus test for the fuzzy middle: if the deployable were deleted, would
the file still mean something? Yes → a shared tree; no → bootstrap, stays with the artifact.

**Features substrate:** cross-slice compile-time constants (e.g. `ModuleIds`, `RoleIds`) may use
the bare `…Features` namespace (no slice Id) so product slices can share ids without TWA0009
cross-slice references. Document the choice in the file's Design region. Full litmus:
`skills/tw-feature-placement` (**Features substrate**).

**Axis-1 filename grammar (family-generic — web, api, grpc):** files under `<family>/features/`
and `<family>/platform/` use `<name>[-<function>]-<layer>.cs` (`handler`→application,
`endpoint`→server; contracts drop the function segment: `create-role-contracts.cs`). Escape
hatch: `<name>-<layer>.cs` with no function (`role-store-application.cs`,
`postgres-db-context-infrastructure.cs`). Registry SSOT (itself family-agnostic):
`source/analyzers/timewarp-architecture-convention-analyzers/feature-filename-grammar.json`
generates the analyzer constants once, plus a standalone `<family>/msbuild/feature-filename-grammar.g.props`
per family (web, api, grpc — yarp is a single-project family and is excluded). Each family's own
tree roots (`WebFeatureTreeRoot`/`WebPlatformTreeRoot`, `ApiFeatureTreeRoot`/`ApiPlatformTreeRoot`,
`GrpcFeatureTreeRoot`/`GrpcPlatformTreeRoot`) are globbed into that family's layer projects via
its own `<family>/msbuild/feature-membership.targets`, imported once via
`<family>/Directory.Build.targets`. **Registry edit ⇒ full rebuild** (analyzer DLLs can go stale
under pure incremental builds). Namespaces do **not** track folders — product slices use
`…Features.<Id>` (TWA0009 — namespace-based, already universal across families); platform
clusters keep non-Features namespaces. Full rule, litmus test, and decision table:
**`feature-placement` skill** (`skills/tw-feature-placement/SKILL.md`).

**Registered-unrouted layer (`tests`, task 135):** the JSON registry's `"unroutedLayers"` key
(currently `["tests"]`) registers a layer suffix that TWA0015/0016 and the membership guard
match and validate exactly like a routed layer, but that gets NO `Compile` glob in any family's
`feature-filename-grammar.g.props` — a `<name>[-<function>]-tests.cs` co-located Jaribu runfile
stays a first-class grammar citizen (misnamed/orphaned files still trip the teaching error, and
`-handler-tests.cs` still trips TWA0015) while compiling into no layer project. **Enforcement
surface:** TWA0015/0016 and the membership guard only fire when the file is compiled. The repo
`dev build` solution gate never touches unrouted `*-tests.cs` (no layer `Compile` glob; not in
`.slnx`). Coverage restored by (1) standalone `dotnet run` / `dotnet build` on the runfile,
(2) family `JARIBU_MULTI` aggregators under `tests/container-apps/<family>/<family>-jaribu-tests/`
via `dev test` (task 136), and (3) `dev template-smoke` tiers 1–3 for the exemplars. Runfile
authoring convention: **`tw-feature-placement`** skill (Co-located Jaribu runfile preamble
section).

## Platform packages (foundation + analyzers + identity)

Greenfield `dotnet new timewarp-architecture` apps **always** reference **published NuGet packages**
for foundation, analyzers, and identity (package-mode only — vendored platform trees are
unconditionally excluded from template output). This monorepo dogfoods all three via
`ProjectReference` when source trees are present.

| PackageId | Contents |
|-----------|----------|
| `TimeWarp.Foundation.*` / `TimeWarp.Modules` | Runtime foundation layers (task 051) |
| `TimeWarp.Architecture.Analyzers` | Convention DiagnosticAnalyzers only (TWA0002–0016, TWA0020–0023) — safe repo-wide |
| `TimeWarp.Architecture.Generators` | Source generators + TWA0001, TWA0017/0018 (ingress route generation) — attach only where gens should run |
| `TimeWarp.Architecture.Attributes` | Runtime attributes (e.g. `[ApiEndpoint]`) — public library |
| `TimeWarp.Identity` | Principal identity (passkeys / agent keys); published since 2.0.0-beta.6 |

MSBuild dual-mode (auto-detects missing source trees; switches defined in ROOT
Directory.Build.props so the tests tree gets them too): `UseFoundationPackages` /
`UseAnalyzerPackages` / `UseIdentityPackages`. CPM `PackageVersion` pins for platform packages
**equal the release `<Version>`** and bump in the same commit as it (task 124 policy — packages
and template publish together in one release run, so pins always reference versions that exist
by the time any generated app restores; the old lag-behind-published policy shipped a template
whose pins predated its own release). Upgrade path for apps that still vendored
`source/analyzers/**`: see
`documentation/developer/how-to-guides/how-to-upgrade-to-analyzer-packages.md`.

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
- **IAssemblyMarker**: every product/platform assembly gets a generated interface marker
  (`GenerateAssemblyMarker` in root `Directory.Build.targets`; namespace via
  `AssemblyMarkerNamespace`, not `RootNamespace` alone — container-apps share one RootNamespace).
  Opt out with `TwGenerateAssemblyMarker=false`.
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
| TWA0005 | **retired** (task 131 F-002) — was MVC endpoint verb vs `[ApiRoute]`; ID reserved, do not reuse. FastEndpoints take verb from the contract at generation time |
| TWA0006 | every routed contract has a server endpoint or `[ClientOnlyContract(reason)]` |
| TWA0007 | Aspire `AddProject` resource names are `ServiceNames` constant values |
| TWA0008 | no template-conditional tokens in comments/strings (the dotnet-new engine misreads them and truncates generated files); escape hatch: the `cnd:noEmit` comment-marker pair |
| TWA0009 | product slices (`…Features.<Id>` under SliceRoot) must not reference other product slices (share via Components/contracts); platform `Applications` is one-way free; opt-out: `[CrossSliceReference(typeof(T), reason)]` |
| TWA0010 | a directive naming a template.json flag requires that flag in DefineConstants (else the region silently vanishes from the repo build) |
| TWA0011/0012 | an `IAggregateRoot` must declare a nested `Invariants : AbstractValidator<T>`, and it must be `private` (kept out of `AddValidatorsFromAssemblyContaining`) |
| TWA0013 | an `[ApiEndpoint]` contract must carry `[EndpointAuthorize]` or `[EndpointAllowAnonymous(reason)]` — the generator is fail-closed and emits no auth config for neither |
| TWA0014 | an `[ApiEndpoint]` contract's auth posture must not be contradictory: not both markers, and not `[EndpointAllowAnonymous]` paired with a nested `Query`/`Command` that declares `IAuthApiRequest` |
| TWA0015 | feature filename: registered function segment pairs with the wrong layer (see feature-filename-grammar.json); also fires on a routed function paired with the registered-unrouted `tests` layer (e.g. `create-role-handler-tests.cs`) |
| TWA0016 | feature filename: unregistered or mis-spelled function segment used as archetype (escape hatch `<name>-<layer>.cs` stays valid, including `<name>-tests.cs`) |
| TWA0017 | a generated ingress web prefix (`WebServerApiRoutePrefixes`) shadows another server's route space — it equals/parents a hosted route in another contracts assembly, or collides with an `IngressReservedPathPrefixes` entry (grpc) |
| TWA0018 | a web-contracts route cannot be collapsed to a top-level ingress prefix (bare `api` or a parameterized second segment like `api/{id}`) |
| TWA0019 | a name in `IngressWebContractAssemblies` matches no referenced assembly (typo / renamed assembly) — otherwise the ingress list would silently generate empty |
| TWA0020 | `[ApiEndpoint]` combined with `[ClientOnlyContract]` (outer or nested Query/Command) — generators skip ClientOnly; remove one of the markers |
| TWA0021 | mock SPA auth providers (`MockAuthenticationStateProvider` / `MockAccessTokenProvider`) registered outside `MockAuthenticationRegistration` — bypasses the Development/Testing + `Authentication:UseMock` fail-closed gate (task 145-009) |
| TWA0022 | direct `Send` on the mediator (`ISender`/`IMediator`, incl. the inherited `Mediator` member) anywhere in SPA client code — dispatch through the TimeWarp.State generated `<Name>ActionSet` method, which wires the `CancellationToken`. Gated on the Blazor WASM SDK's `UsingMicrosoftNETSdkBlazorWebAssembly`; razor-generated trees ARE analyzed, other `.g.cs` trees exempt (task 196) |
| TWA0023 | type-stem identifiers: named type that already names the role **is** the identifier (strip leading `I` on interfaces; two of the same type qualify with the type as head). **Default off** — enable with `dotnet_diagnostic.TWA0023.severity = warning`. Opt-out: `[TypeStemIdentifier(reason)]`. Rule prose: flow skill `tw-csharp`. |

**Generator diagnostics (TWE / SG)** live in
`source/analyzers/timewarp-architecture-analyzers/diagnostics/diagnostic-descriptors.cs`
(SSOT — do not redeclare private copies of these IDs):

| ID | Rule |
|----|------|
| TWE002 | `[ApiEndpoint]` contract missing nested `Query`/`Command` — no endpoint generated |
| TWE003 | route+verb conflict across `[ApiEndpoint]` contracts — **all** parties reported; **none** of the group generated |
| TWE005 | `[Page]` `Policy` must be a const field reference (not string literal / `nameof`) |
| TWE006 | `[TypedId]` target must be a `readonly partial record struct` |
| TWE007 | unresolvable route or `HttpVerb` (missing/empty `[ApiRoute]`, unknown verb) — fail-closed; no emission |
| SG001 | shared source-generator log (resilience backstop) |
| SG002 | `EnableApiEndpointGeneration` true but FastEndpoints / `BaseFastEndpoint` missing |
| SG010 | TypedId BCL surface generation failed (resilience) |
| SG011 | TypedId EF converter generation failed (resilience) |

Retired / reserved generator IDs (do not reuse without deliberate new meaning): **TWE001**,
**TWE004** (declared historically, never reported; deleted task 131-001 F-014).

**Slice isolation (TWA0009):** product code under SliceRoot must not reach other product
slices. Placement, platform `Applications`, sharing, and `[CrossSliceReference]` opt-out:
skill **`slice-isolation`** (`skills/tw-slice-isolation/SKILL.md`).

**Aggregate pattern (TWA0011/0012):** typed id, `Entity<TId>` base, fail-closed `Create`, named
mutations, private nested `Invariants`, save-time enforcement via `AggregateDbContext`. Pattern
SSOT: skill **`tw-aggregate-pattern`** (`skills/tw-aggregate-pattern/SKILL.md`).

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
  `[EndpointAuthorize]` or `[EndpointAllowAnonymous(reason)]`, always) + Handler + **co-located
  Jaribu** integration tests (happy path AND validation rejection). Backend validation comes from
  the mediator's `FluentValidationBehavior` — do not re-validate in handlers.
- **Client feature**: State/Actions/Components + serialization round-trips (prefer co-located
  Jaribu; suite-shaped `web-contracts-tests` is Jaribu MTP) for non-trivial shapes (ctor+Guard,
  envelopes, generated route properties).

## Task management

**Always work on a kanban task** when changing this repo (code, docs, config, skills, CI). Prefer an
existing open task; otherwise create one. Exceptions: pure Q&A, user-waived, read-only exploration.

`ganda kanban` over the `kanban/` tree (`backlog/`, `to-do/`, `in-progress/`, `done/`):
**always `ganda kanban create "title"`** (it assigns the number — never hand-number), then
`move`/`done` to transition. Keep checklist / `## Session` current; commit kanban mutations.
Do not create perpetual/never-closing tasks. See the `tw-kanban` skill.

## Documentation

`documentation/` (developer + conceptual guides, ADRs) — in-repo markdown is the documentation
of record; generated apps receive the tree in their template output. No published docs site
(re-evaluate a public presence when the repo gains an outward-facing audience).

## Cursor Cloud specific instructions

This repo ships a git-owned Cloud Agent environment under `.cursor/` (see
`.cursor/readme.md` for reuse on other TimeWarp repos).

- Image toolchain: Ubuntu 24.04, .NET 10 matching `global.json`, git, sudo,
  Docker-in-Docker (`fuse-overlayfs` / `iptables-legacy`), Aspire CLI.
- `install` restores the solution and self-installs `bin/dev`. It must terminate.
  Do not start `dev run` or `dockerd` from `install`.
- `start` brings up the Docker daemon so Aspire / container-backed tests can run.
- Build and test with this repo's pipeline: `dev build` (or
  `dotnet run tools/dev-cli/dev.cs -- build`). Warnings are errors; 0/0 is the
  only acceptable compile result.
- Secrets, egress allowlists, and dashboard snapshots stay in the Cursor
  dashboard. They are not a substitute for committing `.cursor/`.

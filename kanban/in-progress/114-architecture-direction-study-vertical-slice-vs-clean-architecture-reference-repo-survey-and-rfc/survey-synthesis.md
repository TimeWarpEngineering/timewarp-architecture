# Survey Synthesis — Architecture Direction (task 114)

Synthesized 2026-07-21 from five parallel surveys: `survey-trinsic.md`, `survey-casa.md`,
`survey-jasontaylor.md`, `survey-ardalis.md` (3 repos), `survey-fullstackhero.md`.

## Comparison matrix

| Axis | Trinsic (~2020, Cramer) | CASA (.NET 7, Morris) | JT CleanArch (.NET 10) | ardalis RiverBooks (.NET 10) | FSH (.NET 10) | **Incumbent (timewarp-architecture)** |
|---|---|---|---|---|---|---|
| Macro shape | slices across Client/Server/Contracts | clean layers as projects, feature folders inside | 4 clean layers + Aspire triad | modular monolith: project-per-module, DDD folders inside | modular monolith: module + Contracts pair ×10 | slices under features/ + foundation layers |
| Feature unit | `Features/<Area>/<Op>` mirrored per layer | `Features/<F>` per layer | use-case folder inside Application only | `UseCases/<F>/<Op>` inside module | `Features/vN/<Area>/<Op>` inside module | `features/<slice>` (TWA0009) |
| Endpoint tech | hand-written MVC shims | minimal APIs via runtime reflection | minimal APIs via `IEndpointGroup` reflection | FastEndpoints (thin, internal) | minimal APIs, static Map* per slice | **FastEndpoints source-generated from contracts** |
| Mediator | MediatR | MediatR 11 | **MediatR 14 (commercial!)** | Mediator (source-gen) | Mediator (source-gen) | TimeWarp.Mediator |
| Contracts seam | shared Api project + route consts | `IApiRequest<T>` + `GetRequestPath()` | none (server-internal commands) | `.Contracts` project per module | `.Contracts` project per module | web-contracts + `[ApiRoute]` generated members |
| Persistence | EF 3.1 SqlServer, anemic POCOs | EF 7 SqlServer, rich aggregates, save-time invariants, repo+spec | EF 10, choice of sqlite/sqlserver/postgres, 2 interceptors | **EF 10, DbContext+schema per module**, domain-event dispatch on save | **EF 10 Postgres, DbContext+schema per module**, outbox, multitenant default-on | PostgresDbContext (entity-free) w/ invariants guard + version hook; NO AppHost postgres yet |
| Cross-module comms | n/a | n/a | n/a | sync mediator contracts + integration events (domain→integration bridge) | mediator against other module's Contracts | components/contracts sharing per TWA0009; no async channel |
| Enforcement | **runtime reflection at boot** | **none** | **none (not even arch tests)** | NsDepCop (build-breaking) + NetArchTest | **15-file NetArchTest suite (test-time)** | **Roslyn analyzers TWA0001–0014 + generators (compile-time)** |
| Frontend | Blazor WASM + Blazor-State | Blazor WASM + Telerik | Angular/React choice | none (API) | React 19 ×2 | Blazor WASM + TimeWarp.State + FluentUI |
| Template mechanics | n/a | n/a | choice symbols + #if + modifiers | `--add module` item templates | **2 coarse flags via folder-exclusion, almost no #if** | 5 flags via #if regions (TWA0008/0010 guard) |
| Distribution | n/a | n/a | template only | template + `Ardalis.SharedKernel` pkg | **source-ownership: everything ProjectReference, nothing packaged** | **platform as versioned NuGet packages (Foundation/Analyzers)** |

## The industry convergence (strong signal — all current .NET 10 repos agree)

1. **Vertical slices are the feature unit everywhere.** Even Jason Taylor's canonical Clean
   Architecture is use-case-folders inside Application. Nobody ships pure layer-organized
   features anymore. The incumbent's slice-first layout is on the winning side.
2. **MediatR is abandoned** (licensing): ardalis and FSH → martinothamar Mediator (source-gen);
   JT still pins commercial MediatR 14 + AutoMapper 16 — a liability, not a pattern.
   TimeWarp.Mediator was the same move made earlier.
3. **Controllers are dead**: FastEndpoints (ardalis, incumbent) or minimal APIs (JT, FSH, CASA).
   The incumbent is the only one *generating* endpoints from contracts — everyone else
   hand-writes or reflects.
4. **`.Contracts` project per module is the standard public seam** (ardalis, FSH) — the
   compiler-checked reference graph (module may reference other modules' Contracts only) is the
   industry's strongest boundary mechanism, and it's *project*-granular, stronger than
   namespace-granular TWA0009 in one respect (can't even see internals) and weaker in another
   (no fine-grained rules inside a project).
5. **DbContext + schema per module** (RiverBooks, FSH, VCMM-03) is the persistence consensus for
   modular shapes, with domain-event dispatch on save (everyone) and rich DDD aggregates with
   static factories (FSH, CASA, JT — and the incumbent's golden aggregate 106).
6. **Aspire everywhere**; Postgres is the default database of the current generation (FSH
   default, JT option, incumbent flag).
7. **Nobody has compile-time architecture enforcement.** Best-in-class elsewhere: RiverBooks'
   NsDepCop (build-breaking but config-file-based) and FSH's excellent-but-test-time NetArchTest
   suite. JT and CASA have nothing. The incumbent's TWA analyzers + fail-closed generators are
   genuinely ahead of every surveyed repo — this is the moat; every structural decision must
   preserve or extend it.

## What the incumbent should steal (candidate resolutions, by source)

**Persistence (feeds 113):**
- DbContext + `HasDefaultSchema("<slice>")` per slice, self-migration hook (RiverBooks, FSH, VCMM-03) — analyzer-checkable: slice X's context maps only slice X's aggregates.
- Migrations as one-shot DbMigrator host gated by Aspire `WaitForCompletion` — never at API startup (FSH).
- SaveChanges interceptors for audit stamps + domain-event dispatch (JT, FSH) — the incumbent's invariants-guard hook already lives there; extend, don't multiply mechanisms.
- Outbox for integration events (FSH) — pairs with the RiverBooks domain→integration-event bridge.

**Boundaries / structure (the RFC core):**
- Domain-event → integration-event bridge in `Integrations/` per slice (RiverBooks) — async cross-slice channel that never leaks domain types; generator + TWA-check candidate.
- Per-slice registration via `static abstract` interface, but **enumerated by a source generator at build time** instead of startup reflection (improves on Modulith + FSH's ModuleLoader; kills FSH's hand-maintained assembly-list duplication).
- Intra-slice layer directionality (Domain ⊄ Data) as a new TWA rule — the NsDepCop capability, natively.
- Transport-swapping pipeline behavior (VCMM-04): same contract in-process today, bus-published later — the growth path story without microservices ceremony.
- Consider: `<Module>`/slice MSBuild metadata on projects so generators/tooling key off slice identity declaratively (RiverBooks).

**Template mechanics:**
- Folder-exclusion `sources.modifiers` for coarse toggles instead of in-file `#if` where granularity allows (FSH) — sidesteps the TWA0008 truncation bug class entirely; keep `#if` only where line-level granularity is required.
- `[Meta]`-style single-source validation metadata (CASA's idea, minus Fody) — a generator that derives FluentValidation + contract nullability from one source would subsume TWA0002/0003's manual agreement.
- Test the packed template across flag combos in CI (JT's `test-templates.yml`).

**Confirmed incumbent positions (survey evidence says keep):**
- OneOf<Response, SharedProblemDetails> over HTTP-flavored exceptions (FSH's `CustomException(HttpStatusCode)` is the anti-pattern).
- ContractSerializationDefaults over inline seam options (FSH configures enum-as-string inline in Program.cs — the exact drift TWA-philosophy forbids).
- Platform-as-packages distribution (vs FSH source-ownership) — keep, but the RFC should note FSH's "nothing to eject" pitch as the trade-off.
- Compile-time enforcement as the moat — never regress to test-time.

## Refined RFC decision axes (supersedes the draft list in task.md)

1. **Slice project granularity**: today product slices are folders inside layer projects
   (web-application etc.). Move to project-per-slice (+ optional `.Contracts` per slice, ardalis/FSH
   style) or keep folder-granular slices with namespace enforcement? Middle option: keep folders
   for template default, make project-per-slice the documented growth path with analyzer support
   for both.
2. **Contracts placement**: one web-contracts (today) vs per-slice contracts projects vs
   generated contracts assemblies. Interacts with 051 foundation packaging.
3. **Cross-slice communication**: add the async integration-event channel (bridge + outbox) as
   golden pattern next to the existing sync sharing rules? Which parts generated/TWA-enforced?
4. **Intra-slice layering**: adopt Domain⊄Data directionality as TWA00xx? What are the
   layer names inside a slice (the incumbent foundation split vs RiverBooks DDD folders)?
5. **Persistence shape** (joint with 113): DbContext-per-slice+schema vs single context;
   DbMigrator host; interceptor consolidation; where the actor-model question lands given the
   modular shape.
6. **Template flag mechanics**: migrate coarse flags to folder-exclusion modifiers; keep `#if`
   only for line-granular seams.
7. **.NET 11 posture** (Nov 2026): all decisions must be source-gen/AOT-friendly; no new
   runtime-reflection seams (three surveyed repos still boot-scan assemblies — decline).

## Bottom line

The surveys don't force a pivot — they largely *vindicate* the incumbent hybrid (slices over a
thin horizontal foundation, contracts as the seam, generated endpoints, compiler enforcement)
and show the whole field converging toward it from both directions (Clean people adding slices,
slice people adding contracts projects). The genuinely open calls are **granularity** (folders
vs projects per slice), the **async cross-slice channel**, **intra-slice layering enforcement**,
and the **persistence-per-slice shape** — which is exactly the ballot for the RFC.

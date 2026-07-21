# CASA (Casa.Careplace) — Architecture Survey

Peter Morris's example app. .NET 7, Blazor WebAssembly, hosted model, SQL Server. Single solution `Casa.Careplace.sln`, single product namespace `Casa.Careplace.*`. Azure DevOps pipeline (`azure-pipelines.yml`), no git history in the working copy.

## 1. Solution / project structure

Nine projects under `Source/Casa.Careplace/`:

| Project | SDK | Contents (one line) |
|---|---|---|
| **Contracts** | classlib | Shared client↔server seam: `IApiRequest<T>`, `ResponseBase`, `ApiEndpoint`, `AllFeatureEndpoints`, request/response DTOs under `Features/`, `Metas/` (shared validation-attribute sources), enums, `OpenDataQueryParameters`. |
| **Domain** | classlib | Aggregate roots / entities under `Entities/<Feature>/`, `EntityBase`, `AggregateRoot`, `ISpecification<T>` + concrete `Specifications/`, `Errors`, `SequentialGuidGenerator`, Moxy `.mixin` templates. |
| **Application** | classlib | Feature handlers, repositories, EF `ApplicationDbContext`, `UnitOfWork`, `RepositoryBase<T>`, MediatR pipeline behaviors (`RequestMiddlewares/`), domain/app services, AutoMapper profiles, EF migrations. |
| **Server** | Web SDK | Blazor WASM host. `Program.cs`, reflection-driven `RoutesRegistration`, `ServicesRegistration`, endpoint filters (`HttpFilters/`), Azure AD / JWT auth, App Insights. |
| **Client** | BlazorWebAssembly SDK | WASM SPA. Feature pages under `Features/<Feature>/`, `ApiService`, `ApiComponentBase`, Telerik UI, MSAL auth, Moxy mixins. |
| **SharedKernel** | classlib | Tiny — a few extension helpers, no dependencies. |
| **Validation** | classlib | `ValidatorBase<T>` (DataAnnotations→FluentValidation bridge), `ValidationError`, custom validation attributes. No dependencies. |
| **ApplicationTests** | xUnit | One test file (`AutoMapperConfigurationTests`). |
| **ClientTests** | xUnit | One test file (query-string helper). |

## 2. Architecture style

**Hybrid: clean-architecture layers as projects, vertical feature-folders inside each layer.** The layer boundary is the project (Contracts / Domain / Application / Server / Client); within Application, Contracts, and Client, code is organized by feature (`Features/UserInvitations/`, `Features/Organisations/`, `Features/ServiceProviders/`, …) and within Domain by `Entities/<Feature>/`. A single feature is therefore a slice threaded through multiple layer-projects — not a self-contained module. There is no per-feature assembly or module isolation.

Notable deviation from textbook onion: **Domain references Contracts** (`Domain.csproj → Contracts.csproj`) so entities can reuse the shared `Metas/` validation-attribute classes and enums. Dependency inward-purity is traded for DRY validation metadata.

## 3. Feature flow end-to-end

Endpoint tech is **minimal APIs, auto-registered by reflection** — no controllers, no FastEndpoints, no hand-written endpoint classes.

- **Contract** (`Contracts/Features/UserInvitations/SysAdminUserInvitationCreateCommand.cs`): a DTO implementing `IApiRequest<TResponse>` (which extends MediatR `IRequest<TResponse>`) with a `static abstract string GetRequestPath()`. Class-name suffix carries the verb: `…Query` → GET, `…Command` → POST (`ApiRequestHelper.IsQuery`). Some contracts nest a FluentValidation `Invariants : ValidatorBase<T>`.
- **Registration** (`Contracts/Features/AllFeatureEndpoints.cs`): reflects over the Contracts assembly's exported types, finds everything implementing `IApiRequest<>`, builds an `ApiEndpoint` (request type, response type, path, isQuery). `Server/RoutesRegistration.cs` iterates that list and does `MapGet`/`MapPost` via a generic method invoked reflectively. Adding an endpoint = adding a contract class; the server wires it automatically.
- **Dispatch**: the minimal-API handler is a thin lambda taking `[AsParameters]`/`[FromBody] TRequest` + `IRequestDispatcher`, which wraps `IMediator.Send` (`Application/Services/RequestDispatcher.cs`).
- **Pipeline** (`Application/RequestMiddlewares/`): two ordered MediatR `IPipelineBehavior`s — `RequestValidatorMiddleware` (validates request, short-circuits to `BadRequest` + `ValidationErrors`) then `RequestErrorHandlerMiddleware` (catches DB concurrency/unique/FK-violation and domain-invariant exceptions, maps to `ResponseStatus`). "Validate on the way in, handle errors on the way out."
- **Handler** (`Application/Features/…/…CommandHandler.cs`): `internal sealed IRequestHandler<,>`, does its own coarse authorization (`AuthorizationService.DemandSystemAdministratorAccess()`), talks to repositories, AutoMapper for DTO↔entity, `UnitOfWork.CommitAsync`, publishes domain events via `IEventDispatcher`. Returns a `ResponseBase`-derived response — handlers never throw for business errors, they return status-bearing responses.
- **Client** (`Client/Services/ApiService.cs` + `Components/ApiComponentBase.cs`): reuses the **same** `IApiRequest<TResponse>` contract, calls `GetRequestPath()` to build the URL, GET-vs-POST from the same Query/Command convention. One generic `ApiService` for all endpoints — no per-endpoint typed client.

## 4. Persistence

- **EF Core 7 + SQL Server**, lazy-loading proxies, NetTopologySuite (spatial), retry-on-failure.
- **Single `ApplicationDbContext`** (`internal sealed`), `DbSet`s with private setters, `ApplyConfigurationsFromAssembly` picks up per-feature `IEntityTypeConfiguration<T>` (`…PersistenceConfiguration.cs`). Uses `FieldDuringConstruction` access mode + `IncludePrivateState()` to map encapsulated fields/owned types.
- **Aggregate pattern**: `EntityBase` (client-generated sequential-GUID `Id`, private setter) → `AggregateRoot` (adds `[Timestamp]` rowversion for optimistic concurrency). Entities are rich (private ctors, encapsulated state, owned value types like `OneTimeCode`).
- **Domain invariants enforced at save**: `SaveChangesAsync` overridden — before committing, `CheckDomainInvariantsAsync` collects changed `AggregateRoot`s and runs `IDomainInvariantsGuard`, which requires each to have a FluentValidation validator (the nested `Invariants`) and throws `DomainInvariantViolationException` on failure. `ChangeTracker.AutoDetectChangesEnabled` is off by default (read performance) and only turned on inside `UnitOfWork`, so writes must go through the UoW.
- **Repository + Specification**: generic `RepositoryBase<T : AggregateRoot>` with an in-memory identity `Cache`, `IncludeAggregateParts` hook for eager-loading aggregate parts, `GetAsync(ISpecification<T>[])`, and a generic OData `SearchAsync<TResult>` (Community.OData.Linq + AutoMapper `ProjectTo`). Specifications are small `ISpecification<T>` classes (`Apply(IQueryable<T>)`) living beside the entity.
- **SQL-exception translation**: DbContext parses SQL error numbers (2601 unique, 547 FK) and regex-matches constraint names into typed exceptions the pipeline maps to validation errors — requires a naming convention on indexes/FKs.
- **Migrations**: classic EF `Add-Migration` design-time flow, migrations checked into `Application/Persistence/DbMigrations/` (dated files, Feb–Mar 2023). Batch files at repo root (`AddMigration.bat`, `UpdateDB.bat`, `GenerateDbScript.bat`).

## 5. Frontend

**Blazor WebAssembly, hosted** (server serves the SPA + API). This is .NET 7, so **no render modes** (predates .NET 8 Auto/Server/WASM). Client-side rendering only. Auth via MSAL + Azure AD (`Microsoft.Authentication.WebAssembly.Msal`, `CascadingAuthenticationState`, `AuthorizeRouteView`). UI kit: **Telerik.UI.for.Blazor** wrapped in `TelerikRootComponent`. Pages use `.razor` + `.razor.cs` + `.razor.css` (isolation) + sometimes `.razor.Model.cs`. No Fluxor/state library — components derive from `ApiComponentBase`, which centralizes API calls, busy-message stack, toast notifications, server-validation-error surfacing, and concurrency-conflict dialogs.

## 6. Dependency rules & enforcement

Reference graph (from csproj):
```
Validation      → (none)
SharedKernel    → (none)
Contracts       → Validation
Domain          → Contracts, Validation
Application     → Contracts, Domain, SharedKernel, Validation
Client          → Contracts, Validation
Server          → Application, Client, Contracts, Domain   (composition root)
```
Client is cleanly limited to Contracts + Validation (no Domain/Application leakage). **Enforcement is conventional only** — there are **no architecture-enforcing analyzers, no ArchUnit-style tests, no build-time rules**. Conventions are load-bearing but unchecked: Query/Command name suffixes, index/FK naming for exception translation, one `IApiRequest<>` per contract (checked at runtime in `AllFeatureEndpoints`, not at build). `TreatWarningsAsErrors` is on (Release and via a second unconditional PropertyGroup), `Nullable` is **disabled** repo-wide.

## 7. .NET / C# version & notable packages

- **.NET 7** (`global.json` pins SDK 7.0.101, roll-forward latestFeature), C# latest, `ImplicitUsings` on, **`Nullable` disabled**.
- MediatR **11**, FluentValidation 11, AutoMapper 12, EF Core 7 (SqlServer + Proxies + NetTopologySuite), Community.OData.Linq, LinqKit.
- Auth: Microsoft.Identity.Web 2.6, JwtBearer/OpenIdConnect, MSAL.
- UI: Telerik.UI.for.Blazor 4.1; Morris.Blazor.FluentValidation / .Validation / .Web.Modal; Morris.EasyAuth.
- **Source-gen / weaving**: **Morris.Moxy** (`.mixin` templates → partial-class code generation, e.g. `MixinFullName`), **MetaMerge.Fody** (IL-weaves shared `[Meta(typeof(...))]` attribute metadata onto properties).
- Integrations: Azure.Storage.Blobs, Mailjet.Api, Microsoft.PowerBI.Api, ApplicationInsights.
- Tests: **xUnit** + coverlet (but only two test files exist).

## 8. Patterns worth stealing for a dotnet-new template

1. **Reflection-driven endpoint registration from shared contracts** — `Contracts/Features/AllFeatureEndpoints.cs` discovers every `IApiRequest<>` and `Server/RoutesRegistration.cs` maps them. Zero per-endpoint host code. (TimeWarp achieves the same intent with a source generator + `[ApiEndpoint]` — CASA is the runtime-reflection version of the same idea.)
2. **One contract type is the single source of truth for client + server + route** — `IApiRequest<TResponse>` with `static abstract GetRequestPath()`; both `ApiService` (client) and `RoutesRegistration` (server) consume it. Directly parallels TimeWarp's contract seam.
3. **Nested `Invariants : ValidatorBase<T>` on aggregate roots, enforced inside `SaveChangesAsync`** (`ApplicationDbContext.CheckDomainInvariantsAsync` + `DomainInvariantsGuard`). This is essentially TWA0011/0012 done at runtime — invariants can't be bypassed because the DbContext refuses to persist an invalid aggregate.
4. **Two-stage MediatR pipeline: validate-in / translate-errors-out** returning status-bearing `ResponseBase` instead of throwing (`RequestMiddlewares/`). Uniform error surface for the client.
5. **`[Meta(typeof(XxxMeta))]` single-source validation metadata** (`Contracts/Metas/`) IL-merged by MetaMerge.Fody onto both the DTO and the domain entity, then `ValidatorBase<T>` converts those DataAnnotations into FluentValidation rules (`FluentValidationDataAnnotationsHelper`). Define `[Required, MaxLength(64), Email]` once, applied everywhere. Strong "generate/derive rather than repeat" fit for the template philosophy.
6. **SQL exception → typed domain exception translation** via constraint-name regex convention (`ApplicationDbContext`), turning DB unique/FK violations into field-level `ValidationError`s.
7. **Generic `RepositoryBase<T>` with identity-map cache, `IncludeAggregateParts` hook, and generic OData `SearchAsync<TResult>`** — a clean aggregate-oriented repository with paged server-side querying in ~one base class.
8. **`ApiComponentBase`** consolidating busy-state, toasts, server-error binding, and concurrency dialogs for all API-calling Blazor components.

## 9. Weaknesses / dated aspects

- **.NET 7 (out of support), `Nullable` disabled** — heavy `?? throw new ArgumentNullException` boilerplate in every ctor instead of NRT.
- **No architecture enforcement** — every convention (Query/Command suffix, one `IApiRequest` per type, index/FK naming, aggregate must have `Invariants`) is unchecked at build time; several fail only at runtime. This is exactly the gap TimeWarp's TWA analyzers close.
- **Runtime reflection for endpoint wiring** — `MakeGenericMethod`/`Invoke` per endpoint; not AOT/trimming-friendly, slower cold start, errors surface at boot not build. A source generator would be strictly better.
- **Blazor WASM-only, pre-render-modes** — no SSR/streaming; the whole SPA ships to the client.
- **Fody (`MetaMerge.Fody`) IL weaving** — build-time magic that's opaque to tooling and a maintenance/tooling-compat risk; a source generator is the modern equivalent.
- **Coupled to commercial UI (Telerik)** and heavy Azure/MSAL/PowerBI assumptions — not neutral template material.
- **Near-zero test coverage** (2 test files) despite full xUnit setup; no integration tests for the endpoint/handler flow.
- **Domain → Contracts reference** inverts clean-architecture purity (pragmatic, but a smell).
- Manual **AutoMapper** everywhere (v12, pre-commercial-license era) — mapping is unenforced and easy to drift; several `TODO: PeteM` comments remain in core files (`RepositoryBase`, `DbContext`).

---

### Executive summary
CASA is a .NET 7 Blazor-WASM hosted app in **clean-architecture layers (projects) with per-feature folders inside each layer** — a layered/vertical hybrid, not a modular monolith. Its defining move is a **shared `IApiRequest<TResponse>` contract seam** that a single reflection pass turns into minimal-API routes on the server and HTTP calls on the client, with **MediatR handlers behind a validate-in/translate-errors-out pipeline** returning status-bearing responses. Persistence is **rich DDD aggregates over one EF Core `DbContext`**, with **domain invariants (nested FluentValidation `Invariants`) enforced inside `SaveChangesAsync`** and a generic caching **repository + specification + OData search** base. The standout reusable ideas — contract-driven endpoint generation, single-source `[Meta]` validation metadata, and save-time invariant enforcement — are the same patterns TimeWarp implements, but CASA does them with **runtime reflection and Fody IL-weaving and no build-time enforcement**. It is dated (out-of-support .NET, `Nullable` off, Telerik/Fody coupling, ~zero tests), so it's best mined as a **pattern reference**, not a code source.

Key evidence files: `Contracts/Features/AllFeatureEndpoints.cs`, `Contracts/IApiRequest.cs`, `Server/RoutesRegistration.cs`, `Application/RequestMiddlewares/*.cs`, `Application/Persistence/{ApplicationDbContext,RepositoryBase,UnitOfWork}.cs`, `Application/Services/DomainInvariantsGuard.cs`, `Domain/Entities/{AggregateRoot,EntityBase}.cs`, `Contracts/Metas/*.cs`, `Domain/MoxyMixins/MixinFullName.mixin`, `Client/Services/ApiService.cs`, `Client/Components/ApiComponentBase.razor.cs`.

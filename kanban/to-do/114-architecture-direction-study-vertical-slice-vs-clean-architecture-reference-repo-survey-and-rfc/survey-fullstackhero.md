# FullStackHero .NET Starter Kit — Architecture Survey

Repo: `/home/steve/worktrees/github.com/fullstackhero/dotnet-starter-kit/main` · .NET 10 · MIT · commit `6f381db4`. Self-described "production-ready, modular .NET 10 monolith + two React 19 apps." Directly comparable to a `dotnet new` architecture template, but with a very different distribution philosophy (source-ownership, not packages) and a different enforcement mechanism (architecture *tests*, not Roslyn analyzers).

## 1. Solution / project structure

No `.sln`/`.slnx` checked in at root (the CLI probes for a generated `.slnx`); projects are grouped under `src/` in four tiers. One `Directory.Build.props` + `Directory.Packages.props` (CPM).

**BuildingBlocks** (`src/BuildingBlocks/`) — framework layer, namespace `FSH.Framework.*`:
- `Core` — domain primitives (`AggregateRoot`, `IEntity`, `IDomainEvent`, `ISoftDeletable`), dependency-free (enforced).
- `Shared` — cross-cutting contracts: `AppTenantInfo`, `FshPermission`, authorization constants, `RequirePermission` endpoint extension.
- `Persistence` — `BaseDbContext`, EF interceptors (audit + domain-events), Specification pattern, pagination, tenant-isolation extensions, `IDbInitializer`.
- `Web` — `IModule`/`ModuleLoader`/`FshModuleAttribute`, idempotency filter, platform bootstrap (`AddHeroPlatform`/`UseHeroPlatform`).
- `Caching` (HybridCache/Valkey), `Eventing` + `Eventing.Abstractions` (outbox/domain-event dispatch), `Jobs` (Hangfire), `Mailing` (MailKit/SendGrid), `Storage` (S3/MinIO presigned), `Quota`.

**Modules** (`src/Modules/`) — 10 bounded contexts, namespace `FSH.Modules.*`: Identity, Multitenancy, Billing, Catalog, Tickets, Chat, Files, Webhooks, Auditing, Notifications. **Each is a *pair* of projects**: `Modules.X` (runtime) + `Modules.X.Contracts` (its only public surface).

**Host** (`src/Host/`):
- `FSH.Starter.Api` — the single ASP.NET Core host (minimal APIs) that loads all modules.
- `FSH.Starter.AppHost` — .NET Aspire orchestrator.
- `FSH.Starter.DbMigrator` — one-shot console (verbs `apply --seed`, `seed-demo`); migrations never run at API startup.
- `FSH.Starter.Migrations.PostgreSQL` — single migrations assembly with **per-module subfolders** (Catalog/, Identity/, Billing/…), one physical assembly holding every module's migration set.

**Tests** (`src/Tests/`) — 14 projects: one per module + `Architecture.Tests`, `Integration.Tests`, `Framework.Tests`, `Generic.Tests`. Tests use **xUnit + Shouldly + NetArchTest.Rules** (note: xUnit, not the target repo's Fixie).

**Tools** (`src/Tools/CLI`) — the `fsh` CLI (Spectre.Console), excluded from the shipped template.

## 2. Architecture style — vertical slice inside a modular monolith (verified)

The VSA claim holds up cleanly. Inside `Modules.Catalog/Features/v1/{Brands,Categories,Products}/<Operation>/` each slice is a self-contained folder of `XCommandHandler.cs` + `XEndpoint.cs` + `XCommandValidator.cs` (e.g. `Features/v1/Products/CreateProduct/`). The command/query *record* itself lives across the boundary in `Modules.Catalog.Contracts/v1/Products/CreateProductCommand.cs`. Handlers talk directly to the module's `DbContext` — no repository indirection for the common path (Specification pattern exists in BuildingBlocks for query composition but sampled handlers use `DbContext` + LINQ directly).

**Module boundary & registration** — the distinctive infra worth studying:
- `IModule` (`ConfigureServices` / `MapEndpoints` / `ConfigureMiddleware`) in `BuildingBlocks/Web/Modules/IModule.cs`.
- Each module publishes itself via an **assembly-level attribute** with an ordering hint: `[assembly: FshModule(typeof(CatalogModule), 600)]` (top of `CatalogModule.cs`).
- `ModuleLoader.AddModules()` reflects over assemblies, finds `FshModuleAttribute`s, orders by `Order`, instantiates each, calls `ConfigureServices`; `MapModules()` later calls `MapEndpoints`. The list of assemblies is still hand-maintained in `Program.cs` (both the `AddMediator` assembly list *and* the `moduleAssemblies` array — duplication there).
- Each module maps its own versioned route group: `endpoints.MapGroup("api/v{version:apiVersion}/catalog").RequireAuthorization()` in `CatalogModule.MapEndpoints`, then calls each slice's `MapXEndpoint()`.

Modules communicate only through `*.Contracts` — cross-module calls go via Mediator against the *other* module's contract types (e.g. Catalog references `FSH.Modules.Files.Contracts` for a file-access policy, never `Modules.Files`).

## 3. Feature flow end to end

Endpoint tech: **ASP.NET Core Minimal APIs** (not FastEndpoints, not MVC controllers). Each endpoint is a `static` class with an `internal static RouteHandlerBuilder MapXEndpoint(this IEndpointRouteBuilder)` extension — verified by `EndpointConventionTests`.

CQRS: **`martinothamar/Mediator`** — the *source-generated* mediator (`ICommand<T>`/`IQuery<T>`/`ICommandHandler`), registered with `ServiceLifetime.Scoped`. Handlers return `ValueTask<T>`. (Same spirit as the target repo's TimeWarp.Mediator, but FSH uses Mediator+source-gen and is `OneOf`-free, using exceptions.)

The `CreateProduct` slice, concretely:
1. `CreateProductCommand(record) : ICommand<Guid>` in Contracts.
2. `CreateProductEndpoint`: `endpoints.MapPost("/products", (cmd, IMediator, ct) => Results.Ok(await mediator.Send(cmd,ct))).RequirePermission(CatalogPermissions.Products.Create).WithIdempotency()`.
3. `CreateProductCommandValidator : AbstractValidator<CreateProductCommand>` (FluentValidation) — invoked by a mediator pipeline behavior, not in the handler.
4. `CreateProductCommandHandler` — checks brand/category existence, calls the `Product.Create(...)` **domain factory**, enforces SKU/slug uniqueness (throws `NotFoundException`/`CustomException` with HTTP status), `dbContext.Products.Add` + `SaveChangesAsync`.

Errors surface as RFC 9457 ProblemDetails via a global exception handler. Validation is *not* re-done in handlers.

## 4. Persistence

- **EF Core 10 + Npgsql (PostgreSQL)**; also references SqlServer/InMemory providers.
- **DbContext-per-module**: `CatalogDbContext : BaseDbContext` with `const Schema = "catalog"`, `HasDefaultSchema(Schema)`, `ApplyConfigurationsFromAssembly`. Each module owns its schema; migrations for all contexts live in the one `Migrations.PostgreSQL` assembly, subfoldered by module.
- **Multitenancy via Finbuckle.MultiTenant 10**: `BaseDbContext : MultiTenantDbContext`. `ApplyTenantIsolationByDefault()` marks every entity `IsMultiTenant()` unless it implements `IGlobalEntity` — **isolation is opt-out, default-on**, and enforced by `TenantIsolationTests` (a context entity must be tenant-isolated *or* explicitly marked global). Per-tenant connection strings supported (a tenant can have its own DB); `TenantNotSetMode.Overwrite` on save.
- **Soft-delete + audit** as global query filters + save interceptors (`AppendGlobalQueryFilter<ISoftDeletable>`, `AuditableEntitySaveChangesInterceptor`, `DomainEventsInterceptor`). Domain events dispatched via an **outbox** (`Eventing`, with a hosted `OutboxDispatcherHostedService` and dead-letter/back-off handling per recent commits).
- **Aggregate pattern is real DDD**: `Product : AggregateRoot<Guid>, ISoftDeletable` has a private ctor, static `Create` factory, private setters, behavior methods (`ChangePrice`, `AdjustStock`, `AddImage`) that raise domain events via `AddDomainEvent(DomainEvent.Create(...))`, and a private `_images` list exposed as `IReadOnlyList`. `DomainEntityTests` enforce: aggregates reference other aggregates by Id only, domain events sealed + `IDomainEvent`, value objects immutable. Uses `Guid.CreateVersion7()` for IDs.
- Migrations applied only by DbMigrator as a deploy step (Aspire `WaitForCompletion(migrator)` gates the API).

## 5. Frontend

**Not Blazor.** Two **React 19 + Vite 7 + TypeScript** SPAs under `clients/`: `admin` (operator console, port 5173) and `dashboard` (tenant app, 5174). Stack: TanStack Query v5, React Router 7 (`react-router-dom`), React Hook Form + Zod, Radix UI + Tailwind v4 (shadcn-style), `@microsoft/signalr` for realtime, `sonner` toasts. Hand-written typed API client (`src/api`), runtime `/config.json` (no per-env rebuild), Playwright E2E suites mirroring backend modules (`tests/{tenants,billing,webhooks,...}`). Aspire wires them via `AddJavaScriptApp(...).WithNpm().WithReference(api)` with `VITE_API_BASE_URL` pointed at the API's HTTPS endpoint.

## 6. Dependency rules & enforcement — architecture *tests*, not analyzers

This is the sharpest contrast with the target repo (which uses build-breaking Roslyn analyzers TWA0001–0014). FSH enforces conventions through **`Architecture.Tests` (xUnit + NetArchTest + reflection/csproj XML parsing)** — 15 test files. Key rules:

- **CircularReferenceTests** — DFS/topological-sort over all `.csproj` ProjectReferences; no cycles anywhere, and modules specifically may not cycle.
- **ContractsPurityTests** — `*.Contracts` must not depend on EF Core, FluentValidation, or Hangfire; must contain no `DbContext`/concrete repository types; must not reference module implementations; commands/queries must be `record` or `sealed`.
- **ModuleArchitectureTests** — a module runtime project may reference *other modules' Contracts* but not their implementations (`Modules_Should_Not_Depend_On_Other_Modules`).
- **LayerDependencyTests** — Core has zero deps and no EF/AspNetCore; Domain types don't depend on Persistence/Infrastructure; Features don't touch AspNetCore directly.
- **BuildingBlocksIndependenceTests** — BuildingBlocks never reference Modules or Hosts; Core building block is dependency-free; layered order enforced.
- **HandlerValidatorPairingTests** — every command handler has a matching validator; paginated query handlers have validators; no orphan validators. (FSH's analog to the target repo's TWA0002/0003 pairing, done as a test.)
- **EndpointConventionTests** — endpoints are static classes in a `Features` namespace, have a `Map*` method returning `RouteHandlerBuilder` taking `IEndpointRouteBuilder`, contain no business logic, follow naming.
- **AuthorizationMetadataTests** — exactly one `RequiredPermissionAttribute` across all assemblies, implementing `IRequiredPermissionMetadata` (the permission-metadata seam that `RequirePermission` feeds).
- **TenantIsolationTests / DomainEntityTests / FeatureArchitectureTests** (v1 can't depend on newer versions) / **NamespaceConventionsTests** (namespace matches folder).

Build hygiene: `TreatWarningsAsErrors`, `AnalysisMode=AllEnabledByDefault`, SonarAnalyzer.CSharp repo-wide — but these are generic analyzers, not domain-specific codegen. There are **no source generators authored by the project** (Mediator's generator is third-party).

## 7. Versions, Aspire, notable packages

- **.NET 10** (`global.json` 10.0.100, `net10.0`), C# `latest`, Nullable + ImplicitUsings on, CPM with transitive pinning.
- **.NET Aspire 13.4** — full orchestration: Postgres + pgAdmin, Valkey (plain container, not `AddRedis` — they dropped to RESP/TCP because 13.4 forces TLS), RedisInsight, MinIO + `mc` init container, DbMigrator, demo-seeder, API, both React apps. Explicit `WaitFor`/`WaitForCompletion` DAG so the API never boots against an unmigrated DB or cold pool.
- Notable packages: `Mediator.SourceGenerator` 3.0.2, `Finbuckle.MultiTenant` 10.1, `FluentValidation` 12.1, `Hangfire` 1.8, `Asp.Versioning` 10, `Microsoft.Extensions.Caching.Hybrid` (HybridCache), full OpenTelemetry stack + Serilog, `QuestPDF`, MailKit, JwtBearer + ASP.NET Identity.

## 8. Packaging / shipping as a template — vs preprocessor flags

Distribution model is **source-ownership**, stated explicitly in `Directory.Build.props`: only **two** NuGet packages ship — `FullStackHero.NET.StarterKit` (the `dotnet new` template) and `FullStackHero.CLI` (the `fsh` tool). All 33 module/framework projects are `<IsPackable>false</IsPackable>` and consumed by `ProjectReference` in the generated app — "no hidden NuGet runtime, nothing to eject." (The target repo does the opposite for greenfield apps: `TimeWarp.Foundation.*`/`Analyzers` ship *as packages*, dogfooded here via ProjectReference. Different bet: FSH gives you all the framework source to edit; TimeWarp keeps the platform as versioned packages.)

**Template mechanics** (`.template.config/template.json`, `sourceName: "FSH.Starter"`, shortName `fsh`):
- **Feature toggles are coarse and few**: only `aspire` and `frontend` (both bool, default true), plus `skipRestore`, `includeTools`, and string params for OpenAPI contact/mail. Toggling is done via **`sources.modifiers` folder-exclusion** (`condition: "(!aspire)"` excludes `AppHost/**`; `(!frontend)` excludes `clients/**`) — **not** in-file `#if` preprocessor regions. Only *one* `#if (frontend)` C# region exists, in `AppHost/Program.cs`, to drop the React app wiring.
- Renames/derived symbols: `README-template.md → README.md`, `underscoreForm`/`kebabForm`/`displayForm` transforms rewrite namespaces, issuer slug, brand, OpenAPI title from the project name.
- The `fsh` CLI (`NewCommand.cs`) is a thin wrapper: interactively prompts, ensures the template is installed (`dotnet new install FullStackHero.NET.StarterKit`), then shells `dotnet new fsh -n <name> --aspire <b> --frontend <b> --force`, optionally `git init` + `npm install`, and injects a generated JWT signing key over the placeholder. It adds nothing `dotnet new` can't do — UX polish + post-scaffold key generation.

**Contrast with the target repo's flags**: TimeWarp uses 5 architecture-axis preprocessor flags (`api/grpc/web/yarp/postgres`) with in-file `#if`/`<!--#if-->` regions and an analyzer (TWA0008/0010) guarding them. FSH deliberately avoids in-file conditionals almost entirely, toggling whole *folders* instead — far simpler to author and immune to the "dotnet-new engine truncates on conditional tokens" class of bug, at the cost of coarser granularity (you can't conditionally include one module).

## 9. Patterns worth stealing — and what to avoid

**Steal:**
- **Assembly-attribute module registration with ordering** — `[assembly: FshModule(typeof(CatalogModule), 600)]` + reflective `ModuleLoader` (`BuildingBlocks/Web/Modules/`). Clean, discoverable, decouples module list from host. The target repo could *generate* the `moduleAssemblies` array from these attributes and kill the hand-maintained duplication FSH still has in `Program.cs`.
- **`*.Contracts` project as the *only* module public surface, enforced by `ContractsPurityTests`** — physically prevents contracts from leaking EF/validation/Hangfire. A project-boundary version of the target repo's slice-isolation (TWA0009); the two-project-per-module split is stronger than namespace rules because the compiler enforces the reference graph.
- **Default-on tenant isolation with opt-out marker + a test that fails if you forget** (`ApplyTenantIsolationByDefault` + `IGlobalEntity` + `TenantIsolationTests`). Fail-safe default is the right polarity — mirrors the target repo's fail-closed auth philosophy.
- **Rich aggregate roots with static factories + domain events + private collections** (`Domain/Product.cs`) — textbook, and `DomainEntityTests` keep them honest.
- **Migrations as a separate one-shot host with verbs, gated by Aspire `WaitForCompletion`** (`DbMigrator` + AppHost DAG) — "migrations never run at API startup" is a production-correct pattern the target repo could adopt.
- **Folder-exclusion template toggles instead of in-file `#if`** for coarse features — dramatically less fragile than preprocessor regions.
- **`WithIdempotency()` endpoint filter** and **`RequirePermission(const)`** with a permissions-as-constants registry (`CatalogPermissions.All` + `PermissionConstants.Register`) — clean, testable authZ metadata seam.

**Avoid / weaker than the target repo's approach:**
- **Conventions enforced by tests, not analyzers.** NetArchTest rules only fail in the test run, produce no in-editor feedback, and are easy to skip. The target repo's build-breaking Roslyn analyzers (TWA*) catch violations at *compile* time in every project and in the IDE — strictly better feedback. Don't regress toward test-only enforcement.
- **No source generators of their own** — everything (route mapping, mediator registration, module assembly list) is hand-written or reflective. The `AddMediator` assembly list and `moduleAssemblies` array in `Program.cs` are **duplicated and hand-maintained**; a generator would eliminate both. Clearest place FSH is behind.
- **Handlers hit `DbContext` directly and throw HTTP-flavored exceptions** (`CustomException(..., HttpStatusCode.Conflict)`) — couples the domain/application layer to HTTP semantics. The target repo's `OneOf<Response, SharedProblemDetails>` return contract is cleaner separation.
- **Enum-serialization-by-string configured inline in `Program.cs`** with a comment explaining `[Flags]` exceptions — exactly the "seam options declared inline" the target repo forbids (`ContractSerializationDefaults`). Don't copy.
- **xUnit + reflection-heavy arch tests** — the target repo standardizes on Fixie + Shouldly; FSH's xUnit choice and its ~400-line hand-rolled DFS cycle detector are not worth importing.

## Executive summary (5 lines)

1. FSH is a **.NET 10 modular monolith** — 10 bounded-context modules, each a `Modules.X` + `Modules.X.Contracts` project pair, loaded into a single minimal-API host via an assembly-attribute + reflective `ModuleLoader`; front-end is **two React 19 SPAs**, not Blazor.
2. Vertical slices are genuine: `Features/vN/<Area>/<Operation>/` folders of Handler+Endpoint+Validator over **source-generated Mediator CQRS**, FluentValidation in a pipeline, and **rich DDD aggregates** with static factories and outbox-dispatched domain events.
3. Persistence is **EF Core 10 / Postgres, DbContext-and-schema per module**, one shared migrations assembly, **Finbuckle multitenancy with default-on isolation**, soft-delete + audit interceptors, and a **one-shot DbMigrator gated by Aspire** so migrations never run at API startup.
4. Conventions are enforced by a **15-file `Architecture.Tests` suite (xUnit + NetArchTest)** — contract purity, no module cycles, handler/validator pairing, tenant isolation, endpoint shape — i.e. *test-time* rather than the target repo's *compile-time Roslyn analyzers*.
5. It ships **source-ownership style**: only the template + `fsh` CLI are NuGet packages, everything else is ProjectReference; template toggles are just two coarse **folder-exclusion flags** (`aspire`, `frontend`) with almost no in-file `#if` — simpler and less fragile than preprocessor regions, but coarser, and it authors **zero source generators**, leaving hand-maintained duplication the target repo's generator-first philosophy would remove.

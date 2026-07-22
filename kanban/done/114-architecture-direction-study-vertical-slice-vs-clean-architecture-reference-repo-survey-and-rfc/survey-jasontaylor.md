# Jason Taylor Clean Architecture (`ca-sln`) — Structural Survey

Repo: `jasontaylordev/CleanArchitecture` · template shortName `ca-sln` · **.NET 10** (`global.json` pins SDK 10.0.201). This is the current Aspire-era rewrite — significantly different from the pre-2024 versions people remember.

## 1. Solution / project structure

`CleanArchitecture.slnx` with the canonical four layers plus three infra projects:

| Project | Role / what lives here |
|---|---|
| `src/Domain` | Entities (`TodoItem`, `TodoList`), `Common/` base types (`BaseEntity`, `BaseAuditableEntity`, `BaseEvent`, `ValueObject`), `Events/`, `Enums/`, `ValueObjects/`, `Constants/`, `Exceptions/`. Zero dependencies. |
| `src/Application` | Use-case slices + `Common/` (Behaviours, Interfaces, Models, Security, Exceptions, Mappings). Depends on Domain only. Defines `IApplicationDbContext`, `IIdentityService`, `IUser` — the ports. |
| `src/Infrastructure` | `Data/` (`ApplicationDbContext`, `Configurations/`, `Interceptors/`, `ApplicationDbContextInitialiser`), `Identity/`. Implements Application's interfaces. |
| `src/Web` | Minimal-API host: `Endpoints/`, `Infrastructure/` (endpoint conventions + OpenAPI transformers), `Services/`, `ClientApp/` (Angular) + `ClientApp-React/`. `Program.cs` wires everything. |
| `src/Shared` | `Services`/`ServiceNames` constants shared between AppHost and Web (Aspire resource names). |
| `src/AppHost` | **Aspire** distributed-app host (`Program.cs` + `Extensions.cs`). |
| `src/ServiceDefaults` | Aspire shared telemetry/health/resilience defaults. |

The Aspire AppHost + ServiceDefaults + Shared triad is new; the classic template had only Domain/Application/Infrastructure/Web.

## 2. Feature organization within layers — "vertical slice within Clean"

**Yes, within `Application` it is use-case-folder VSA.** Structure is `Application/{Aggregate}/{Commands|Queries}/{UseCase}/`:

```
Application/TodoItems/Commands/CreateTodoItem/
    CreateTodoItem.cs                  (Command record + Handler in ONE file)
    CreateTodoItemCommandValidator.cs
Application/TodoLists/Queries/GetTodos/
    GetTodos.cs  TodosVm.cs  TodoListDto.cs  TodoItemDto.cs
```

Command/record + `IRequestHandler` live in the same file (`CreateTodoItem.cs`). It is **slice-within-a-layer, not full vertical slice** — a single feature is still smeared across four projects: entity + domain event in Domain, command/handler/validator/DTO in Application, EF config in Infrastructure, endpoint in Web. Locality of behaviour is per-layer, not per-feature.

## 3. Feature flow end to end

- **Endpoints: Minimal APIs** (no controllers). Custom convention: `Endpoints/TodoItems.cs` implements `IEndpointGroup` (`src/Web/Infrastructure/IEndpointGroup.cs`) with `static abstract void Map(RouteGroupBuilder)`. `WebApplicationExtensions.MapEndpoints` reflection-scans exported types for `IEndpointGroup`, maps each as `/api/{ClassName}` with a matching OpenAPI tag. `EndpointRouteBuilderExtensions` derives the endpoint name (→ `operationId`, used for NSwag client gen) from the **handler method name**, and calls `Guard.Against.AnonymousMethod(handler)` to force named handlers. Handlers are thin: `ISender sender` + command → `sender.Send` → `TypedResults`.
- **MediatR 14.1.0** (real MediatR — now commercial-licensed) via `AddMediatR` scanning the Application assembly.
- **Pipeline** (registered in `Application/DependencyInjection.cs`, in order): `LoggingBehaviour` (open request *pre-processor*), then behaviours `UnhandledExceptionBehaviour` → `AuthorizationBehaviour` → `ValidationBehaviour` → `PerformanceBehaviour`.
- **Validation**: `ValidationBehaviour` runs all `IValidator<TRequest>` (FluentValidation, `AddValidatorsFromAssembly`), aggregates failures, throws a custom `ValidationException`. Surfaced as 400 by a global exception handler; every operation advertises 400 via `ApiExceptionOperationTransformer`.
- **Authorization**: two-tier. (a) Endpoint groups call `groupBuilder.RequireAuthorization()`. (b) A **custom `[Authorize(Roles=, Policy=)]` attribute** on request records, reflected by `AuthorizationBehaviour` against `IUser`/`IIdentityService`, throwing `UnauthorizedAccessException`/`ForbiddenAccessException`. Identity is ASP.NET Core Identity via `MapIdentityApi` (bearer tokens); `Users.cs` endpoint + `IdentityApiOperationTransformer` document it.

## 4. Persistence

- **EF Core 10**. `ApplicationDbContext : IdentityDbContext<ApplicationUser>` implements `IApplicationDbContext` (the Application-layer port exposing `DbSet`s — note this leaks EF into Application, a debated pattern). Configs via `IEntityTypeConfiguration` in `Data/Configurations/` (`ApplyConfigurationsFromAssembly`).
- **Two SaveChanges interceptors** (`Data/Interceptors/`):
  - `AuditableEntityInterceptor` — auto-stamps `Created/CreatedBy/LastModified/LastModifiedBy` on `BaseAuditableEntity` (uses `IUser` + `TimeProvider`; even handles owned-entity changes).
  - `DispatchDomainEventsInterceptor` — collects `BaseEntity.DomainEvents`, clears them, publishes each via `IMediator.Publish` after save. Domain events raised in the entity itself (e.g. `TodoItem.Done` setter → `AddDomainEvent(new TodoItemCompletedEvent(this))`).
- **Migrations** applied at startup in Development via `ApplicationDbContextInitialiser` (`InitialiseDatabaseAsync` + seed).
- DB engine is a **template choice** (`Database`: sqlite [default] / sqlserver / postgresql), wired both in EF and in the Aspire AppHost (`AddSqlite` / `AddAzureSqlServer` / `AddAzurePostgresFlexibleServer`, `.RunAsContainer` persistent lifetime).

## 5. Frontend options

Three, via the `ClientFramework` choice symbol: **Angular** (default), **React**, **None** (Web-API-only). Both SPAs ship as sibling folders `src/Web/ClientApp` (Angular) and `src/Web/ClientApp-React`. `template.json` `modifiers` exclude the unused one and rename `ClientApp-React`→`ClientApp` for React; API-only strips both plus `Web.AcceptanceTests` and redirects `/` → Scalar. In **run mode** the AppHost launches the SPA via Aspire's `AddJavaScriptApp(Services.WebFrontend, "./../Web/ClientApp").WithRunScript("start")`; Web serves it via `UseFileServer` + `MapFallbackToFile("index.html")`.

## 6. Dependency rules & enforcement

Enforced **only by project references** (Domain→nothing, Application→Domain, Infrastructure/Web→Application). **There are no architecture tests** — no NetArchTest/ArchUnit anywhere in `tests/` (grep-confirmed). Earlier community forks had NetArchTest; this template does not. So conventions like "handlers stay thin", "no authz in handlers", "Application doesn't reference Infrastructure types" are unguarded at build time. This is a real gap versus TimeWarp's compiler-checked TWA rules.

## 7. .NET / template mechanics / CI

- **.NET 10**, C# latest, `ImplicitUsings`, `Nullable`, CPM (`Directory.Packages.props`), `TreatWarningsAsErrors=true` (`Directory.Build.props`).
- Notable pins: MediatR 14.1.0, AutoMapper 16.1.1 (**both now commercially licensed**), FluentValidation 12, Ardalis.GuardClauses 5, Scalar.AspNetCore 2.13 (OpenAPI UI, replacing Swashbuckle), Aspire 13.2.2, NUnit 4.5 + Shouldly 4.3 + Moq + Respawn 7, Reqnroll 3.3 (SpecFlow successor) + Playwright 1.59.
- **Template engine**: `.template.config/template.json`, `sourceName: CleanArchitecture`, `shortName: ca-sln`. Uses `port` generators for Kestrel + all Aspire ports, `coalesce`/`constant` generators, computed symbols (`UseAngular`, `UseSqlite`, …), preprocessor `<!--#if-->` / `#if` regions, and per-condition source `modifiers`/`rename`. Packaged via `CleanArchitecture.nuspec` (`build/` folder).
- **Tests**: `Application.UnitTests`, `Domain.UnitTests`, `Infrastructure.IntegrationTests`, `Application.FunctionalTests` (Aspire.Hosting.Testing + `TestAppHost` + Respawn/Testcontainers reset per DB), `Web.AcceptanceTests` (Reqnroll BDD + Playwright, feature/step/page folders).
- **CI** (`.github/workflows/`): `build.yml`, `release.yml`, `test-templates.yml` (installs the packed template and generates across option combos), `codeql.yml`. Renovate for deps.

## 8. Patterns worth stealing / criticisms

**Worth stealing (with file evidence):**
- **`IEndpointGroup` + reflection `MapEndpoints`** (`src/Web/Infrastructure/IEndpointGroup.cs`, `WebApplicationExtensions.cs`) — convention-driven minimal-API grouping; route = `/api/{ClassName}`, `operationId` = handler method name. TimeWarp already goes further (source-generated FastEndpoints from contracts), but the `Guard.Against.AnonymousMethod` trick to guarantee stable operationIds for client-gen is a nice touch.
- **Two SaveChanges interceptors** for auditing + domain-event dispatch (`Data/Interceptors/`) — keeps cross-cutting persistence concerns out of handlers entirely. Directly comparable to TimeWarp's "prefer generators/interceptors over convention-by-memory".
- **Shared `Services`/`ServiceNames` constants** for Aspire resource names (`src/Shared`) — same idea TWA0007 enforces; JT does it by convention, not analyzer.
- **OpenAPI operation transformers** (`ApiExceptionOperationTransformer`, `BearerSecuritySchemeTransformer`, `IdentityApiOperationTransformer`) that inject 400/401/403 and security schemes based on pipeline knowledge — declarative doc accuracy.

**Known criticisms / weaknesses:**
- **Layer ceremony vs slice locality**: one feature spans 4–5 projects; adding a field to `TodoItem` touches Domain entity, Application command/DTO/validator, Infrastructure config, Web endpoint. Standard VSA-vs-Clean critique, and it applies here — the "vertical slice" is only within Application.
- **No enforcement**: dependency direction and handler conventions are unguarded (no arch tests) — regressions compile fine.
- **`IApplicationDbContext` leaks EF `DbSet` into Application** — a long-debated abstraction that isn't really an abstraction.
- **Duplicated authorization** — both endpoint `RequireAuthorization()` and the `[Authorize]` MediatR behaviour, two mechanisms to keep in sync.
- **Commercial-license exposure**: template pins MediatR 14 and AutoMapper 16, both now paid for commercial use — a live concern for anyone adopting it as a starting point (directly relevant to TimeWarp's own MediatR→TimeWarp.Mediator and no-AutoMapper stances).

---

**Executive summary (5 lines):**
1. Current JT template is a **.NET 10 + Aspire** rewrite: Domain/Application/Infrastructure/Web plus AppHost, ServiceDefaults, and a Shared constants project.
2. Features are **use-case-folder slices within the Application layer** (Command+Handler in one file, sibling Validator/DTOs) — slice-within-Clean, not true vertical slice; a feature still spans 4–5 projects.
3. Flow is **Minimal APIs via a reflection-discovered `IEndpointGroup` convention → MediatR pipeline** (Logging/UnhandledException/Authorization/Validation/Performance) with FluentValidation and a custom `[Authorize]` behaviour; ASP.NET Core Identity + Scalar for docs.
4. Persistence is **EF Core 10 with two SaveChanges interceptors** (auditing + domain-event dispatch); DB engine and frontend (Angular/React/None) are `dotnet new` choice symbols wired through both EF and the Aspire AppHost.
5. Biggest gaps vs TimeWarp: **no architecture tests** (dependency rules rest on project refs alone) and reliance on now-commercial MediatR/AutoMapper — while its interceptor pattern, endpoint-group convention, and Aspire resource-name constants are the pieces most worth borrowing.

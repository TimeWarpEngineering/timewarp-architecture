# Structural Survey: Three ardalis .NET Repos

## Repo A — `modulith` (the `dotnet new` modular-monolith template)

Template generator, not an app. Real content lives under `working/content/modulith/` with `.template.config/template.json`. Scaffolds a solution + modules on demand (`dotnet new modulith --add solution|basic-module|ddd-module`, optional `--WithUi`).

**1. Per-module structure.** Two archetypes:
- **basic-module** (`NewModule/`): single project with endpoint + service in the root, plus optional sibling projects `.Contracts`, `.HttpModels`, `.UI` (Blazor), `.Tests`.
- **ddd-module** (`DddModule/`): clean-ish layers *as folders inside one project* — `Api/`, `Domain/`, `Infrastructure/`. Not layers-as-projects.

**2. Boundary mechanics.** Modules discovered by **reflection at startup**, not wired explicitly. Each module implements a `static abstract` `IRegisterModuleServices.ConfigureServices` (`Shared/Modulith.SharedKernel/IRegisterModuleServices.cs`, C# 11 static-abstract interface); `Modulith.Web/ModuleRegistrationExtensions.cs` scans solution assemblies and invokes it. Blazor UI modules discovered separately via `IBlazorAssemblyDiscoveryService` + `AddAdditionalAssemblies`. Quirk: `Modulith.DddModule.Contracts.csproj` references the module project (reverse direction) — contracts isolation is nominal only.

**3. Endpoint/CQRS/validation.** FastEndpoints, REPR, `internal` endpoints (`Api/WeatherForecastEndpoint.cs` : `EndpointWithoutRequest<T[]>`). `Mediator.Abstractions` (martinothamar) referenced but barely used — body is a weather-forecast stub, no handlers. Template description says "MediatR" but csproj pulls `Mediator`.

**4. Persistence.** None in the template body — fake services (`FakeTemperatureService`). No DbContext, no per-module persistence story.

**5. Stack.** Central package management, `.slnx` solution format, FastEndpoints, Blazor (Server + WASM) behind `WithUi`, Aspire-oriented. Uses `#if (WithUi)` template conditionals in source.

---

## Repo B — `RiverBooks` (course sample, modular monolith)

Real runnable app. Modules `Books`, `Users`, `OrderProcessing`, `EmailSending`, `Reporting`, each with a paired `.Contracts` project, over a `SharedKernel`, hosted by one `Web` project + Aspire AppHost.

**1. Per-module structure.** **One project per module, vertical-slice inside with DDD folders.** `RiverBooks.Users/` has `Domain/`, `Data/` (+`Migrations/`), `Interfaces/`, `Integrations/`, `UseCases/` (CQRS: `UseCases/Cart/AddItem/{Command,Validator,Handler}`), `{X}Endpoints/`. Use cases grouped per feature, not Commands/Queries folders.

**2. Boundary mechanics — the strongest of the three.**
- **Contracts-only references.** `RiverBooks.Users.csproj` references *only* other modules' `.Contracts` projects, never implementations.
- **Compiler-enforced internal layering.** Each module has a `config.nsdepcop` with `IssueKind Severity="Error"` and `<WarningsAsErrors>NSDEPCOP01`, forbidding e.g. `Users.Domain.* → Users.Data.*` and `Users.UseCases.* → Users.Data.*`. Build-breaking namespace-dependency analyzer — the closest analogue to our Roslyn slice-isolation rule.
- **NetArchTest** arch tests (`RiverBooks.OrderProcessingTests/Arch/InfrastructureDependencyTests.cs`) as a second net.
- Cross-module comms have **two shapes**: (a) synchronous request/response via Mediator `IRequest<Result<T>>` contracts (`Users.Contracts/UserDetailsByIdQuery.cs`); (b) async `IntegrationEventBase` notifications published via Mediator (`NewUserAddressAddedIntegrationEvent`). Internal **domain** events (`AddressAddedEvent`) are translated into **integration** events by a handler in `Integrations/` — a clean domain-event→integration-event bridge.

**3. Endpoint/CQRS/validation.** FastEndpoints, `internal sealed` endpoints that only translate HTTP→command and `Send` to Mediator (`UserEndpoints/AddAddress.cs`). Mediator (martinothamar) source-gen, `AddMediatorFluentValidationBehavior()` + `AddValidatorsFromAssemblyContaining`. Ardalis.Result for outcomes, FastEndpoints.Security for JWT.

**4. Persistence — DbContext + schema per module.** `UsersDbContext` with `modelBuilder.HasDefaultSchema("Users")`; connection string per module (`UsersConnectionString`); migrations inside each module's `Data/Migrations/`. `SaveChangesAsync` override dispatches domain events. Each module owns its schema in a shared DB.

**5. Stack.** .NET 10, EF Core 10.0.8, FastEndpoints 8.1, Mediator.Abstractions 3.0.2, Ardalis.Result/GuardClauses, Serilog, Aspire, NsDepCop 2.7. `<Module>Users</Module>` MSBuild metadata tags each project.

---

## Repo C — `VerticalCleanModularMicroservices` (the synthesis / conference talk)

Five sibling solutions showing **the same order-demo domain** at escalating sophistication. `01`→`04` are the talk's named stages; `05-CleanVertical` is an extra "combine everything" experiment.

**Stage 01 — Vertical Slice (1 project).** `OrderDemo.Api/` with `CartFeature/`, `OrderFeature/`, `ProductFeature/`. Each slice is **one self-contained file**: `CartFeature/AddToCart.cs` holds the minimal-API `MapPost`, request record, and DTO together, talking straight to `AppDbContext`. No mediator, no repository — max locality. Single `AppDbContext`.

**Stage 02 — Clean Architecture (4 projects).** Classic `Core`/`UseCases`/`Infrastructure`/`Web` with full test pyramid (Unit/Integration/Functional/Aspire).

**Stage 03 — Modular Monolith (RiverBooks-lite).** `Nimble.Modulith.{Customers,Products,Reporting,Users,Email}` + `.Contracts` each, one `Web` host.
- Module internals: `Endpoints/` (Create/Update/Delete/GetById/List one file each), `Models/`, `Data/` (`ProductsDbContext`+Factory+Config), `UseCases/Queries/`.
- Comms: cross-module via Mediator **contracts** — `Products.Contracts/GetProductPriceQuery : IQuery<decimal>`, handled internally against the module's own DbContext.
- Persistence: **DbContext + Aspire-registered DB per module** (`builder.AddSqlServerDbContext<ProductsDbContext>("productsdb")`), each self-migrates (`EnsureProductsModuleDatabaseAsync`).
- Isolation by ProjectReference-to-Contracts convention only — **no NsDepCop/analyzer** (weaker than RiverBooks).

**Stage 04 — Microservices.** Same modules, but `Email` promoted to its own deployable (`Nimble.Modulith.Email.Web`), a `SharedInfrastructure` project appears, comms go **over RabbitMQ**. `SharedInfrastructure/Messaging/EmailCommandPublisherBehavior.cs` is a Mediator pipeline behavior that publishes commands onto the bus instead of handling in-process — same contract, different transport. AppHost wires `AddRabbitMQ` + a DB per service.

**Stage 05 — CleanVertical (the actual "synthesis").** A fork of the **Ardalis.CleanArchitecture template** (README verbatim). Strongly-typed IDs via **Vogen** (`CartId.From(...)`), **Ardalis.Specification** repositories (`ProductByIdSpec`), Ardalis.Result, FastEndpoints with typed `Results<Ok<T>,NotFound,...>` + a `Mapper`. The experiment: keep Clean's `Core`/`UseCases`/`Infrastructure`, but **reorganize the Web project into vertical feature slices** — `Web/CartFeatures/AddToCart/{Endpoint,Handler,...}`. Notably the domain is being pulled *into* the Web project (`OrderDemo.CleanVertical.Web.Domain.CartAggregate`) and the command/handler is **duplicated** between `UseCases/Cart/AddToCart` and `Web/CartFeatures/AddToCart` with divergent namespaces (`Core.CartAggregate.Cart` vs `Web.Domain.CartAggregate`). This stage is **transitional/half-refactored**, not a polished conclusion.

**Stack (all stages).** .NET 10, EF Core 10, martinothamar **Mediator** (source-gen), FastEndpoints/minimal APIs, Aspire, SQL Server, RabbitMQ (04), Vogen + Ardalis.Specification/Result (05). This repo standardized on **Mediator, having abandoned MediatR**.

---

## Comparison — how ardalis's thinking evolved

**Doubled down on (all three):**
- **FastEndpoints + REPR** everywhere, replacing controllers (CleanArch README says controllers/Razor dropped at template v9).
- **Mediator (martinothamar), not MediatR** — deliberate migration across all repos (licensing-driven).
- **A `.Contracts` project per module** as the public seam (Mediator request/response records + integration events).
- **Vertical slices as the feature unit** — grouped by capability, one folder per use case, no Commands/Queries ceremony.
- **Aspire** as orchestration/local-dev substrate.

**Abandoned / moved away from:**
- **Layers-as-projects for a single bounded context** — VCMM 01 collapses Clean's 4 projects into 1; 05 folds Core/Infrastructure back into vertically-sliced Web. The 4-project split is now one point on a spectrum, not the default.
- **MediatR.**
- **A bundled SharedKernel project** — extracted to the `Ardalis.SharedKernel` NuGet package.
- **Controllers / Razor Pages / ApiEndpoints** — removed from the CleanArch template.

**What the synthesis in Repo C concludes.** The thesis is **evolutionary, not prescriptive**: the *same domain and business logic* slides along a spectrum — slice → clean → module → service — adding structure only as coupling/scale demands. The through-line is **the mediator contract as the stable seam**: identical from in-process modular monolith to microservices; only the transport changes (in-process handler vs RabbitMQ publisher behavior). Repo C does **not** crown one architecture. The `05-CleanVertical` fork is explicitly experimental and currently half-refactored (duplicated handlers, mixed namespaces), so it encodes an *aspiration* (Clean's testable domain + Vertical's locality) more than a finished recommendation. **RiverBooks (Repo B), not Repo C, is his most mature, enforcement-backed expression** of the modular-monolith position.

---

## 5 most stealable patterns for our template

(We already out-automate most of his *conventions* via compiler-enforced slice isolation + endpoint-contract generators, so these are the genuinely additive steals.)

1. **Domain-event → integration-event bridge in an `Integrations/` folder.** RiverBooks keeps domain events private and translates them into public integration-event contracts in a dedicated handler (`RiverBooks.Users/Integrations/UserAddressIntegrationEventDispatcherHandler.cs` + `Users.Contracts/NewUserAddressAddedIntegrationEvent.cs`). An async cross-slice channel that never leaks internal domain types — exactly the "two things must agree" seam our generators could emit and TWA-check.

2. **Transport-agnostic mediator seam via a pipeline behavior.** VCMM 04's `SharedInfrastructure/Messaging/EmailCommandPublisherBehavior.cs` swaps in-process handling for bus-publish *without changing the contract*. A `[BusPublished]`-marked contract routed to a queue by a behavior would let a single-host template grow out-of-process without touching call sites — companion to our `[ApiEndpoint]` fail-closed generator.

3. **DbContext-plus-schema-per-slice with self-migration.** RiverBooks (`UsersDbContext` + `HasDefaultSchema("Users")`, per-module connection string) and VCMM 03 (`AddSqlServerDbContext<ProductsDbContext>("productsdb")` + `EnsureProductsModuleDatabaseAsync`) give each slice its own EF schema/DB + self-migration hook. Behind our `postgres` flag this is a strong default and analyzer-checkable (slice X's DbContext maps only slice X's aggregates).

4. **Reflection-free-at-runtime module registration via a `static abstract` interface.** Modulith's `IRegisterModuleServices` (`ConfigureServices` as `static abstract`) is a clean per-slice DI entry point. Steal the *shape*: a `static abstract` registration contract per slice that a source generator enumerates at build time (vs Modulith's startup assembly scan) — same ergonomics, zero runtime reflection.

5. **NsDepCop as validation of our own approach + `<Module>`/schema MSBuild metadata.** RiverBooks' `config.nsdepcop` (`NSDEPCOP01` as error) enforces *intra*-slice layering (Domain ⊄ Data). Our analyzer covers *inter*-slice; the steal is extending it to intra-slice layer directionality, plus tagging each project with `<Module>Name</Module>` metadata so tooling/generators key off slice identity declaratively.

---

**Executive summary (5 lines):**
1. All three converge on **FastEndpoints + REPR, martinothamar Mediator (MediatR abandoned), per-slice `.Contracts` projects, vertical-slice feature folders, and Aspire** — ardalis's current baseline.
2. **RiverBooks is the mature reference**: one project per module, DDD folders inside, contracts-only references, DbContext+schema per module, domain→integration-event bridging — with **NsDepCop compiler-enforced internal layering** (his analogue to our slice isolation).
3. **VCMM is a spectrum, not a verdict**: the same domain slides slice→clean→module→microservice, with the **mediator contract as the invariant seam** and only the transport changing.
4. The `05-CleanVertical` "combine everything" fork is **experimental and half-refactored** (duplicated handlers, mixed namespaces) — an aspiration toward Clean's testable domain + Vertical's locality, not a shipped recommendation.
5. For our template the additive steals are the **domain→integration-event bridge, transport-swapping pipeline behavior, DbContext-schema-per-slice self-migration, `static abstract` per-slice registration contract, and intra-slice layer-direction enforcement** — the rest of his conventions our analyzers/generators already subsume.

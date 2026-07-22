# Trinsic Repository — Structural Architecture Survey

Repo: `/home/steve/worktrees/github.com/TimeWarpEngineering/trinsic/master` (single commit, "Original from AzureDevOps" — an exported snapshot, ~2020-2021 era). Steven Cramer's Trinsic (formerly Streetcred) self-sovereign-identity SaaS platform. Clear ancestor of the timewarp-architecture template.

## 1. Solution / project structure

A **multi-solution mono-repo** (11 `.sln` files, each a semi-independent subsystem), not one unified solution. `aries-framework-dotnet` is a **git submodule** (`.gitmodules` → hyperledger/aries-framework-dotnet), Steven's own fork containing the pattern-defining sample. Major projects:

- `Trinsic.Studio/Source/{Client,Server,Api}` — the flagship: a Blazor WASM hosted app. **Client** = WASM SPA, **Server** = ASP.NET host + backend handlers, **Api** = shared request/response **contracts** library referenced by both.
- `Trinsic.Core` — EF Core persistence: `Data/TrinsicDbContext.cs` (+ `TrinsicReadOnlyDbContext`), `Entities/` (18 entities: `Tenant`, `TenantManagementToken`, `ProductPlan`…), `Migrations/`.
- `Trinsic.Coreapp` — cross-cutting host wiring (configuration, middleware, services).
- `Trinsic.Common` — constants, enums, exceptions, feature flags.
- `Trinsic.Components` — shared Blazor component library (Buttons, Modals, Table, Icons, HeroIcons…).
- `Trinsic.Api.{Credentials,Provider,Wallet}` — standalone public REST API microservices (the actual SSI product surface).
- `IdentityServer/`, `MediatorAgent/`, `VC_Auth/`, `UrlService/`, `Infrastructure/` (Aries/Okapi wrappers), `ServiceClients/` (generated NSwag clients).
- `Brands/{Trinsic,NatWest}` — white-label branding projects conditionally referenced by MSBuild `$(Brand)`.
- `Spikes/`, `Demos/`, `Zapier/`, `load-testing/`, `Tools/` — experiments and tooling.
- Tests scattered per-subsystem: `*.Integration.Tests`, `EndToEnd.TestCafe.Tests`, plus legacy `Source/V1/Streetcred.*.Tests`.

## 2. Architecture style

**Vertical-slice feature organization inside a modular monolith**, with a three-project contract-seam split (Client / Server / Api-contracts). It is *not* clean-architecture-by-layer in Studio — features are the primary axis. Every layer mirrors the same `Features/<Area>/<Operation>/` tree:

- `Api/Features/ProviderKey/GetProviderKeys/` → `Request`, `Response`, `RequestValidator`
- `Server/Features/ProviderKey/GetProviderKeys/` → `Endpoint`, `Handler`
- `Client/Features/ProviderKey/Actions/Fetch/` → `Action`, `Handler`

Sub-features nest (e.g. `Client/Features/Organization/Features/Connection/…`) — a hand-rolled precursor to today's slice-isolation rules. A shared kernel (`Trinsic.Core`, `Trinsic.Common`, `Trinsic.Components`) sits beneath the slices. Note: folders and namespaces are **PascalCase** here (`Features/ProviderKey`), and the seam project is literally named `Api` — kebab-case paths and the `*-contracts`/`foundation` naming came later.

## 3. End-to-end flow of one use-case

Using `GetProviderKeys` as the worked example:

1. **Contract** (`Api/Features/ProviderKey/GetProviderKeys/GetProviderKeysRequest.cs`): `GetProviderKeysRequest : BaseApiRequest, IRequest<GetProviderKeysResponse>`. Carries `public const string RouteTemplate = "api/Providers/GetProviderKeys"` and an override `string GetRoute()` that hand-builds the URL. `[FeatureFlag(FeatureFlags.ProviderKeys)]` gates it. The **route lives on the contract**, shared by client and server — the direct ancestor of today's `[ApiRoute]`.
2. **Validation** (`…RequestValidator.cs`): FluentValidation `AbstractValidator<GetProviderKeysRequest>`. Notably includes a `Must(IsValidRoute)` rule asserting `GetRoute()` equals the expected template — a runtime self-check that two things agree.
3. **Server endpoint** (`Server/…/GetProviderKeysEndpoint.cs`): `GetProviderKeysEndpoint : BaseEndpoint<TRequest,TResponse>` — a **hand-written MVC `ControllerBase` shim** with `[HttpGet(RouteTemplate)]` + Swagger/`ProducesResponseType` attributes; its body is just `Send(request)`, which the base resolves `IMediator` from `HttpContext` and calls `Mediator.Send`.
4. **Server handler** (`…/GetProviderKeysHandler.cs`): `IRequestHandler<Request,Response>` (MediatR). Injects `TrinsicDbContext`, AutoMapper `IConfigurationProvider`, `ICurrentUserService`; queries EF, `ProjectTo<ProviderKey>` via AutoMapper, returns the response. No re-validation in the handler.
5. **Client action** (`Client/Features/ProviderKey/Actions/Fetch/FetchProviderKeysAction.cs`): a `[TrackProcessing]`-attributed `BaseAction` nested in `partial class ProviderKeyState`.
6. **Client handler** (`…/FetchProviderKeysHandler.cs`): `BaseActionHandler<FetchProviderKeysAction>` (Blazor-State + MediatR). Builds the request, calls `HttpClient.GetFromJsonAsync<GetProviderKeysResponse>(request.GetRoute())`, mutates `ProviderKeyState._ProviderKeys`. The **client reuses the exact contract type and its `GetRoute()`** to call the server — the seam is closed by shared types, not codegen.

`BaseRequest` gives every message a `CorrelationId` (Guid); `BaseResponse` echoes it. There is **no `OneOf`/`ProblemDetails`** — errors flow through exceptions/HTTP status.

## 4. Persistence

EF Core 3.1 on **SQL Server** (`Microsoft.EntityFrameworkCore.SqlServer`). `Trinsic.Core/Data/TrinsicDbContext.cs` plus a separate `TrinsicReadOnlyDbContext` (CQRS-ish read/write split at the context level). 18 anemic entity classes under `Entities/` (`Tenant`, `TenantManagementToken`, `TenantWebhook`, `ProductPlan`, `LedgerMonthlyTransaction`…) — **no aggregate roots, no invariants, no domain methods**; they're EF POCOs. Migrations checked in under `Migrations/`. Handlers query the `DbContext` directly and use **AutoMapper `ProjectTo`** to map entities → response DTOs. (The separate SSI product APIs additionally use Azure Table/Cosmos storage and the Aries agent framework.)

## 5. Frontend

**Blazor WebAssembly, hosted model** (`Client` is `OutputType=Exe`, `Microsoft.AspNetCore.Components.WebAssembly`). State management is **Blazor-State** (`Blazor-State` package + `BlazorComponentUtilities`, `Blazored.LocalStorage`, Redux DevTools via `ReduxDevToolsEnabled` constant) — the OSS predecessor of TimeWarp.State, same Action/Handler/partial-State idiom. Form validation via **`PeterLeslieMorris.Blazor.FluentValidation`**. Styling is **Bootstrap CSS** (the aries sample bundles `wwwroot/css/bootstrap` + open-iconic). Extras: `Stateless` state machines (`Machines/FetchStateMachine.cs`), `QRCoder`, `SixLabors.ImageSharp`, `Polly`, JS interop (`Intercom.js`). E2E tests use **TestCafe**.

## 6. Dependency rules & enforcement

Enforcement exists but is **runtime reflection, not compile-time analyzers**. `Client/Analyzer/ProjectAnalyzer.cs` + `PageAnalyzer.cs` run at app **Startup** (`Client/Startup.cs` invokes it): they reflect over every `[Route]` page and `throw` if a page lacks a `public static GetRoute()` or if its `RouteTemplate` const doesn't match the `[Route]` attribute. This is the philosophical seed of "when two things must agree, add a check" — but it fires at boot, not build. Slice boundaries are enforced only by folder convention and project references; brand isolation via conditional `ProjectReference` on `$(Brand)`. A `TimeWarp.ruleset` + `Microsoft.CodeAnalysis.NetAnalyzers`/FxCop provide generic static analysis. Per-file "context" is primitive hashtag comments (e.g. `// #Provider #GetProviderKeysEndpoint #Request #Api`) — the embryo of today's `#region Purpose` agent-context regions.

## 7. .NET / C# version & package currency

Heavily **dated (2020–2021)**: `global.json` pins SDK **3.1.404**; libraries target `netstandard2.1`, apps/tests `netcoreapp3.1`; **C# `LangVersion` 8.0**; `RazorLangVersion 3.0`. Notable now-superseded packages: **MediatR** (→ TimeWarp.Mediator), **Blazor-State** (→ TimeWarp.State), **AutoMapper** + `AutoMapper.Extensions`, **Dawn.Guard** (→ Guard pattern), **FluentValidation** + `FluentValidation.AspNetCore`, **PeterLeslieMorris.Blazor.FluentValidation** (→ Blazilla), **Scrutor** (assembly-scan DI), **Swashbuckle** (Swagger), EF Core 3.1, Azure AD B2C auth, Application Insights, Stripe.net. Versions are centralized as MSBuild properties in a 12KB `Directory.Build.props` (a pre-Central-Package-Management convention).

## 8. Patterns that evolved into the current template

**Kept / evolved:**
- **Endpoint-centric contracts** — `Request`/`Response`/`Validator` co-located per operation, route as a `const` on the contract, contract shared client↔server → became `[ApiRoute]` + generated route members + `web-contracts`.
- **Blazor-State Actions/Handlers/partial-State** → **TimeWarp.State** (near-identical idiom, incl. `[TrackProcessing]`).
- **MediatR `IRequest<T>` + `IRequestHandler`** → **TimeWarp.Mediator**.
- **FluentValidation in the pipeline, not in handlers** → kept (`FluentValidationBehavior`).
- **Three-way Client/Server/Api(contracts) split** → became web-spa / web-server / web-contracts.
- **Feature-slice folder tree mirrored across layers** → kept and formalized (SliceRoot, TWA0009).
- **Blazor form validation via a FluentValidation adapter** (PeterLeslieMorris) → **Blazilla**.
- **The "make two things agree via a check" instinct** (PageAnalyzer, `IsValidRoute`) → matured into **compile-time Roslyn analyzers/source generators (TWA0001–0014)**.
- **Per-file tagged comments** → **`#region Purpose`/agent-context regions**.
- Host-free serialization round-trip tests (`Client.Integration.Tests/Serialization`) → **web-contracts-tests**.

**Abandoned / replaced:**
- **Hand-written `BaseEndpoint<T>` MVC controller shims** → replaced by **FastEndpoints generated from contracts** (AGENTS.md explicitly: "No hand-written BaseEndpoint shims").
- **`BaseResponse` + `CorrelationId` + exception-based errors** → replaced by **`OneOf<Response, SharedProblemDetails>`**.
- **AutoMapper `ProjectTo`** → dropped (no AutoMapper in the template).
- **Runtime reflection enforcement** → replaced by **build-time analyzers**.
- **PascalCase folders + `Directory.Build.props` version soup** → **kebab-case paths + Central Package Management**.
- **Bootstrap CSS** → **FluentUI v5 + plain-CSS design tokens** (no Bootstrap, no Tailwind).
- **Multi-`.sln`, git-submodule, brand-conditional monolith** → single-solution **.NET Aspire-orchestrated** template.
- **`Dawn.Guard`, `Stateless`, `Scrutor`, Swashbuckle, Azure B2C** — not carried into the template's core.

## 9. Weaknesses / dated aspects

- Stuck on **.NET Core 3.1 / C# 8** — long out of support; nullable reference types only partially adopted (Studio contracts use `string Name { get; set; }` with no `?`, aries uses `= null!`).
- **Enforcement is runtime**: a route mismatch throws at app startup, not at build — slow feedback and shippable-broken.
- **Boilerplate-heavy**: every operation needs a hand-written endpoint shim, an explicit `GetRoute()` string-concatenation (duplicated in a validator `IsValidRoute`), and manually-kept-in-sync contract/handler pairs — exactly the drudgery the current template's source generators eliminate.
- **Anemic domain**: entities are EF POCOs with no invariants; business rules live in handlers.
- **Manual URL building** via string interpolation/`Replace` (`GetConnectionRequest.GetRoute()`) is error-prone.
- **AutoMapper `ProjectTo`** couples handlers to a mapping-config side-channel (runtime-validated at best).
- **Sprawl**: 11 solutions, a live git submodule, `V1` legacy `Streetcred.*` test projects, `Spikes/`, `junk.json`, brand-conditional references, and multiple auth stacks (B2C, IdentityServer, VC_Auth) make the repo hard to build coherently — the antithesis of the single-command `dev build` the template now targets.

---

## Executive summary

Trinsic (Streetcred) is a ~2020, .NET Core 3.1 / C# 8 Blazor-WASM-hosted SSI SaaS platform, structured as a multi-solution modular monolith whose flagship `Trinsic.Studio` already embodies the core DNA of today's timewarp-architecture template: vertical feature slices mirrored across a Client / Server / Api-contracts three-way split, endpoint-centric shared contracts carrying their own route, MediatR + Blazor-State + FluentValidation, and a "make two things agree" enforcement instinct. The lineage is unmistakable — Blazor-State→TimeWarp.State, MediatR→TimeWarp.Mediator, PeterLeslieMorris→Blazilla, tagged comments→Purpose regions, contract-route-consts→`[ApiRoute]`. What the template deliberately abandoned is equally telling: hand-written `BaseEndpoint` MVC shims (→ generated FastEndpoints), `BaseResponse`/`CorrelationId`/exception errors (→ `OneOf<Response, ProblemDetails>`), AutoMapper, Bootstrap (→ FluentUI+CSS tokens), PascalCase paths (→ kebab-case), and above all **runtime reflection enforcement (→ compile-time Roslyn analyzers TWA0001–0014)**. The through-line of Steven's evolution is moving convention-by-memory and boot-time checks into the compiler, and replacing hand-written glue with source generation.

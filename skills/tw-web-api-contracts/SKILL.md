---
name: tw-web-api-contracts
description: "**TIMEWARP SKILL** — endpoint-centric Web.Contracts API contracts (Command, Query, ApiRoute, Operation, I*Details, Validator, serialization tests), one contract bound to HTTP, CLI, and MCP. Invoke before scaffolding or fixing contracts. WHEN: Add a CreateTodoItem command contract, Scaffold a GetRole query with ApiRoute and IRoleDetails for the edit form, Add a serialization round-trip test for my Command."
when-to-use: Web.Contracts, web-contracts, command contract, query contract, ApiRoute, ApiEndpoint, Operation attribute, TimeWarp.Contracts, IQuery, ICommand, IIdempotentCommand, CLI or MCP binding, tool hint, Destructive, OpenWorld, Operation title, EndpointAuthorize, EndpointAllowAnonymous, I*Details, EditForm binding, Validator, serialization round-trip, BFF, IAuthApiRequest, FastEndpoint generation
---

# Web API Contracts

Endpoint-centric, JSON-over-HTTP contracts designed for **Blazor front ends**. Each
endpoint owns its request/response types. **Mutability signals purpose**: immutable
members are read-only display data; mutable `I*Details` members make the `Update*` Command the
`EditForm` model with no view model. The store keeps the fetched Response, and a generated `From`
copies it ([edit forms](references/mutability.md#edit-forms-store-snapshot-and-working-copy)).
**Shared validation** lives on interfaces and is composed into per-endpoint validators.

This pattern appears across TimeWarp-based solutions. Project names vary (`web-contracts`,
`Web.Contracts`, `Api.Contracts`, …) but the contract shape is the same.

**Why endpoint-centric, not entity-centric:** each operation owns its request/response types
so endpoints do not couple through a shared entity DTO (over-fetch, under-fetch, and
cross-endpoint breakage). Share structure and validation through `I*Details` interfaces
composed into per-endpoint validators — that is what lets Blazor bind `EditForm` to the
Command through one shape without a parallel view model.

**Why generate FastEndpoints on both hosts:** route, verb, and auth live on the contract, so there are no
hand-written MVC `BaseEndpoint` shims. Validation stays on the mediator's `FluentValidationBehavior`. Hosts set
`EnableApiEndpointGeneration` plus an `ApiEndpointContractAssemblies` AssemblyName allow-list (TWE008).

### Browser-protocol endpoints (the exception)

A hand-written `EndpointWithoutRequest` at `features/<slice>/…-endpoint-server.cs` is allowed when
the HTTP response is not the contract's JSON: an auth challenge, a form POST answered with a
redirect, or an antiforgery token fetch. The living cases are `ChallengeEntraEndpoint`,
`SignOutBrowserSessionEndpoint`, and `SignOutAntiforgeryTokenEndpoint`.

- The path lives on a shared contracts class.
- Any session or state change dispatches the existing mediator handler. Do not write a second
  implementation.
- The Design region says why the generator does not fit.
- These bodies share nothing but the route and `AllowAnonymous`, so a generator is not worth it.
- Validation stays on `FluentValidationBehavior`. Do not adopt FastEndpoints' validator integration.

Ordinary JSON operations stay generated. The filename function `endpoint` is for this exception
(`tw-feature-placement`).

## Detection — find the pattern in the current repo

Activate when **any** signal matches:

| Signal | How to find it |
|--------|----------------|
| Contracts project | `*.csproj` named `*contracts*` (any casing); product contracts may live in a cohesive `web/features/` tree (axis-1) or a project-local `features/`/`Features/` tree |
| Contract file layout | `**/features/**/<use-case>/*-contracts.cs` (one folder per operation, contract beside its handler) or a shared `*-contracts.cs` at slice root for a file used by more than one operation (axis-1 grammar; also match legacy `*.cs` without the layer suffix). Search **case-insensitively** — repos use kebab `features/` or Pascal `Features/` |
| Contract shell | `public static partial class` + nested `Query`/`Command` + `[ApiRoute(...)]` |
| TimeWarp.Mediator return | `IQuery<…>` / `ICommand<…>` / `IIdempotentCommand<…>` over `OneOf<Response, SharedProblemDetails>` (bare `IRequest<…>` where the kind is unclear) |
| Neutral operation | `[Operation("…")]` (`TimeWarp.Contracts`) on the nested `Query`/`Command` |
| Shared validation | `I*Details` interface + `AbstractValidator<I*Details>` |

The mediator is **TimeWarp.Mediator** (not MediatR). `IQuery<T>`, `ICommand<T>`, and `IIdempotentCommand<T>`
all extend `IRequest<T>`, so handlers stay `IRequestHandler<TRequest, TResponse>`.

Before adding a contract, **read 2–3 existing contracts in the same repo** to match
namespace root, folder casing, test project layout, and mock-service registration.

## Folder and namespace rules

| Concern | Rule | Example |
|---------|------|---------------------|
| Feature folder | **Plural**, domain-oriented; axis-1 cohesive root is `web/features/<slice>/` | `web/features/admin/roles/` |
| Filename grammar | `<name>[-<function>]-<layer>.cs`; contracts drop function | `create-role/create-role-contracts.cs` |
| Namespace | **Plural** (does **not** track folder moves) | `{Root}.Features.Admin.Roles` |
| Use-case folder | One folder per operation, contract beside its handler (see `tw-feature-placement`) | `create-role/create-role-contracts.cs`, `get-role/get-role-contracts.cs` |
| Shared bindable shape | Separate file at slice root, not inside any use-case folder | `role-details-contracts.cs` (`IRoleDetails`) |
| Registry | Function→layer SSOT (analyzer + membership guard) | `feature-filename-grammar.json` — edit ⇒ `dev regenerate-generated`, then full rebuild |

**Casing:** kebab-case paths are canonical; if the repo already uses PascalCase folders
(`Features/Admin/Roles/`), match it. Never mix casings within one repo.

## The contract attributes (source-generated)

Two layers of attributes. Route/request attributes are emitted as public types in
`TimeWarp.Foundation.Features` by the bundled contracts generator (class **must be `partial`**).
Server-generation attributes live in `TimeWarp.Architecture.Attributes` and mark which contracts
become hosted FastEndpoints.

### Route / request attributes (on nested `Query`/`Command`)

| Attribute | Generates | Use when |
|-----------|-----------|----------|
| `[ApiRoute("api/…", HttpVerb.X)]` | `RouteTemplate` const, `GetRoute()`, `GetHttpVerb()`, and a typed property per route parameter (`{RoleId:guid}` → `Guid RoleId`) | Every contract request |

`[ApiRoute]` is the only generated request attribute. Every other request member is declared by
hand on the contract, beside the handler that reads it: the published document lists exactly what
the handler uses. A list query that pages declares its own paging members, and a test proves the
page size changes the row count.

The FastEndpoint generator matches `TimeWarp.Foundation.Features.ApiRouteAttribute` by fully qualified metadata
name; the contracts generator emits that public type, so it ignores the generated app's RootNamespace.

#### `[ApiRoute]` route grammar

Templates start with `api/`; each literal segment is lowercase kebab (`api/admin/roles/{RoleId:guid}`), enforced by
TWA0051. Tokens are `{Name}` or `{Name:constraint}`; a type starts **only** after a colon, so bare `{Date}` stays `string`.
`guid` → `Guid`, `datetime` → `DateTime`, `min(n)`/`max(n)` → `int`, other tokens as written. Full grammar: [route-grammar.md](references/route-grammar.md).

### FastEndpoint generation (on the outer operation class)

Both **web-server** and **api-server** host endpoints **generated from contracts** — there are no
hand-written MVC `BaseEndpoint` shims in the template. Opt in per operation:

| Attribute | Effect | Use when |
|-----------|--------|----------|
| `[ApiEndpoint]` | Generator emits `BaseFastEndpoint<Op.Query\|Command, Response>` for this operation | Every contract hosted on a server with `EnableApiEndpointGeneration` |
| `[EndpointAuthorize(Policy=…, AuthenticationSchemes=…)]` | Generator emits `Policies("…")` / `Roles(…)` / `AuthSchemes(…)` | Protected routes. **Always set `AuthenticationSchemes`** on hosted contracts — see scheme lists below |
| `[EndpointAllowAnonymous(reason)]` | Generator emits `AllowAnonymous()` | Genuinely public / pre-auth ceremony endpoints — `reason` is a required, honest, per-contract string |
| *(no marker)* | **Fail-closed (task 110):** generator emits nothing, so FastEndpoints' own default (auth required) applies | Never — every `[ApiEndpoint]` contract must carry exactly one of the two markers above; **TWA0013** flags the omission at build time |

Picking anonymous when the contract shouldn't be is caught too: **TWA0014** flags a contract that
carries both markers, or `[EndpointAllowAnonymous]` alongside a nested `Query`/`Command` that
implements `IAuthApiRequest` — see the auth section below for why that combination is a
contradiction, not just a style nit.

```csharp
[ApiEndpoint]
[EndpointAuthorize
(
  Policy = PermissionIds.IdentityRead,
  AuthenticationSchemes = AuthenticationSchemeNames.AgentToken
)]
public static partial class GetAgentIdentity
{
  [ApiRoute("api/identity/agent/me", HttpVerb.Get)]
  public sealed partial class Query : IApiRequest, IQuery<OneOf<Response, SharedProblemDetails>>;
  // …
}
```

**Policy names (TWA0024):** `[EndpointAuthorize] Policy` must equal a policy the hosting server registers (web: `PermissionIds.*`; named policies via `AddPolicy`). Prefer a contracts-visible const over a literal.

**Scheme lists:** hosted `[EndpointAuthorize]` must set `AuthenticationSchemes` (`AuthenticationSchemeNames` on web); permission
policies carry no scheme list, so omitting it authenticates only the host default scheme. Details and per-surface table:
[endpoint-auth.md](references/endpoint-auth.md).

**Validation stays on the mediator** (`FluentValidationBehavior`). Do not re-validate in handlers
and do not wire FastEndpoints' own FluentValidation integration (`IncludeAbstractValidators =
false`). Handlers implement business logic only; the generated endpoint is pure HTTP plumbing.

Server projects set `<EnableApiEndpointGeneration>true</EnableApiEndpointGeneration>` and
**must** set `ApiEndpointContractAssemblies` to the hosted contracts **AssemblyName**
(`web-contracts` on web-server, `api-contracts` on api-server — not the namespace). Empty,
mistyped, or unmarked names are **TWE008**. Hosted contracts assemblies apply
`[assembly: ApiEndpointsEmbedded]`; host generators walk only marked refs.

### HTTP verbs

| Operation | Verb | Usual kind |
|-----------|------|------------|
| Query | `Get` | `IQuery<…>` |
| Create | `Post` | `ICommand<…>` |
| Update (full replace) | `Put` | `IIdempotentCommand<…>` |
| Delete | `Delete` | `IIdempotentCommand<…>` |

The verb is the contract's `[ApiRoute]` verb. Generated FastEndpoints take it from there. Do not
hand-write a second verb. Analyzer id TWA0005 (MVC verb vs `[ApiRoute]`) is retired and reserved.

The kind does not pick the verb, and the verb does not pick the kind. The column above is the
usual pairing, not a rule: `IIdempotentCommand` promises that a retry is safe, so use `ICommand`
when the handler cannot keep that promise. See [neutral-operations.md](references/neutral-operations.md).

## Neutral operations: one contract, three bindings

The `TimeWarp.Contracts` package adds the transport-neutral part of a contract. Its version pin is its own line in `Directory.Packages.props`. The same request class is an
HTTP endpoint, a CLI command, and an MCP tool:

- **Name, group, and title:** `[Operation("roles.get", Description = "…", Group = "…", Title = "…")]`
  on the nested `Query`/`Command`. A blank title means no title. `[Destructive]` and `[OpenWorld]`
  are tri-state: omitted is unset, not false. MCP treats an omitted destructive hint as true and reads it only when the tool is not read-only. Generators copy the hints only when `[Operation]` has a non-blank name. Kind still supplies read-only and idempotent.
- **Kind:** the mediator interface (`IQuery<T>`, `ICommand<T>`, `IIdempotentCommand<T>`). It is
  never an attribute argument.
- **Bindings:** HTTP comes from `[ApiRoute]` + `[ApiEndpoint]` (this template). CLI and MCP come
  from TimeWarp.Nuru, which reads the same contract and dispatches through the mediator.

**TWA0035** (info) suggests `[Operation]` on a hosted contract that lacks one. Members, kind
choice, bindings, and the catalog: [neutral-operations.md](references/neutral-operations.md).

## Contract shell

Every operation is a `public static partial class` named for the operation:

```csharp
public static partial class CreateRole
{
  [ApiRoute("api/admin/roles", HttpVerb.Post)]
  [Operation("roles.create", Description = "Create a role.", Group = "admin.roles")]
  public sealed partial class Command
    : IAuthApiRequest, IRoleDetails, ICommand<OneOf<Response, SharedProblemDetails>>
  {
    public string Name { get; set; } = null!;
    public string Description { get; set; } = null!;
  }

  public sealed class Validator : AbstractValidator<Command>
  {
    public Validator()
    {
      RuleFor(x => x).SetValidator(new RoleDetailsValidator());
    }
  }

  public sealed class Response
  {
    public Guid RoleId { get; }
    public Response(Guid roleId) => RoleId = Guard.Against.NullOrEmpty(roleId);
  }
}
```

### Nested types

| Type | Name | Role |
|------|------|------|
| Read request | `Query` | GET operations |
| Write request | `Command` | POST/PUT/DELETE |
| Output | `Response` | Success payload |
| Input rules | `Validator` | `AbstractValidator<Query\|Command>` |

The result is always `OneOf<Response, SharedProblemDetails>`, or `OneOf<Stream, SharedProblemDetails>`
for a stream or file. The interface that carries it states the kind: `IQuery<…>` for a read,
`ICommand<…>` for a write, `IIdempotentCommand<…>` for a retry-safe write. Use bare `IRequest<…>`
only when none fits, and say why in the Design region.

### When the request body may be empty

An empty body (`;`) is valid **only** for a route-only request: `[ApiRoute]` generates the route
properties, and nothing else is needed.

**If the request implements `I*Details` (or any data interface), you must declare the interface
properties on the class** — interface members are not generated, and an empty body will not
compile. Do re-declare *data* properties; do **not** re-declare *route* properties (those come
from `[ApiRoute]`).

## Auth requests vs. server auth — two different concerns

`IAuthApiRequest` is a **member-less marker**: "this request serves an authenticated caller." It
does **not** secure the server and does **not** carry the caller. `[EndpointAuthorize]` is the
**sole** marker that secures a generated endpoint, and the handler reads the caller from the
session or bearer principal (`ICurrentPrincipalAccessor`). **TWA0014** enforces that the two do
not contradict each other.

**A request never names its caller.** Do not add a `UserId` (or any caller id) to a hosted
contract: no handler may trust it, so it is a required parameter a generated client has to
invent. Whose data an endpoint touches is decided server-side from the principal. A client-only
contract (`[ClientOnlyContract]`) that needs a user id for mock responses declares its own
property; it never reaches a server.

| `IAuthApiRequest` on `Query`/`Command` | Server auth marker | Meaning |
|---|---|---|
| Present | `[EndpointAuthorize(...)]` | Secured route serving the signed-in caller (e.g. `CreateRole`) |
| Absent | `[EndpointAuthorize(...)]` | Secured route; equally valid — the marker is optional |
| Absent | `[EndpointAllowAnonymous(reason)]` | Genuinely public / pre-auth route; no identity involved |
| **Present** | **`[EndpointAllowAnonymous(reason)]`** | **Forbidden — TWA0014.** A request that serves an authenticated caller on an anonymous endpoint is self-contradictory. Fix by adding `[EndpointAuthorize]` or dropping `IAuthApiRequest`. |

## Workflow

### 1. Identify the operation

Read → `get-*/get-*-contracts.cs` · Write → `create-*|update-*|delete-*/…-contracts.cs`
(one use-case folder per operation, not a `commands`/`queries` split)

### 2. Scaffold the partial class

- `[ApiEndpoint]` on the outer operation class when a server host should generate the FastEndpoint
- Exactly one of `[EndpointAuthorize(Policy=…, AuthenticationSchemes=…)]` (protected; **always
  declare schemes** — task 161) or `[EndpointAllowAnonymous(reason)]` (genuinely public) —
  required on every `[ApiEndpoint]` contract; **TWA0013** flags the omission, **TWA0014** flags
  picking both, or anonymous alongside `IAuthApiRequest`
- `[ApiRoute("api/...", HttpVerb.*)]` on nested `Query`/`Command`
- Implement `IApiRequest` (or the `IAuthApiRequest` marker — see the auth section above; add
  `IQueryStringRouteProvider` when query-string filters apply)
- The kind interface over `OneOf<Response, SharedProblemDetails>`: `IQuery`, `ICommand`, or
  `IIdempotentCommand` (see [neutral-operations.md](references/neutral-operations.md))
- `[Operation("<resource>.<action>", Description = "…")]` on the nested `Query`/`Command`, so HTTP,
  CLI, and MCP share one name (**TWA0035** suggests it on hosted contracts without one)

### 3. Bindable data — interface-driven validation

When Blazor will bind and edit the payload:

1. Define `I<Feature>Details` in a feature-level file (e.g. `role-details-contracts.cs`).
2. Mutable bindable properties use `{ get; set; }` on the interface (no initializers —
   interfaces cannot have them; `= null!` goes on the **implementing class**).
3. Identity/read-only keys on implementations use `{ get; init; }` or `{ get; }`.
4. Add `AbstractValidator<I<Feature>Details>` in the same file.
5. `Create*` / `Update*` **Command implements the interface** and is the `EditForm` model;
   `partial` with a parameterless constructor gets a generated `From(I<Feature>Details)`.
6. `Get*` **Response implements the interface** so `From` can copy it; the store keeps it whole
   and the form never binds it. `partial` gets a generated deep `Clone`.
7. Endpoint `Validator` composes: `RuleFor(x => x).SetValidator(new RoleDetailsValidator());`

This is the core value over default .NET DTO patterns: **one shape, shared rules, no parallel view model**.

When an aggregate root lives in the slice (`<slice>.Domain`), its group's contracts agree with it
by exact member name: details and query-response members exist on the root, a `Set<Name>` list
command has a root `Set<Name>` mutation, a handler-derived response value is `[Computed]`, and any
other operation carries `[NotAggregate("reason")]` (TWA0039–TWA0043, `tw-aggregate-pattern`).

`From` runs on load, Cancel, and own save, not every render; `Version` stays off the interface
(`CopyBeyondDetails`). Rules, TWA0047–TWA0049, Blazilla: [edit forms](references/mutability.md#edit-forms-store-snapshot-and-working-copy).

### 4. Apply nullability — type declares intent

Nullability is **not** inferred from validators. The **type annotation is the contract**;
validators must agree.

| Intent | Type | Initializer | Validator |
|--------|------|-------------|-----------|
| Required after validation | `string` | `= null!` | `NotEmpty()` / `NotNull()` |
| Truly optional / absent OK | `string?` | none | No unconditional `NotEmpty()`; use `.When(x => x != null)` if format rules apply when present |
| Required nested object | `Person` | `= null!` | `RuleFor(x => x.Person).NotNull().SetValidator(...)` |
| Optional nested object | `Person?` | none | Validate only when present |
| Required value type | `int`, `Guid`, … | default | `GreaterThan(0)`, `NotEmpty()`, etc. |
| Optional value type | `int?`, `DateTime?` | none | Rules only when `.HasValue` / `.When(...)` |

**The two contradictions (not equal sins, both rejected):**

- `= string.Empty` on a required field (+ `NotEmpty()`) — **forbidden, a real bug**: JSON omission
  leaves `""`, `NotEmpty()` passes, silent wrong data.
- `string?` with unconditional `NotEmpty()` — **discouraged, a smell**: runtime behavior is right,
  but the annotation lies and disarms the compiler's null analysis.

The timewarp-architecture template enforces both at build time (**TWA0002**/**TWA0003**).

Also forbidden: `= default!` on non-generic reference types (use `null!`), and FluentValidation on
`Response` (use ctor + `Guard.Against.*`; validation is for user-facing requests).

See [nullability.md](references/nullability.md).

### 5. Apply mutability — accessor declares intent

| Intent | Accessor | Collection |
|--------|----------|------------|
| Display / server-built | `{ get; }` or `{ get; init; }` | `IReadOnlyList<T>` |
| Blazor bindable / edit | `{ get; set; }` on `I*Details` | `List<T>` when editable |

Read-only display sharing across endpoints: get-only interfaces (e.g. `IPolicyDto`) — not bindable, not `I*Details`.

### 6. Response patterns — the discriminator is "has invariants"

| Case | Pattern |
|------|---------|
| **Any invariant to enforce** (non-empty id, valid state) | Parameterized ctor + `Guard.Against.*`; immutable `{ get; }` |
| Editable load | `partial`; implements `I*Details`; ctor sets identity; mutable `{ get; set; }` on the details fields; generated deep `Clone` (never `Clone() => this`, TWA0047); stored whole, copied into the Command by `From` |
| List | `Response : ListResponse<TDto>` |
| No body | `public sealed class Response;` |
| Truly invariant-free echo | `public required int Id { get; init; }` acceptable |
| File/stream | `IQuery<OneOf<Stream, SharedProblemDetails>>` |

`required init` is **not** a general alternative to ctor+`Guard`: it enforces *presence at the
construction site* but checks nothing — `new Response { RoleId = Guid.Empty }` compiles and ships.
If the field has any invariant (a `Guid` id that must be non-empty **is** one), use ctor+`Guard`.

### 7. Query-string queries

Implement `IQueryStringRouteProvider` + `GetRouteWithQueryString()` for optional filters.
Optional filter properties are `string?` / nullable value types with **no** unconditional
required rules. Put each filter the handler reads into the query string, and nothing else:

```csharp
var collection = new NameValueCollection
{
  { nameof(Search), string.IsNullOrWhiteSpace(Search) ? null : Search.Trim() },
};
return $"{GetRoute()}?{this.GetQueryString(collection)}";
```

### 8. Validator

- Compose shared validators via `SetValidator`.
- Empty validator is valid: `public sealed class Validator : AbstractValidator<Query>;`
- **The validator is the source of OpenAPI `required`.** `OpenApiValidatorRequired` reads the
  validators `FluentValidationBehavior` runs. A member is published as required (body schema
  `required`, or a required query parameter) when an unconditional `NotEmpty()` or `NotNull()`
  rejects the value the member keeps when the caller omits it. Rules reached through
  `RuleFor(x => x).SetValidator(...)` and `Include(...)` count. These do not make a member
  required: a rule under `When`/`Unless`, `RuleForEach`, a named rule set, a nested path
  (`x => x.Address.Street`), and `NotNull()` on a member with a non-null initializer (`= []`),
  because omission keeps the initializer and passes.
- Do not add `[Required]` or the `required` keyword to mark the document. Write the rule in the
  validator. A document check fails `/openapi/{doc}.json` (500, naming the operation and member)
  when a later transformer drops `required` from a member that has such a rule.
- Do **not** add isolated validator unit tests in the contracts test project — FluentValidation
  is tested at integration level.

### 9. Contract serialization tests (dedicated project)

Contracts are authored **before** the server exists (frontend-first, mock-backed BFF flow); a
host-free serialization check is the only test that can run in that window. Prefer a
**co-located Jaribu** `*-tests.cs` next to the contract (epic 145 north star; see
`create-role-tests.cs`). Suite-shaped `*contracts-tests` projects are Jaribu MTP — assert with
**Shouldly** only; do not reintroduce Fixie or xUnit.

Add `SerializeAndDeserialize` round-trips using `ContractSerializationDefaults`. Never declare
contract-seam `JsonSerializerOptions` inline. The shared options are camelCase properties and
PascalCase string enums via `JsonStringEnumConverter` (integers rejected). **Prioritize**
contracts where serialization can actually diverge: `required`/`init` members, custom converters,
non-default constructors, enum properties, `OneOf`/`SharedProblemDetails` envelopes. Plain
auto-property POCOs are low-priority once server integration tests exist. Do not use
FluentAssertions (v8+ is commercially licensed).

### 10. Mock response factory (when mock mode needs it)

Add `GetMockResponseFactory()` on the contract + register it in the SPA mock service **when SPA
mock mode needs this endpoint** — the mock service falls back to the real API for unregistered
types, so factories are per-endpoint opt-in, not mandatory ceremony.

**Detect the repo's mock pattern first**: the canonical shape puts the factory *on the contract*
and registers it in a `Dictionary<Type, Delegate>`; some solutions instead use standalone
`*MockFactory` classes inside the SPA. Copying the wrong shape into a repo is a common agent
error. See the `tw-mock-response-factory` skill.

### 11. Referenceable resources (Ask `@` references)

A list query people should `@`-reference in Ask carries `[ReferenceableResource]`; the `@` registry
is generated from it. Shape, rules and the analyzer: [references/referenceable-resources.md](references/referenceable-resources.md).

## Validation checklist

- [ ] `public static partial class` with nested `Query`/`Command`, `Response`, `Validator`
- [ ] `[ApiEndpoint]` on hosted operations; exactly one of `[EndpointAuthorize]` /
      `[EndpointAllowAnonymous(reason)]`, always (TWA0013/TWA0014 enforce this)
- [ ] Hosted `[EndpointAuthorize] Policy` is a registered server policy (`PermissionIds.*` or an
      `AddPolicy` name) — TWA0024 flags drift at the server build
- [ ] Hosted `[EndpointAuthorize]` sets `AuthenticationSchemes`
- [ ] `[ApiRoute]` with correct verb and route constraints (`{Id:guid}`, `{Id:min(1)}`, …)
- [ ] Kind interface over `OneOf<Response, SharedProblemDetails>`: `IQuery` / `ICommand` /
      `IIdempotentCommand` (TimeWarp.Mediator); bare `IRequest` only with a stated reason
- [ ] `[Operation("<resource>.<action>", Description = "…")]` on the request (TWA0035)
- [ ] Folder plural + repo's casing; namespace plural
- [ ] Bindable flows use `I*Details` + shared `AbstractValidator<I*Details>`; Command/Response
      declare the interface properties
- [ ] Edit form binds the Command from `From`, not the stored Response (TWA0047–TWA0049)
- [ ] Nullability matches validator rules — no `string?` + unconditional `NotEmpty()` (TWA0002),
      no `= string.Empty` on required fields (TWA0003)
- [ ] No `default!` on non-generic reference types
- [ ] Response invariants enforced in ctor + `Guard`, not FluentValidation
- [ ] Mutability matches binding intent (`set` vs `init`/get-only)
- [ ] Serialization round-trip test in the contracts test project (prioritize non-trivial shapes)
- [ ] `GetMockResponseFactory()` registered if SPA mock mode exercises this endpoint
- [ ] A list query people should `@`-reference in Ask carries `[ReferenceableResource]` and a
      `string? Search` (TWA0033/TWA0034)

## Common pitfalls

| Pitfall | Fix |
|---------|-----|
| `string?` + `NotEmpty()` | Required field → `string` + `= null!` + `NotEmpty()` |
| `= string.Empty` + `NotEmpty()` | Silent-bug default → `= null!` (or `required`) |
| Empty `Command` body while implementing `I*Details` | Won't compile — declare the interface's data properties on the class |
| Initializer on an interface property | Invalid C# — `= null!` belongs on the implementing class |
| Separate Blazor view model, or `EditForm` bound to the stored Response | Bind `I*Details` over `Update*.Command.From(response)` |
| Runtime-type validator resolution (Morris) with interface binding | Use Blazilla's explicit `Validator` instance parameter |
| Entity-centric shared DTO per endpoint | Endpoint-centric types; share only validation interfaces or read-only display interfaces |
| `sealed record` request/response | Classes + `partial` + source generation |
| Hand-declared route params | Trust `[ApiRoute]` source generation |
| `[ApiEndpoint]` with no auth marker, assuming the generator defaults to anonymous | It doesn't (task 110, fail-closed) — no marker emits nothing, so FastEndpoints' own default (auth required) applies; TWA0013 also catches it at build time |
| `[EndpointAuthorize(Policy=…)]` without `AuthenticationSchemes` | Non-default schemes never run against PermissionIds policies. Set `AuthenticationSchemes`. |
| `[EndpointAuthorize(Policy = "…")]` literal that does not match a server `AddPolicy` / `PermissionIds` name | TWA0024 at the server build (task 111). Use `PermissionIds.*` on web; keep api literals byte-identical to `AgentTokenDefaults` policy constants |
| Treating `IAuthApiRequest` as if it secures the route | It's a member-less marker — `[EndpointAuthorize]` is the sole server-auth marker; TWA0014 flags pairing `IAuthApiRequest` with `[EndpointAllowAnonymous]` |
| A `UserId` (caller id) on a hosted request, or a paging member the handler ignores | Published as a parameter a client must fill and the server never reads. The handler reads the principal; declare only members the handler uses |
| Re-validating in the handler or enabling FE FluentValidation | Validation is `FluentValidationBehavior` on the mediator only |
| `[Required]` on a request member so OpenAPI lists it as required | Write `NotEmpty()` / `NotNull()` in the `Validator`; the document takes `required` from it |
| A new slice's first web-server operation with no tag description | Add its tag (the namespace leaf under `Features`) to `WebServerOpenApiTags.Descriptions`; `/openapi/v1.json` returns 500 naming a missing one |
| `required init` Response with invariants | `Guid.Empty` slips through — ctor + `Guard` |
| Copying paths/casing from another repo | Read existing contracts in **this** repo first |

## Canonical examples

- **Living anchor (timewarp-architecture template):** `web/features/admin/roles/` — every
  operation carries `[Operation]` and a kind interface (`IQuery`, `ICommand`, `IIdempotentCommand`);
  `role-details-contracts.cs` (`IRoleDetails` + validator, shared at slice root),
  `create-role/create-role-contracts.cs`, `get-roles/get-roles-contracts.cs` (`Search` is the only
  query parameter), `get-role/get-role-contracts.cs` (the route id is the whole request:
  `IAuthApiRequest`, no caller property, `I*Details` Response). Each
  contract sits beside its handler in the same use-case folder
  (`create-role/create-role-handler-application.cs`); layer projects compose via static
  `*-{layer}.cs` globs regardless of folder.
- Inline reference implementations: [examples.md](references/examples.md).

## Related skills

- `tw-feature-placement` — the filename grammar every `-contracts.cs` file follows (function
  segment dropped, escape hatch, registry, TWA0015/TWA0016)
- `tw-mock-response-factory` — `GetMockResponseFactory()` on contracts + SPA mock service registration
- `tw-csharp` — formatting and naming only; does not override contract nullability/mutability rules
- `tw-blazor-layout` / `tw-blazor-css-strategy` — UI shell and styling; contracts feed `EditForm` binding
- `tw-slice-isolation` — product-slice placement / TWA0009; **contracts assemblies are free**
  under TWA0009 (other assembly), but still use plural `…Features.*` namespaces aligned with
  the SPA product slices they serve
- Do **not** use `dotnet-webapi` for this contract pattern

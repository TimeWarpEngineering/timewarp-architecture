---
name: tw-web-api-contracts
description: >-
  **TIMEWARP SKILL** — endpoint-centric Web.Contracts API contracts (Command, Query, ApiRoute,
  I*Details, Validator, serialization tests). Invoke before scaffolding or fixing contracts.
  WHEN: "Add a CreateTodoItem command contract", "Scaffold a GetRole query with ApiRoute and
  IRoleDetails for the edit form", "Add a serialization round-trip test for my Command".
when-to-use: >
  Web.Contracts, web-contracts, command contract, query contract, ApiRoute, ApiEndpoint,
  EndpointAuthorize, EndpointAllowAnonymous, I*Details, EditForm binding, Validator,
  serialization round-trip, BFF, AuthApiRequest, FastEndpoint generation
---

# Web API Contracts

Endpoint-centric, JSON-over-HTTP contracts designed for **Blazor front ends**. Each
endpoint owns its request/response types. **Mutability signals purpose**: immutable
members are read-only display data; mutable members on `I*Details` interfaces bind in
`EditForm` without a separate view model. **Shared validation** lives on interfaces and
is composed into per-endpoint validators.

This pattern appears across TimeWarp-based solutions. Project names vary (`web-contracts`,
`Web.Contracts`, `Api.Contracts`, …) but the contract shape is the same.

## Detection — find the pattern in the current repo

Activate when **any** signal matches:

| Signal | How to find it |
|--------|----------------|
| Contracts project | `*.csproj` named `*contracts*` (any casing) with a `features/` or `Features/` tree |
| Contract file layout | `**/features/**/commands/*.cs` or `**/features/**/queries/*.cs` (search **case-insensitively** — repos use kebab `features/` or Pascal `Features/`) |
| Contract shell | `public static partial class` + nested `Query`/`Command` + `[ApiRoute(...)]` |
| TimeWarp.Mediator return | `IRequest<OneOf<Response, SharedProblemDetails>>` |
| Shared validation | `I*Details` interface + `AbstractValidator<I*Details>` |

The mediator is **TimeWarp.Mediator** (not MediatR) — same `IRequest<T>` shape, different package.

Before adding a contract, **read 2–3 existing contracts in the same repo** to match
namespace root, folder casing, test project layout, and mock-service registration.

## Folder and namespace rules

| Concern | Rule | Example |
|---------|------|---------------------|
| Feature folder | **Plural**, domain-oriented | `features/admin/roles/` |
| Namespace | **Plural** | `{Root}.Features.Admin.Roles` |
| Commands / Queries | Subfolders under feature | `commands/create-role.cs`, `queries/get-role.cs` |
| Shared bindable shape | Separate file in feature folder | `role-details.cs` (`IRoleDetails`) |

**Casing:** kebab-case paths are canonical; if the repo already uses PascalCase folders
(`Features/Admin/Roles/`), match it. Never mix casings within one repo.

## The contract attributes (source-generated)

Two layers of attributes. Route/mixin attributes are emitted into the consumer's root namespace by
the bundled contracts generator (class **must be `partial`**). Server-generation attributes live
in `TimeWarp.Architecture.Attributes` and mark which contracts become hosted FastEndpoints.

### Route / request mixins (on nested `Query`/`Command`)

| Attribute | Generates | Use when |
|-----------|-----------|----------|
| `[ApiRoute("api/…", HttpVerb.X)]` | `RouteTemplate` const, `GetRoute()`, `GetHttpVerb()`, and a typed property per route parameter (`{RoleId:guid}` → `Guid RoleId`) | Every contract request |
| `[AuthApiRequest]` | `Guid UserId { get; set; }` + private `GetAuthQueryParameters()` for query-string composition | List/GET queries that carry user identity in the query string |
| `[OpenDataQueryParameters]` | `Top`/`Skip`/`Filter`/`OrderBy`/`ReturnTotalCount` + private `GetOpenDataQueryParameters()` | Pageable/sortable list queries |

The FastEndpoint generator matches `ApiRouteAttribute` by simple name, so the attribute works from
any root namespace.

### FastEndpoint generation (on the outer operation class)

Both **web-server** and **api-server** host endpoints **generated from contracts** — there are no
hand-written MVC `BaseEndpoint` shims in the template. Opt in per operation:

| Attribute | Effect | Use when |
|-----------|--------|----------|
| `[ApiEndpoint]` | Generator emits `BaseFastEndpoint<Op.Query\|Command, Response>` for this operation | Every contract hosted on a server with `EnableApiEndpointGeneration` |
| `[EndpointAuthorize(Policy=…)]` | Generator emits `Policies("…")` / `Roles(…)` / `AuthSchemes(…)` | Protected routes (policy, roles, and/or schemes) |
| `[EndpointAllowAnonymous(reason)]` | Generator emits `AllowAnonymous()` | Genuinely public / pre-auth ceremony endpoints — `reason` is a required, honest, per-contract string |
| *(no marker)* | **Fail-closed (task 110):** generator emits nothing, so FastEndpoints' own default (auth required) applies | Never — every `[ApiEndpoint]` contract must carry exactly one of the two markers above; **TWA0013** flags the omission at build time |

Picking anonymous when the contract shouldn't be is caught too: **TWA0014** flags a contract that
carries both markers, or `[EndpointAllowAnonymous]` alongside a nested `Query`/`Command` that
declares `IAuthApiRequest` (manually or via the `[AuthApiRequest]` mixin) — see the auth-forms
section below for why that combination is a contradiction, not just a style nit.

```csharp
[ApiEndpoint]
[EndpointAuthorize(Policy = "agent-scope:identity:read")]
public static partial class GetAgentIdentity
{
  [ApiRoute("api/identity/agent/me", HttpVerb.Get)]
  public sealed partial class Query : IApiRequest, IRequest<OneOf<Response, SharedProblemDetails>>;
  // …
}
```

**Validation stays on the mediator** (`FluentValidationBehavior`). Do not re-validate in handlers
and do not wire FastEndpoints' own FluentValidation integration (`IncludeAbstractValidators =
false`). Handlers implement business logic only; the generated endpoint is pure HTTP plumbing.

Server projects set `<EnableApiEndpointGeneration>true</EnableApiEndpointGeneration>`. Web-server
also sets `ApiEndpointContractAssemblies` so only web-contracts contribute endpoints (it
transitively references other contract assemblies).

### HTTP verbs

| Operation | Verb |
|-----------|------|
| Query | `Get` |
| Create | `Post` |
| Update | `Put` |
| Delete | `Delete` |

The verb must match the server endpoint.

## Contract shell

Every operation is a `public static partial class` named for the operation:

```csharp
public static partial class CreateRole
{
  [ApiRoute("api/Roles", HttpVerb.Post)]
  public sealed partial class Command
    : IAuthApiRequest, IRoleDetails, IRequest<OneOf<Response, SharedProblemDetails>>
  {
    public Guid UserId { get; set; }
    public string Name { get; set; } = null!;
    public string Description { get; set; } = null!;
  }

  public sealed class Validator : AbstractValidator<Command>
  {
    public Validator()
    {
      RuleFor(x => x).SetValidator(new RoleDetailsValidator());
      RuleFor(x => x).SetValidator(new AuthApiRequestValidator());
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

Return type is always `IRequest<OneOf<Response, SharedProblemDetails>>` unless returning
a stream/file (`OneOf<Stream, SharedProblemDetails>`).

### When the request body may be empty

An empty body (`;`) is valid **only** for a route-only request: `[ApiRoute]` generates the route
properties, and nothing else is needed.

**If the request implements `I*Details` (or any data interface), you must declare the interface
properties on the class** — interface members are not generated, and an empty body will not
compile. Do re-declare *data* properties; do **not** re-declare *route* properties (those come
from `[ApiRoute]`).

## Auth requests vs. server auth — two different concerns

**Canonical statement (task 110):** `IAuthApiRequest` is a **client/mock-mode identity signal
only** — it does **not** secure the server. `[EndpointAuthorize]` is the **sole** marker that
secures a generated endpoint. The two are independent axes; **TWA0014** enforces that they don't
contradict each other.

### The three valid states (plus the one that's forbidden)

| `IAuthApiRequest` on `Query`/`Command` | Server auth marker | Meaning |
|---|---|---|
| Present | `[EndpointAuthorize(...)]` | Secured route; client *also* carries `UserId` for mock-mode tailoring (e.g. `CreateRole`) |
| Absent | `[EndpointAuthorize(...)]` | Secured route; server derives identity entirely from the auth token/claims — contract stays auth-agnostic |
| Absent | `[EndpointAllowAnonymous(reason)]` | Genuinely public / pre-auth route; no identity involved |
| **Present** | **`[EndpointAllowAnonymous(reason)]`** | **Forbidden — TWA0014.** A contract that carries a user identity but declares its endpoint unauthenticated is self-contradictory. Fix by adding `[EndpointAuthorize]` or dropping `IAuthApiRequest`. |

`IAuthApiRequest` is detected by either shape the mixin generator produces — the interface
implementation or the `[AuthApiRequest]` attribute itself — so the forbidden row is caught
regardless of which of the two forms below produced it.

### The two forms of `IAuthApiRequest` itself

The contract can carry the current user's identity (`Guid UserId`) so that **mock mode** — where
no server exists to derive identity — can tailor responses per user. Two forms:

| Form | Shape | Use when |
|------|-------|----------|
| **Attribute** `[AuthApiRequest]` | Generates `UserId` **and** `GetAuthQueryParameters()` | Query-string queries (`IQueryStringRouteProvider` — lists, filters) that must append `UserId` to the URL |
| **Manual** `: IAuthApiRequest` + declared `Guid UserId { get; set; }` | You write the property | POST bodies and GET-by-id routes — no query-string composition needed |

Both pair with `RuleFor(x => x).SetValidator(new AuthApiRequestValidator())`.

**Security rule:** the server must **never trust** a client-sent `UserId` — it re-derives identity
from the auth token. The contract field exists for mock-mode tailoring and client-side context;
it plays no role in whether the generated endpoint actually requires authentication.

**Valid alternative:** derive the user **entirely server-side** (claims/token) and keep contracts
auth-agnostic. Choose it when mock-mode identity isn't needed; it is not wrong.

## Workflow

### 1. Identify the operation

Read → `queries/get-*.cs` · Write → `commands/create-|update-|delete-*.cs`

### 2. Scaffold the partial class

- `[ApiEndpoint]` on the outer operation class when a server host should generate the FastEndpoint
- Exactly one of `[EndpointAuthorize(Policy=…)]` (protected) or `[EndpointAllowAnonymous(reason)]`
  (genuinely public) — required on every `[ApiEndpoint]` contract; **TWA0013** flags the omission,
  **TWA0014** flags picking both, or anonymous alongside `IAuthApiRequest`
- `[ApiRoute("api/...", HttpVerb.*)]` on nested `Query`/`Command`
- Implement `IApiRequest` (or `IAuthApiRequest` — see auth forms above; add
  `IQueryStringRouteProvider` when query-string filters apply)
- `IRequest<OneOf<Response, SharedProblemDetails>>`

### 3. Bindable data — interface-driven validation

When Blazor will bind and edit the payload:

1. Define `I<Feature>Details` in a feature-level file (e.g. `role-details.cs`).
2. Mutable bindable properties use `{ get; set; }` on the interface (no initializers —
   interfaces cannot have them; `= null!` goes on the **implementing class**).
3. Identity/read-only keys on implementations use `{ get; init; }` or `{ get; }`.
4. Add `AbstractValidator<I<Feature>Details>` in the same file.
5. `Create*` / `Update*` **Command implements the interface** (and declares its properties).
6. `Get*` **Response implements the interface** when the form loads existing data for edit.
7. Endpoint `Validator` composes: `RuleFor(x => x).SetValidator(new RoleDetailsValidator());`

This is the core value over default .NET DTO patterns: **one shape, shared rules, no
parallel view model**.

**The Blazor side is validation-library-dependent.** Binding `EditForm` to the *interface* and
running the shared validator requires a library that accepts an **explicit validator instance** —
**Blazilla**: `<FluentValidator Validator="@(new RoleDetailsValidator())" />`. Libraries that
resolve validators by the model's *runtime type* (Morris.Blazor.FluentValidation) can never find
an `AbstractValidator<I*Details>` for a `Command` model; Blazored.FluentValidation worked but is
deprecated. Living reference: `RoleForm.razor` (`web-spa/features/admin/roles/components/`).

See [mutability.md](references/mutability.md).

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

Read-only display sharing across endpoints: get-only interfaces (e.g. `IPolicyDto`) — not
bindable, not `I*Details`.

### 6. Response patterns — the discriminator is "has invariants"

| Case | Pattern |
|------|---------|
| **Any invariant to enforce** (non-empty id, valid state) | Parameterized ctor + `Guard.Against.*`; immutable `{ get; }` |
| Editable load | Implements `I*Details`; ctor sets identity; mutable `{ get; set; }` on bindable fields |
| List | `Response : ListResponse<TDto>` |
| No body | `public sealed class Response;` |
| Truly invariant-free echo | `public required int Id { get; init; }` acceptable |
| File/stream | `IRequest<OneOf<Stream, SharedProblemDetails>>` |

`required init` is **not** a general alternative to ctor+`Guard`: it enforces *presence at the
construction site* but checks nothing — `new Response { RoleId = Guid.Empty }` compiles and ships.
If the field has any invariant (a `Guid` id that must be non-empty **is** one), use ctor+`Guard`.

### 7. Query-string queries

Implement `IQueryStringRouteProvider` + `GetRouteWithQueryString()` for optional filters.
Optional filter properties are `string?` / nullable value types with **no** unconditional
required rules. Compose generated helpers into the query string:

```csharp
var collection = new NameValueCollection { GetAuthQueryParameters(), GetOpenDataQueryParameters() };
return $"{GetRoute()}?{this.GetQueryString(collection)}";
```

### 8. Validator

- Compose shared validators via `SetValidator`.
- Empty validator is valid: `public sealed class Validator : AbstractValidator<Query>;`
- Do **not** add isolated validator unit tests in the contracts test project — FluentValidation
  is tested at integration level.

### 9. Contract serialization tests (dedicated project)

Contracts are authored **before** the server exists (frontend-first, mock-backed BFF flow); a
dedicated, host-free contracts test project (`*contracts-tests`, Fixie + **Shouldly**) is the only
test that can run in that window.

Add `SerializeAndDeserialize` round-trips using `ContractSerializationDefaults` (camelCase
properties; PascalCase string enums via `JsonStringEnumConverter`, integers rejected). **Prioritize**
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

## Validation checklist

- [ ] `public static partial class` with nested `Query`/`Command`, `Response`, `Validator`
- [ ] `[ApiEndpoint]` on hosted operations; exactly one of `[EndpointAuthorize]` /
      `[EndpointAllowAnonymous(reason)]`, always (TWA0013/TWA0014 enforce this)
- [ ] `[ApiRoute]` with correct verb and route constraints (`{Id:guid}`, `{Id:min(1)}`, …)
- [ ] `IRequest<OneOf<Response, SharedProblemDetails>>` (TimeWarp.Mediator)
- [ ] Folder plural + repo's casing; namespace plural
- [ ] Bindable flows use `I*Details` + shared `AbstractValidator<I*Details>`; Command/Response
      declare the interface properties
- [ ] Nullability matches validator rules — no `string?` + unconditional `NotEmpty()` (TWA0002),
      no `= string.Empty` on required fields (TWA0003)
- [ ] No `default!` on non-generic reference types
- [ ] Response invariants enforced in ctor + `Guard`, not FluentValidation
- [ ] Mutability matches binding intent (`set` vs `init`/get-only)
- [ ] Serialization round-trip test in the contracts test project (prioritize non-trivial shapes)
- [ ] `GetMockResponseFactory()` registered if SPA mock mode exercises this endpoint

## Common pitfalls

| Pitfall | Fix |
|---------|-----|
| `string?` + `NotEmpty()` | Required field → `string` + `= null!` + `NotEmpty()` |
| `= string.Empty` + `NotEmpty()` | Silent-bug default → `= null!` (or `required`) |
| Empty `Command` body while implementing `I*Details` | Won't compile — declare the interface's data properties on the class |
| Initializer on an interface property | Invalid C# — `= null!` belongs on the implementing class |
| Separate Blazor view model | Command/Response implement `I*Details`; bind the interface |
| Runtime-type validator resolution (Morris) with interface binding | Use Blazilla's explicit `Validator` instance parameter |
| Entity-centric shared DTO per endpoint | Endpoint-centric types; share only validation interfaces or read-only display interfaces |
| `sealed record` request/response | Classes + `partial` + source generation |
| Hand-declared route params | Trust `[ApiRoute]` source generation |
| Hand-written MVC `BaseEndpoint` shim for a hosted contract | Annotate `[ApiEndpoint]` (+ `[EndpointAuthorize]` or `[EndpointAllowAnonymous(reason)]`); generation is the template convention |
| `[ApiEndpoint]` with no auth marker, assuming the generator defaults to anonymous | It doesn't (task 110, fail-closed) — no marker emits nothing, so FastEndpoints' own default (auth required) applies; TWA0013 also catches it at build time |
| Treating `IAuthApiRequest` as if it secures the route | It's a client/mock-mode identity signal only — `[EndpointAuthorize]` is the sole server-auth marker; TWA0014 flags pairing `IAuthApiRequest` with `[EndpointAllowAnonymous]` |
| Re-validating in the handler or enabling FE FluentValidation | Validation is `FluentValidationBehavior` on the mediator only |
| `required init` Response with invariants | `Guid.Empty` slips through — ctor + `Guard` |
| Copying paths/casing from another repo | Read existing contracts in **this** repo first |

## Canonical examples

- **Living anchor (timewarp-architecture template):** `web-contracts/features/admin/roles/` —
  `role-details.cs` (`IRoleDetails` + validator), `commands/create-role.cs`, `queries/get-roles.cs`
  (attribute auth + open-data), `queries/get-role.cs` (manual auth, `I*Details` Response).
- Inline reference implementations: [examples.md](references/examples.md).

## Related skills

- `tw-mock-response-factory` — `GetMockResponseFactory()` on contracts + SPA mock service registration
- `tw-csharp` — formatting and naming only; does not override contract nullability/mutability rules
- `tw-blazor-layout` / `tw-blazor-css-strategy` — UI shell and styling; contracts feed `EditForm` binding
- `tw-slice-isolation` — product-slice placement / TWA0009; **contracts assemblies are free**
  under TWA0009 (other assembly), but still use plural `…Features.*` namespaces aligned with
  the SPA product slices they serve
- Do **not** use `dotnet-webapi` for this contract pattern

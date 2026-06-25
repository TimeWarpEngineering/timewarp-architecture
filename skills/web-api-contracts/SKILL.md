---
name: web-api-contracts
description: >-
  **TIMEWARP SKILL** — endpoint-centric Web.Contracts API contracts (Command, Query, RouteMixin,
  I*Details, Validator, serialization tests). Invoke before scaffolding or fixing contracts.
  WHEN: "Add a CreateTodoItem command contract in Web.Contracts Features folder",
  "Scaffold a GetRole query with RouteMixin and IRoleDetails for the edit form",
  "Fix string? plus NotEmpty contradiction in my API contract Command",
  "Add Web.Contracts.Tests serialize and deserialize round-trip for my Command".
when-to-use: >
  Web.Contracts, Features folder, command contract, query contract, RouteMixin, IRoleDetails,
  string? plus NotEmpty, API contract Command, serialize and deserialize round-trip,
  Web.Contracts.Tests, partial class, Validator, BFF
---

# Web API Contracts

Endpoint-centric, JSON-over-HTTP contracts designed for **Blazor front ends**. Each
endpoint owns its request/response types. **Mutability signals purpose**: immutable
members are read-only display data; mutable members on `I*Details` interfaces bind in
`EditForm` without a separate view model. **Shared validation** lives on interfaces and
is composed into per-endpoint validators.

This pattern appears across TimeWarp-based solutions. Project names vary (`Web.Contracts`,
`Api.Contracts`, …) but the contract shape is the same.

## Detection — find the pattern in the current repo

Activate when **any** signal matches:

| Signal | How to find it |
|--------|----------------|
| Contracts project | `*.csproj` named `*Contracts*` referencing `Features/` |
| Contract file layout | `**/Features/**/Commands/*.cs` or `**/Features/**/Queries/*.cs` |
| Contract shell | `public static partial class` + nested `Query`/`Command` + `[RouteMixin(...)]` |
| MediatR return | `IRequest<OneOf<Response, SharedProblemDetails>>` |
| Shared validation | `I*Details` interface + `AbstractValidator<I*Details>` |

Before adding a contract, **read 2–3 existing contracts in the same repo** to match
namespace root, test project layout, and mock-service registration conventions.

## Folder and namespace rules

| Concern | Rule | Example |
|---------|------|---------|
| Feature folder | Singular, domain-oriented | `Features/Admin/SecurityRole/` |
| Namespace | **Plural** — avoids class/name conflicts | `{Root}.Features.Admin.SecurityRoles` |
| Commands / Queries | Subfolders under feature | `Commands/CreateSecurityRole.cs` |
| Shared bindable shape | Separate file in feature folder | `SecurityRoleDetails.cs` |

**Namespaces do not mirror folder names.** Plural namespaces and singular folders are
intentional — two separate concerns.

## Contract shell

Every operation is a `public static partial class` named for the operation:

```csharp
public static partial class CreateSecurityRole
{
  [RouteMixin("api/SecurityRoles", HttpVerb.Post)]
  public sealed partial class Command
    : IApiRequest, ISecurityRoleDetails, IRequest<OneOf<Response, SharedProblemDetails>>
  { /* properties */ }

  public sealed class Validator : AbstractValidator<Command> { /* rules */ }
  public sealed class Response { /* ... */ }
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

### Route parameters

Route/template parameters (`SecurityRoleId`, `AccountId`, …) come from `[RouteMixin]`
via source generation. The `Query`/`Command` body is often empty (`;`) — do not
re-declare generated route properties by hand.

### HTTP verbs

| Operation | Verb |
|-----------|------|
| Query | `Get` |
| Create | `Post` |
| Update | `Put` |
| Delete | `Delete` |

## Workflow

### 1. Identify the operation

Read → `Queries/Get*.cs` · Write → `Commands/Create|Update|Delete*.cs`

### 2. Scaffold the partial class

- `[RouteMixin("api/...", HttpVerb.*)]` on nested `Query`/`Command`
- Implement `IApiRequest` (and `IQueryStringRouteProvider` when query-string filters apply)
- `IRequest<OneOf<Response, SharedProblemDetails>>`

### 3. Bindable data — interface-driven validation

When Blazor will bind and edit the payload:

1. Define `I<Feature>Details` in a feature-level file (e.g. `SecurityRoleDetails.cs`).
2. Mutable bindable properties use `{ get; set; }` on the interface.
3. Identity/read-only keys on implementations use `{ get; init; }` or `{ get; }`.
4. Add `AbstractValidator<I<Feature>Details>` in the same file.
5. `Create*` / `Update*` **Command implements the interface**.
6. `Get*` **Response implements the interface** when the form loads existing data for edit.
7. Endpoint `Validator` composes: `RuleFor(x => x).SetValidator(new SecurityRoleDetailsValidator());`

This is the core value over default .NET DTO patterns: **one shape, shared rules, no
parallel view model**.

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

**Forbidden**

- `string?` with unconditional `NotEmpty()` — contradiction; use non-nullable `string` + `null!`
- `= string.Empty` on required fields — JSON omission leaves `""`, `NotEmpty()` passes, silent bug
- `= default!` on non-generic reference types — use `null!`
- FluentValidation on `Response` — use ctor + `Guard.Against.*`; validation is for user-facing requests

See [nullability.md](references/nullability.md).

### 5. Apply mutability — accessor declares intent

| Intent | Accessor | Collection |
|--------|----------|------------|
| Display / server-built | `{ get; }` or `{ get; init; }` | `IReadOnlyList<T>` |
| Blazor bindable / edit | `{ get; set; }` on `I*Details` | `List<T>` when editable |

Read-only display sharing across endpoints: get-only interfaces (e.g. `IPolicyDto`) — not
bindable, not `I*Details`.

### 6. Response patterns

| Case | Pattern |
|------|---------|
| Display DTO | Parameterized ctor + `Guard.Against.*`; immutable `{ get; }` |
| Editable load | Implements `I*Details`; ctor sets identity; mutable `{ get; set; }` on bindable fields |
| List | `Response : ListResponse<TDto>` |
| No body | `public sealed class Response;` |
| Created id | `public required int Id { get; init; }` or ctor |
| File/stream | `IRequest<OneOf<Stream, SharedProblemDetails>>` |

### 7. Query-string queries

Implement `IQueryStringRouteProvider` + `GetRouteWithQueryString()` for optional filters.
Optional filter properties are `string?` / nullable value types with **no** unconditional
required rules.

### 8. Validator

- Compose shared validators via `SetValidator`.
- Empty validator is valid: `public sealed class Validator : AbstractValidator<Query>;`
- Do **not** add isolated validator unit tests in the contracts test project — FluentValidation
  is tested at integration level.

### 9. Contract tests (required)

In the repo's `*Contracts.Tests` project, add `SerializeAndDeserialize` for
`Command`/`Query` and `Response` using camelCase `JsonSerializerOptions`. This validates
JSON round-trip shape.

Do **not** test validators in isolation here.

### 10. Mock response factory (required)

Every contract needs `GetMockResponseFactory()` and SPA registration. Use the
`mock-response-factory` skill for implementation and wiring details.

## Validation checklist

- [ ] `public static partial class` with nested `Query`/`Command`, `Response`, `Validator`
- [ ] `[RouteMixin]` with correct verb and route constraints (`{Id:min(1)}`, etc.)
- [ ] `IRequest<OneOf<Response, SharedProblemDetails>>`
- [ ] Namespace plural; folder singular
- [ ] Bindable flows use `I*Details` + shared `AbstractValidator<I*Details>`
- [ ] Nullability matches validator rules — no `string?` + unconditional `NotEmpty()`
- [ ] No `string.Empty` or `default!` on required reference types
- [ ] Response invariants enforced in ctor + `Guard`, not FluentValidation
- [ ] Mutability matches binding intent (`set` vs `init`/`get only`)
- [ ] `*Contracts.Tests` serialization round-trip tests
- [ ] `GetMockResponseFactory()` implemented and registered in mock service

## Common pitfalls

| Pitfall | Fix |
|---------|-----|
| `string?` + `NotEmpty()` | Required field → `string` + `= null!` + `NotEmpty()` |
| Separate Blazor view model | Command/Response implement `I*Details`; bind the interface |
| Entity-centric shared DTO per endpoint | Endpoint-centric types; share only validation interfaces or read-only display interfaces |
| `sealed record` request/response | Classes + partial + source generation |
| Namespace matches folder name | Namespace plural; folder singular |
| Hand-declared route params | Trust `RouteMixin` source generation |
| Missing mock factory | Add `GetMockResponseFactory()` — required for SPA mock mode |
| Copying paths from another repo | Read existing contracts in **this** repo first |

## Canonical examples in this skill

See [examples.md](references/examples.md) for inline reference implementations and how to
discover equivalents in the repo you are working in.

## Related skills

- `mock-response-factory` — `GetMockResponseFactory()` on contracts + SPA mock service registration
- `csharp` — formatting and naming only; does not override contract nullability/mutability rules
- `blazor-layout` / `blazor-css-strategy` — UI shell and styling; contracts feed `EditForm` binding
- Do **not** use `dotnet-webapi` for this contract pattern
# How to Build Your BFF API Contracts

## Introduction

This guide is tailored for designing API contracts within the TimeWarp Architecture, where the backend-for-frontend (BFF) approach empowers UX designers to define the structure of API contracts, which will be implemented by the API. This BFF strategy ensures that the APIs are optimized for the specific use cases and workflows of the frontend, streamlining development. This document provides a concise roadmap for creating these API contracts, ensuring they are functional, efficient, and aligned with the frontend needs.

## Contract Structure and Contents

The TimeWarp Architecture prescribes a methodical approach for organizing API contract files. This structured approach facilitates ease of navigation and quick understanding of each API's role and capabilities.

### Contract Feature Folder

All API contract files are located in the `features` directory, nested under the respective feature name.

- **Path**: `features/<pluralized-feature-name>`

  > **Note**: The FeatureName is pluralized to differentiate from class names, representing a group of related functionalities.
  >
  > Example: `features/chart-of-accounts`, `features/users`
  >
  > **Casing**: this repository uses kebab-case paths (`features/admin/roles/`); some older
  > TimeWarp solutions use PascalCase (`Features/Admin/Roles/`). Match the repo you are in —
  > namespaces are PascalCase regardless of path casing.

When features are part of a larger domain, an additional categorization layer is used for clarity.

> Example: `Features/Accounting/ChartOfAccounts`

#### Feature Folder Contents

- **Commands Folder**: Contains command files for write operations (create, update, delete).
  - **Path**: `Features/<PluralizedFeatureName>/Commands`

- **Queries Folder**: Contains query files for read operations.
  - **Path**: `Features/<PluralizedFeatureName>/Queries`

#### Naming the Contract Files Within Folders

- **Commands**: Named with an action verb indicating the operation, like `CreateUser`, `UpdateUser`, or `DeleteUser`.

- **Queries**: Prefixed with "Get" to denote retrieval, like `GetUser` or `GetUsers`.

#### UX Bindable Interfaces

Interfaces designated for binding to UI components in Blazor, such as `EditForm`, are central to the TimeWarp Architecture's approach to contract design. They ensure that UX-driven requests maintain consistent validation and structure throughout the application. The interfaces, aptly named to reflect the domain entities they represent, serve as contracts for form data binding and validation. Typically located alongside the `Commands` and `Queries` within the feature folder, these files streamline front-end development and enforce a single source of truth for validation.

These interfaces facilitate a modular approach by centralizing validation rules, which can be reused across different parts of the frontend, thereby reducing redundancy and streamlining frontend development.

#### Namespace

The namespace should be at the feature level, following the convention:

```csharp
namespace <ProjectName>.Features.<PluralizedFeatureName>
```

> Note: FeatureName should be plural this helps avoid naming conflicts with Classes.
> Example: `namespace TimeWarp.Features.ChartOfAccounts`

> Note: Sometimes Features are grouped and there could be another layer.
> Example: `namespace TimeWarp.Features.Accounting.ChartOfAccounts`

This organization helps in logically grouping vertical slices of functionality across the projects of the solution. 

#### Public Static Partial Class

The `public static partial class` use of the `partial` keyword supports mixin patterns, allowing for extendable code generation without modifying the original class. This separation of generated and custom code promotes a clean and maintainable codebase. The class names follow CRUD operation prefixes. This provides instant clarity on the API's purpose, enabling developers to quickly identify and understand the contract's functionality.

This naming strategy aligns with RESTful design principles, making it easier for new developers to understand the API's functions intuitively.

```csharp
public static partial class GetUser 
public static partial class CreateUser
public static partial class UpdateUser
public static partial class DeleteUser
```

#### Nested Classes

Within the main class, several nested classes define the structure of the API contract:

- **Query/Command**: Represents the request. Named `Query` for read operations or `Command` for create, update, and delete operations. Decorated with `[ApiRoute]` and implementing a request marker interface:
  ```csharp
  [ApiRoute("api/Users/{UserId:guid}", HttpVerb.Get)]
  public sealed partial class Query : IApiRequest, IRequest<OneOf<Response, SharedProblemDetails>>;
  ```
  The `OneOf<Response, SharedProblemDetails>` return type means every request resolves to either
  the success payload or an RFC-7807-style problem details — no exceptions for expected failures.

- **Response**: Defines the shape of the data returned by the API.
  ```csharp
  public sealed class Response : IUserDetails
  ```

- **Validator**: Provides validation rules for the request, ensuring that the data meets expected formats and constraints before processing by the API. These same rules will be evaluated by the API Server to ensure data integrity. Shared `I*Details` rules are composed rather than repeated, and an empty validator is valid:
  ```csharp
  public sealed class Validator : AbstractValidator<Command>
  {
    public Validator() => RuleFor(x => x).SetValidator(new UserDetailsValidator());
  }

  public sealed class Validator : AbstractValidator<Query>;   // no rules — still declared
  ```

#### Route Attributes and Source Generation

The request class must be `partial`: a bundled Roslyn source generator (shipped with
`TimeWarp.Foundation.Contracts`) expands three attributes into contract members. This layer is
what connects the contract to both the HTTP client and the server's FastEndpoint generation.

| Attribute | Generates | Use when |
|-----------|-----------|----------|
| `[ApiRoute("api/…", HttpVerb.X)]` | `RouteTemplate` const, `GetRoute()`, `GetHttpVerb()`, and a typed property per route parameter (`{UserId:guid}` → `Guid UserId`) | Every contract request |
| `[AuthApiRequest]` | `Guid UserId { get; set; }` + private `GetAuthQueryParameters()` | Query-string queries that carry user identity |
| `[OpenDataQueryParameters]` | `Top`/`Skip`/`Filter`/`OrderBy`/`ReturnTotalCount` + private `GetOpenDataQueryParameters()` | Pageable/sortable list queries |

Do **not** hand-declare route parameters — they are generated from the route template. Do declare
any `I*Details` data properties — interface members are not generated.

Supporting request shapes:

- **`IApiRequest` / `IAuthApiRequest`** (foundation interfaces): the base request markers. The
  manual `IAuthApiRequest` form (implement the interface, declare `Guid UserId`) suits POST bodies
  and GET-by-id; the `[AuthApiRequest]` attribute form suits query-string list queries. The server
  must never trust a client-sent `UserId` — it re-derives identity from the auth token.
- **`IQueryStringRouteProvider`**: implement `GetRouteWithQueryString()` for optional filters,
  composing the generated helpers into a `NameValueCollection`.
- **`ListResponse<TDto>`**: base for list responses (`totalCount` + items).
- **Streams/files**: return `IRequest<OneOf<Stream, SharedProblemDetails>>`.

> Older TimeWarp solutions use the pre-rename attribute names `[RouteMixin]`,
> `[IAuthApiRequestMixin]`, `[IOpenDataQueryParametersMixin]` (originally Morris.Moxy mixins).
> Recognize them when reading old code; write the current names in new code.

For the full normative spec — nullability/mutability rules, response patterns, contract tests,
mock factories — see the `web-api-contracts` skill (`skills/web-api-contracts/`).

### Conclusion

Adopting the TimeWarp Architecture's structured approach enhances clarity, manageability, and scalability in application development. It ensures consistency within the current development team and facilitates the onboarding of new developers. The strategic organization and naming conventions lead to an intuitive and maintainable codebase, fostering effective collaboration between frontend and backend teams and streamlining the development process.

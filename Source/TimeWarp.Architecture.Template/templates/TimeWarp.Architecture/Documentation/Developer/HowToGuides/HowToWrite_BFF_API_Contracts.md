# How to Build Your BFF API Contracts

## Introduction

This guide is tailored for designing API contracts within the TimeWarp Architecture, where the backend-for-frontend (BFF) approach empowers UX designers to define the structure of API contracts, which will be implemented by the API. This BFF strategy ensures that the APIs are optimized for the specific use cases and workflows of the frontend, streamlining development. This document provides a concise roadmap for creating these API contracts, ensuring they are functional, efficient, and aligned with the frontend needs.

## Contract Structure and Contents

The TimeWarp Architecture prescribes a methodical approach for organizing API contract files. This structured approach facilitates ease of navigation and quick understanding of each API's role and capabilities.

### Contract Feature Folder

All API contract files are located in the `Features` directory, nested under the respective feature name.

- **Path**: `Features/<FeatureName>`

  > **Note**: The `<FeatureName>` is pluralized to differentiate from class names, representing a group of related functionalities.
  >
  > Example: `Features/ChartOfAccounts`, `Features/Users` 

When features are part of a larger domain, an additional categorization layer is used for clarity.

> Example: `Features/Accounting/ChartOfAccounts`

#### Feature Folder Contents

- **Commands Folder**: Contains command files for write operations (create, update, delete).
  - **Path**: `Features/<FeatureName>/Commands`

- **Queries Folder**: Contains query files for read operations.
  - **Path**: `Features/<FeatureName>/Queries`

#### Naming the Contract Files Within Folders

- **Commands**: Named with an action verb indicating the operation, like `CreateUser`, `UpdateUser`, or `DeleteUser`.

- **Queries**: Prefixed with "Get" to denote retrieval, like `GetUser` or `GetUsers`.

#### UX Bindable Interfaces

Bindable interfaces are defined for requests within a feature. These interfaces can be implemented by requests that are bound to Blazor's `EditForm` components, promoting consistent validation across the frontend. The naming of the interface files should be clear, descriptive, and reflective of the entities they model. They are placed within the feature folder, alongside the `Commands` and `Queries` folders.

#### Namespace

The namespace should be at the feature level, following the convention:

\```csharp
namespace <ProjectName>.Features.<FeatureName>
\```

> Note: FeatureName should be plural this helps avoid naming conflicts with Classes.
> Example: `namespace TimeWarp.Features.ChartOfAccounts`

> Note: Sometimes Features are grouped and there could be another layer.
> Example: `namespace TimeWarp.Features.Accounting.ChartOfAccounts`

This organization helps in logically grouping vertical slices of functionality across the projects of the solution. 

#### Public Sealed Partial Class

Each API contract is encapsulated in a `public sealed partial class`, facilitating separation of concerns and clean organization. The `partial` keyword is utilized to allow for mixin code generation.

The Naming follows typical CRUD operation prefixes such as Get, Create, Delete, Update, reflecting the action the API endpoint will perform:

\```csharp
public sealed partial class GetUser 
public sealed partial class CreateUser
public sealed partial class UpdateUser
public sealed partial class DeleteUser
\```

#### Nested Classes

Within the main class, several nested classes define the structure of the API contract:

- **Query/Command**: Represents the request. Named `Query` for read operations or `Command` for create, update, and delete operations.
  \```csharp
  public sealed partial class Query : IRequest<OneOf<Response, SharedProblemDetails>>
  \```

- **Response**: Defines the shape of the data returned by the API.
  \```csharp
  public sealed class Response : IUserDetails
  \```

- **Validator**: Provides validation rules for the request, ensuring that the data meets expected formats and constraints before processing by the API.
  \```csharp
  public sealed class Validator : AbstractValidator<Query>
  \```

### Conclusion

Adopting the TimeWarp Architecture's structured approach enhances clarity, manageability, and scalability in application development. It ensures consistency within the current development team and eases the integration of new developers, contributing to a cohesive development lifecycle. The thoughtful organization and naming conventions lead to a more intuitive and maintainable codebase, facilitating smoother collaboration between frontend and backend teams.


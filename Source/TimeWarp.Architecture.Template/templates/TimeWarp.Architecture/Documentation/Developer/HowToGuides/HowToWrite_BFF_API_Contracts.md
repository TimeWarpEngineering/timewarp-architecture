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

Interfaces designated for binding to UI components in Blazor, such as `EditForm`, are central to the TimeWarp Architecture's approach to contract design. They ensure that UX-driven requests maintain consistent validation and structure throughout the application. The interfaces, aptly named to reflect the domain entities they represent, serve as contracts for form data binding and validation. Typically located alongside the `Commands` and `Queries` within the feature folder, these files streamline front-end development and enforce a single source of truth for validation.

These interfaces facilitate a modular approach by centralizing validation rules, which can be reused across different parts of the frontend, thereby reducing redundancy and streamlining frontend development.

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

The `public sealed partial class` use of the `partial` keyword supports mixin patterns, allowing for extendable code generation without modifying the original class. This separation of generated and custom code promotes a clean and maintainable codebase. The class names, following CRUD operation prefixes, provide instant clarity on the API's purpose, enabling developers to quickly identify and understand the contract's functionality.

This naming strategy not only enhances discoverability but also aligns with RESTful design principles, making it easier for new developers to understand the API's functions intuitively.

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

### Handling Mutability in API Contracts

The mutability of properties in API contracts within the TimeWarp Architecture should reflect their intended use in the application, ensuring that data integrity and application logic are preserved while providing a seamless user experience.

#### Immutable Members

For API contracts designed to fetch data without expecting any modifications to be sent back to the server, properties should be immutable. This immutability is enforced by setting properties with `{ get; init; }` accessors, which allows assignment only during object initialization. Immutable properties prevent accidental data modifications, which can be crucial for maintaining state consistency, especially in distributed environments.

Example of an immutable model:
\```csharp
public class UserDetails
{
public int UserId { get; init; }
public string UserName { get; init; }
public string Email { get; init; }
}
\```

#### Mutable Members

When an API contract involves data that the frontend might need to modify, such as during CRUD operations in `EditForm` components, properties should be mutable. This allows the frontend to bind input fields directly to these properties and push changes back to the server. Mutability should be clearly indicated by using `{ get; set; }` accessors.

Example of a mutable model:
\```csharp
public class UserUpdateCommand
{
public int UserId { get; init; } // Immutable identifier
public string Email { get; set; } // Mutable for updates
}
\```

#### Collections and Aggregate Data

Handling collections and aggregates requires careful consideration of their roles within the application. If a collection is part of a larger aggregate that is fetched for display purposes only, it should be exposed as an `IReadOnlyList<T>` to ensure it remains unmodifiable from the client side. Conversely, if the collection needs to be editable as part of a transactional operation, it should be mutable and often paired with appropriate validation logic to manage changes.

Example of managing collection mutability:
\```csharp
public class ProjectDetails
{
public int ProjectId { get; init; }
public IReadOnlyList<TaskDetails> Tasks { get; init; } // Immutable collection for display
}

public class ProjectUpdateCommand
{
public int ProjectId { get; init; }
public List<TaskUpdateInfo> Tasks { get; set; } // Mutable collection for updates
}
\```

#### Interface-Driven Mutability

Since interfaces often drive the binding and validation in frontend frameworks like Blazor, ensuring that the interfaces reflect the correct level of mutability is essential. Interfaces should clearly distinguish between fields that are meant to be displayed and those intended for user interaction and modification.

By adhering to these principles, API contracts within the TimeWarp Architecture ensure that data flows are appropriately controlled, reducing errors and enhancing the robustness of the application.

### Conclusion

Understanding and implementing the correct mutability settings in API contracts is crucial for maintaining data integrity and providing a flexible yet secure user interface. The TimeWarp Architecture's approach to clearly defined mutability helps streamline frontend interactions and backend processes, leading to more maintainable and error-resistant code.



### Conclusion

Adopting the TimeWarp Architecture's structured approach enhances clarity, manageability, and scalability in application development. It ensures consistency within the current development team and facilitates the onboarding of new developers. The strategic organization and naming conventions lead to an intuitive and maintainable codebase, fostering effective collaboration between frontend and backend teams and streamlining the development process.

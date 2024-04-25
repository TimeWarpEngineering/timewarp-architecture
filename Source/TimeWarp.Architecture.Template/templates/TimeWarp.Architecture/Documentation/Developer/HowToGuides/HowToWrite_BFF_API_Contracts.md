# How to Build Your BFF API Contracts

## Introduction

This guide is tailored for designing API contracts within the TimeWarp Architecture, where the backend-for-frontend (BFF) approach empowers UX designers to define the structure of API contracts, which will be implemented by the API. This BFF strategy ensures that the APIs are optimized for the specific use cases and workflows of the frontend, streamlining development. This document provides a concise roadmap for creating these API contracts, ensuring they are functional, efficient, and aligned with the frontend needs.

### Contract Structure and Contents

When constructing API contracts, especially within the TimeWarp Architecture, maintaining a clear and simple structure is crucial. This ensures that both the development team and UX designers can easily understand and work with the API. The example provided exemplifies a well-structured approach.

#### Example Explained

\```csharp
namespace Copic.Features.Admin.SecurityRoles;

public sealed partial class GetSecurityRole
{
[UsedImplicitly]
[RouteMixin("api/SecurityRoles/{SecurityRoleId:min(1)}", HttpVerb.Get)]
public sealed partial class Query : IAuthApiRequest, IRequest<OneOf<Response, SharedProblemDetails>>
{
public Guid UserId { get; set; }
}

public sealed class Response: ISecurityRoleDetails
{
public int Id { get; init; }
public Guid Guid { get; init; }
public Guid B2CGroupId { get; init; }
public string? Name { get; set; }
public string? Description { get; set; }
public string? Code { get; set; }
}

// No validator needed for this query
public sealed class Validator : AbstractValidator<Query>;
}
\```

### Key Components

1. **Namespace and Class Structure**:
  - **Namespace**: `Copic.Features.Admin.SecurityRoles` clearly indicates the functional area within the application, helping developers understand the context.
  - **Class**: `GetSecurityRole` is descriptive, indicating that the class is responsible for fetching security role details.

2. **Query Class**:
  - **Annotations**: Use of `[RouteMixin]` and attributes like `[UsedImplicitly]` provide metadata that enhances API discoverability and tooling support.
  - **Interfaces**: Implements `IAuthApiRequest` and `IRequest<OneOf<Response, SharedProblemDetails>>`, which denote authentication requirements and response handling. This structure demonstrates how the API handles successful responses and problems, promoting robust error management.

3. **Response Class**:
  - **Interface Implementation**: Implements `ISecurityRoleDetails`, suggesting a standardized response structure that other parts of the application can rely on.
  - **Properties**: Properties are straightforward, using nullable types where appropriate to indicate optional fields, further adding clarity on what information may or may not be available.

4. **Validator Class**:
  - **Purpose**: Even though it’s mentioned that no validator is needed, the existence of a `Validator` class placeholder suggests where validation logic would be implemented if necessary, preparing for future needs without complicating the current implementation.

### Guidelines for Contract Structure

- **Consistency**: Use consistent naming conventions and structures across different API contracts to reduce learning curves and mistakes.
- **Simplicity**: Avoid over-engineering. Implement what is necessary for the current requirements, but design with future enhancements in mind.
- **Clarity in Annotations**: Utilize annotations to clearly define routes, parameters, and other metadata, which aids in both development and the use of automated tools for API management.

By following these principles, API contracts within the TimeWarp Architecture will not only meet the current functional requirements but also remain maintainable and scalable, accommodating changes with minimal disruption.

### Conclusion

The given structure exemplifies how API contracts should be designed to ensure they are easily understood, implemented, and maintained. Each element of the contract serves a clear purpose, from defining access and response behavior to specifying how data should be validated, all contributing to a coherent and effective API strategy.








## 1. Define Your Data Models

### Key Principles

- **Clarity and Simplicity**: Ensure your data models are easy to understand and use.
- **Consistency**: Use consistent naming and data types across your API to reduce confusion.

### Example

```csharp
public class User
{
  public int UserId { get; init; }
  public string FirstName { get; set; }
  public string LastName { get; set; }
}
```

## 2. Choose the Right Data Structures

### Immutability vs. Mutability

- **Immutable Objects**: Use when you want to ensure the object cannot be modified after creation. This is crucial for maintaining state integrity across distributed systems.
- **Mutable Objects**: Use when the object needs to be updated, such as in CRUD operations.

### Example

```csharp
public interface IUserDetails
{
int UserId { get; init; }
string FirstName { get; set; }
}
```

## 3. Implementing Interfaces for Flexibility

### Benefits of Interfaces

- **Flexibility**: Interfaces allow for multiple implementations and easier modifications of the API.
- **Abstraction**: They abstract implementation details, focusing on what operations are possible rather than how they are performed.

### Example

```csharp
public interface IUserDetails
{
int UserId { get; }
string FirstName { get; }
string LastName { get; }
}
```

## 4. Handling Collections

### Mutable vs. Immutable Collections

- **Use \`IReadOnlyList<T>\`: When exposing data that should not be modified by the consumer.
- **Use \`List<T>\` internally**: To maintain flexibility within the service layer.

### Example

```csharp
public class UserDetails : IUserDetails
{
private List<string> _roles = new List<string>();
public IReadOnlyList<string> Roles => _roles.AsReadOnly();
}
```

## 5. Documenting API Contracts

### Importance of Documentation

- **Transparency**: Clear documentation ensures that all parties understand the API’s capabilities and limitations.
- **Ease of Use**: Well-documented APIs are easier to integrate and use effectively.

### Tools and Practices

- **Swagger/OpenAPI**: Use tools like Swagger to automatically generate documentation and provide interactive API explorers.
- **Comments and Examples**: Include comments and usage examples in your code to clarify how the API should be used.

## Conclusion

Building effective API contracts requires careful consideration of the data models, structures, and the communication between components. By following best practices such as using interfaces and choosing appropriate data structures, you can create robust and flexible APIs that are easy to manage and evolve.

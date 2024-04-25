### Handling Mutability in BFF API Contracts

The mutability of properties in API contracts within the TimeWarp Architecture should reflect their intended use in the application, ensuring that data integrity and application logic are preserved while providing a seamless user experience.

#### Immutable Members

For API contracts designed to fetch data without expecting any modifications to be sent back to the server, properties should be immutable. This immutability is enforced by setting properties with `{ get; init; }` accessors, which allows assignment only during object initialization. Immutable properties prevent accidental data modifications.

Example of an immutable Dto:

```csharp
public sealed class UserDto
{
  public int UserId { get; init; }
  public string UserName { get; init; }
  public string Email { get; init; }
}
```

#### Mutable Members

When an API contract involves data that the frontend might need to modify, such as during CRUD operations in `EditForm` components, properties should be mutable. This allows the frontend to bind input fields directly to these properties and push changes back to the server. Mutability should be clearly indicated by using `{ get; set; }` accessors.

Example of a mutable model:
```csharp
public interface IUserDetails
{
  public string Email { get; set; } // Mutable
  public string FirstName { get; set; } // Mutable
  public string LastName { get; set; } // Mutable
}

public sealed class UserDto : IUserDetails
{
  public int UserId { get; init; }
  public string Email { get; set; } // Mutable
  public string FirstName { get; set; } // Mutable
  public string LastName { get; set; } // Mutable
}
```

#### Collections and Aggregate Data

Handling collections and aggregates requires careful consideration of their roles within the application. If a collection is part of a larger aggregate that is fetched for display purposes only, it should be exposed as an `IReadOnlyList<T>` to ensure it remains unmodifiable from the client side. Conversely, if the collection needs to be editable as part of a transactional operation, it should be mutable and often paired with appropriate validation logic to manage changes.

Example of immutable collection:
```csharp
public sealed class ProjectDto
{
public int ProjectId { get; init; }
public IReadOnlyList<TaskDetails> Tasks { get; init; } // Display only
}
```

Example of mutable collection:
```csharp
public class ProjectDto
{
public int ProjectId { get; init; }
public List<TaskUpdateInfo> Tasks { get; set; } // Editable for updates
}
```

#### Interface-Driven Mutability

Since interfaces drive the binding and validation in the Blazor frontend, ensuring that the interfaces reflect the correct level of mutability is essential. Interfaces should clearly distinguish between fields that are meant to be displayed and those intended for user interaction and modification.

By adhering to these principles, API contracts within the TimeWarp Architecture ensure that data flows are appropriately controlled, reducing errors and enhancing the robustness of the application.

### Conclusion

Understanding and implementing the correct mutability settings in API contracts is crucial for maintaining data integrity and providing a flexible yet secure user interface. The TimeWarp Architecture's approach to clearly defined mutability helps streamline frontend interactions and backend processes, leading to more maintainable and error-resistant code.

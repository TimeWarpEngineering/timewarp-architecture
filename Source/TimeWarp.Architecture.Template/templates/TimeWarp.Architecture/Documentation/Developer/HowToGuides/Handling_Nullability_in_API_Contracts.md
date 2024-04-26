### Handling Nullability in API Contracts

Properly managing the nullability of properties in API contracts is essential to convey the intent of the contract and ensure the robustness of the application. This guide provides best practices for handling nullable and non-nullable properties in request and response models, along with recommendations for constructor usage in API contracts. 

#### Nullability in Request Models

When a request model property may not be validated for non-null values, it should be declared as nullable (`string?`). This makes it explicit that the property may contain a `null` value, and programmers must handle it accordingly.

Example of nullable property in request model:

```csharp
public sealed partial class UpdateUser 
{  
  public class Command
  {
    ...
    public string? OptionalNickname { get; set; } // This property might be null if not provided
    ...
  }
}
```

If the request model's `Validator` ensures a property is non-null, then the property can safely be declared as non-nullable. However, to satisfy the C# compiler's requirement for non-nullable reference types at the point of object creation, we use the null-forgiving operator (`!`). This approach is used under the assumption that the property will be set to a valid, non-null value immediately after object construction and before it is utilized.

Example of non-nullable property initialized using null-forgiving operator:

```csharp
public sealed partial class UpdateUser
  public class Command
  {
    public string MandatoryEmail { get; set; } = default!;
  }
  public class Validator : AbstractValidator<Command>
  {
    public Validator()
    {
      RuleFor(x => x.MandatoryEmail).NotEmpty();
    }
  }
}
```

#### Constructor Strategies in Response Models

For response models, it is critical to ensure that properties which should not be null are handled correctly to prevent sending inconsistent data to clients. A parameterized constructor can be used to enforce non-null properties by throwing an exception if a null value is passed to a property that shouldn't be null.

Example of response model with a parameterized constructor:

```csharp
public class Response
{
  public string Email { get; init; }
  public string Name { get; init; }
  
  public Response(string email, string name)
  {
    Email = email ?? throw new ArgumentNullException(nameof(email));
    Name = name ?? throw new ArgumentNullException(nameof(name));
  }
}
```

By following these guidelines, the mutability and nullability of properties in the API contracts can be effectively managed, further improving the robustness and reliability of the application within the TimeWarp Architecture.

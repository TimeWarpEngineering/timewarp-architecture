### Handling Nullability in API Contracts

Properly managing the nullability of string properties in API contracts is essential to avoid runtime errors and ensure that the API's behavior is predictable and consistent. This section outlines the recommended approaches to handle nullable and non-nullable string properties, along with the best practices for constructor usage in request and response models.

#### Nullability in Request Models

When a request model property is a string that may not be validated for non-null values, it should be declared as nullable (`string?`). This makes it explicit that the property may contain a `null` value, and programmers must handle it accordingly.

Example of nullable property in request model:

```csharp
public class UserRequest
{
public string? OptionalNickname { get; set; } // This property might be null if not provided
}
```

If the request model's validator ensures a property is non-null, then the property can safely be declared as non-nullable. It is a good practice to initialize these properties with a default value to satisfy the C# compiler's requirement for non-nullable references.

Example of non-nullable property with default value:

```csharp
public class UserRequest
{
public string MandatoryEmail { get; set; } = string.Empty; // Non-null with a default value
}
```

#### Constructor Strategies in Response Models

For response models, it is critical to ensure that properties which should not be null are handled correctly to prevent sending inconsistent data to clients. A parameterized constructor can be used to enforce non-null properties by throwing an exception if a null value is passed to a property that shouldn't be null.

Example of response model with a parameterized constructor:

```csharp
public class UserResponse
{
public string Email { get; init; }
public string Name { get; init; }

public UserResponse(string email, string name)
{
Email = email ?? throw new ArgumentNullException(nameof(email));
Name = name ?? throw new ArgumentNullException(nameof(name));
}

[Obsolete("Serialization only")]
public UserResponse() {}
}
```

Additionally, mark the parameterless constructor as `[Obsolete("Serialization only")]` to indicate that it should only be used for deserialization purposes. It is also worthwhile to verify if System.Text.Json (STJ) can utilize the parameterized constructor during deserialization to streamline the process and enhance safety.

By following these guidelines, the mutability and nullability of properties in the API contracts can be effectively managed, further improving the robustness and reliability of the application within the TimeWarp Architecture.



--- original content  From Pete ---

If the validator for the request does not ensure a non-null value for a string property, then the property should be declared as `string?` so the programmer using that property is aware that null is a possible value.

If the request validator does ensure a non-null value then the programmer can assume the value of the string is not null because it has been validated before reaching the code being executed. In this scenario, it is necessary to provide a default value for the property to the C# compiler - in which case the property should be assigned with a forgiving null.


---- Petes method ----
Requests coming in to the API are validated, so I am confident they are not null. So these can have parameterless constructors.

Responses going out from the API use a parameterised constructor that throws an exception if something that should not be null receives a null value - and I have a parameterless constructor for deserialization purposes which I mark as [Obsolete("Serialization only")] but check if STJ will actually use the parameterised one so you don't need to worry about doing that.

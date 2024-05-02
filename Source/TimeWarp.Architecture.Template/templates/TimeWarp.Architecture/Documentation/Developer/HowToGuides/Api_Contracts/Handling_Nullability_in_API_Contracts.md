### Handling Nullability in API Contracts

Understanding the differences between nullable value types and nullable reference types is crucial in C# programming. Nullable value types are represented by `Nullable<T>` and are explicitly nullable in the code. Conversely, nullable reference types are a feature introduced in C# 8.0 that provide compiler hints, enabling the compiler to identify and warn about potential null reference operations.

Properly managing the nullability of properties in API contracts is essential to convey the intent of the contract and ensure the robustness of the application. This guide provides best practices for handling nullable and non-nullable properties in requests and responses, along with recommendations for constructor usage in API contracts. 

#### Nullability in Requests

When a `Request` property may not be validated for non-null values, it should be declared as nullable (`string?`). This makes it explicit that the property may contain a `null` value, and programmers must handle it accordingly.

Example of nullable property in a `Command`:

```csharp
public static partial class UpdateUser 
{  
  public sealed partial class Command
  {
    ...
    public string? OptionalNickname { get; set; } // This property might be null if not provided
    ...
  }
}
```

If the `Request`'s `Validator` ensures a property is non-null, then the property can safely be declared as non-nullable. However, to satisfy the C# compiler's requirement for non-nullable reference types at the point of object creation, we use the null-forgiving operator (`!`). This approach allows us to inform the compiler we know the value will be set by the time it is first read (e.g. after deserialization) without us actually having to set it to a non-null value.

Setting the property to a non-null value by default (e.g. empty string) is not good practice; to save payload size, some json requests will exclude properties that have a null value, the receiver will then not assign a value to the property - resulting in the property retaining its incorrect non-null default rather than being null as intended. Any NotNull validation will pass and the system will continue as if the requester had explicitly informed us a value should be (the value we used in code, "").

Example of non-nullable property initialized using null-forgiving operator:

```csharp
public static partial class UpdateUser
  public sealed partial class Command
  {
    public string MandatoryEmail { get; set; } = null!;
  }
  public sealed class Validator : AbstractValidator<Command>
  {
    public Validator()
    {
      RuleFor(x => x.MandatoryEmail).NotEmpty();
    }
  }
}
```

##### Avoid Using `default` with Non-Nullable Properties
  
  When initializing non-nullable properties, avoid using the `default` keyword, as it can lead to unexpected behavior. The `default` keyword returns the default value of a type, which is `null` for reference types. Whereas value types have a default value of `0` or `false`, depending on the type. Using `default` with non-nullable properties can lead to confusion and potential bugs in the code. 
 
> Note: Only use `default` with generic types `<T>`, since the actual default value is type-dependent and cannot always be known in advance.

Do this:
```csharp
public string MandatoryEmail { get; set; } = null!;
public Person Person { get; set; } = null!;
public T GenericProperty { get; set; } = default!;
```
Don't do this:
```csharp
public string MandatoryEmail { get; set; } = default!;
public Person Person { get; set; } = default!;
```

#### Constructor Strategies in Responses

For `Response`s, it is critical to ensure that properties which should not be null are handled correctly to prevent sending inconsistent data to clients. A parameterized constructor can be used to enforce non-null properties by throwing an exception if a null value is passed to a property that shouldn't be null.

Example of `Response` with a parameterized constructor:

```csharp
public sealed class Response
{
  public string Email { get; }
  public string Name { get;  }
  
  public Response(string email, string name)
  {
    Email = Guard.Against.NullOrWhiteSpace(email); 
    Name = Guard.Against.NullOrWhiteSpace(name);
  }
}
```

By following these guidelines, the mutability and nullability of properties in the API contracts can be effectively managed, further improving the robustness and reliability of the application within the TimeWarp Architecture.


### FAQ

**Question:** Why not use Fluent validation for Responses?

**Answer:** Validation is designed to provide user-friendly feedback, while exceptions aim to alert developers to code issues. Using Fluent validation for responses would involve additional CPU processing to generate user-friendly errors, which isn't necessary since the main audience for these messages are developers who can interpret technical details like stack traces. Therefore, we avoid the extra computational overhead by not using Fluent validation in responses.




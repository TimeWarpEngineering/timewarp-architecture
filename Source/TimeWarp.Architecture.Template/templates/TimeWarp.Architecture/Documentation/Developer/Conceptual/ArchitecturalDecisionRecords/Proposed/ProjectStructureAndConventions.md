# Contracts Project Structure and Conventions

In this project, we have adopted a feature-based folder structure, grouped by entity, with nested classes for organizing commands, queries, and their corresponding response and validation classes. We also use `OneOf` for handling multiple response types and `FluentValidation` for request validation.

## Folder Structure

The folder structure is organized by entity, with separate folders for Commands and Queries, and then further divided by specific commands or queries:

```
Features/
├── Entity/
│   ├── Commands/
│   │   ├── Command1/
│   │   │   ├── Command1.Command.cs
│   │   │   ├── Command1.SuccessResponse.cs
│   │   │   └── Command1.Validator.cs
│   │   └── Command2/
│   │       ├── Command2.Command.cs
│   │       ├── Command2.SuccessResponse.cs
│   │       └── Command2.Validator.cs
│   └── Queries/
│       ├── Query1/
│       │   ├── Query1.Query.cs
│       │   ├── Query1.SuccessResponse.cs
│       │   └── Query1.Validator.cs
│       └── Query2/
│           ├── Query2.Query.cs
│           ├── Query2.SuccessResponse.cs
│           └── Query2.Validator.cs
```

## Nested Classes

We use nested classes to group related classes within their respective command or query classes. One can use partial files (show above) if desired. This approach helps to keep the code organized and maintain a close relationship between the classes.

```csharp
public partial class CommandOrQuery1
{
    public class Command : IRequest<OneOf<SuccessResponse, ProblemDetails>>
    {
        // ... properties
    }

    public class SuccessResponse
    {
        public UserDto User { get; set; }
    }

    public class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            // ... validation rules
        }
    }

    public class UserDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        // ... other user properties
    }
}
```

## OneOf and FluentValidation

For handling multiple response types, we use the `OneOf` library, which allows us to represent a response as one of multiple possible types, making it easier to work with different outcomes.

We use `FluentValidation` for validating request objects. By integrating validators with the nested classes, we can maintain a clear relationship between the request and its validation rules. Validators are implemented using the `AbstractValidator<T>` class provided by FluentValidation.

## Key Takeaways

- Feature-based folder structure, grouped by entity.
- Commands and Queries separated into their respective folders.
- Nested classes to organize commands, queries, responses, and validators.
- OneOf for handling multiple response types.
- FluentValidation for request validation.

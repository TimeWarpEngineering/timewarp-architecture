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
│   │   │   ├── Command1.Response.cs
│   │   │   └── Command1.Validator.cs
│   │   └── Command2/
│   │       ├── Command2.Command.cs
│   │       ├── Command2.Response.cs
│   │       └── Command2.Validator.cs
│   └── Queries/
│       ├── Query1/
│       │   ├── Query1.Query.cs
│       │   ├── Query1.Response.cs
│       │   └── Query1.Validator.cs
│       └── Query2/
│           ├── Query2.Query.cs
│           ├── Query2.Response.cs
│           └── Query2.Validator.cs
```

### SignalR Realtime Bidrectional

If using SignalR in the solution the Contracts for the respective Hubs will be organzied similiarly by entity but with seperate folders ClientToServer and ServerToClient.

```
Features/
├── Entity/
│   ├── Commands/
│   │   ├── Command1/
│   │   │   ├── Command1.Command.cs
│   │   │   ├── Command1.Response.cs
│   │   │   ├── Command1.<X>Dto.cs
│   │   │   └── Command1.Validator.cs
│   │   └── Command2/
│   │       ├── Command2.Command.cs
│   │       ├── Command2.Response.cs
│   │       └── Command2.Validator.cs
│   ├── Queries/
│   │   ├── Query1/
│   │   │   ├── Query1.Query.cs
│   │   │   ├── Query1.Response.cs
│   │   │   └── Query1.Validator.cs
│   │   └── Query2/
│   │       ├── Query2.Query.cs
│   │       ├── Query2.Response.cs
│   │       └── Query2.Validator.cs
│   ├── ClientToServer/
│   │   ├── Request1/
│   │   │   ├── Request1.Command.cs
│   │   │   ├── Request1.Response.cs
│   │   │   ├── Request1.<X>Dto
│   │   │   └── Request1.Validator.cs
│   │   └── Request2/
│   │       ├── Request2.Query.cs
│   │       ├── Request2.Response.cs
│   │       ├── Request2.<X>Dto.cs
│   │       └── Request2.Validator.cs
│   └── ServerToClient/
│   │   ├── Request1/
│   │   │   ├── Request1.Command.cs
│   │   │   ├── Request1.Response.cs
│   │   │   ├── Request1.<X>Dto
│   │   │   └── Request1.Validator.cs
│   │   └── Request2/
│   │       ├── Request2.Query.cs
│   │       ├── Request2.Response.cs
│   │       ├── Request2.<X>Dto.cs
│   │       └── Request2.Validator.cs
```

## Namespaces

## Namespaces

Namespaces should be ordered by most significant to least significant. Consider using the following pattern: `<Container>.<Feature>.<Entity>.<Assembly>` instead of `<Container>.<Assembly>.<Feature>.<Entity>.<Folder>`. The Feature is more significant than the Assembly, and the Container is the most significant, as they are not supposed to have any dependencies at all.

\```
<Container>.<Feature>.<Entity>.<Assembly>
\```

## Nested Classes

We use nested classes to group related classes within their respective command or query classes. One can use partial files (show above) if desired. This approach helps to keep the code organized and maintain a close relationship between the classes.

```csharp
public partial class Query1
{
    public class Query : IRequest<OneOf<SuccessResponse, ProblemDetails>>
    {
        // ... properties
    }

    public class SuccessResponse
    {
        public MyExampleDto MyExample { get; set; }
    }

    public class Validator : AbstractValidator<Query>
    {
        public Validator()
        {
            // ... validation rules
        }
    }

    public class MyExampleDto
    {
        // ... properties
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

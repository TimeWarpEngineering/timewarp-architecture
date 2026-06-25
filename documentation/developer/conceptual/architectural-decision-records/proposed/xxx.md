# Project Structure and Conventions

In this project, we follow a specific convention for organizing the codebase and implementing features. This convention involves grouping files by entity and using a nested class structure with partial classes, OneOf for handling multiple response types, and FluentValidation for validating requests.

## File and Folder Structure

The file and folder structure is organized as follows:

```plaintext
Features/
├── EntityName/
│   ├── FeatureName/
│   │   ├── FeatureName.Command.cs
│   │   ├── FeatureName.SuccessResponse.cs
│   │   └── FeatureName.Validator.cs
```

- The `EntityName` folder groups the files related to a specific entity.
- The `FeatureName` folder contains separate files for each nested class, including the Command or Query, SuccessResponse, and Validator.

## Nesting of Classes

We use a nested class structure with partial classes to keep related classes together while allowing each class to be defined in a separate file.

For example, in the `FeatureName.Command.cs` file:

```csharp
public partial class FeatureName
{
    public class Command : IRequest<OneOf<SuccessResponse, ProblemDetails>>
    {
        // ... properties
    }
}
```

## OneOf

We use the OneOf library to handle multiple response types in a single response object. OneOf allows us to define a response type that can be one of several specified types, making it easier to work with different outcomes for a single request.

For example, in a Command class:

```csharp
public class Command : IRequest<OneOf<SuccessResponse, ProblemDetails>>
{
    // ... properties
}
```

## FluentValidation

FluentValidation is used to validate requests. Validators are created as nested classes within the respective Command or Query classes and use FluentValidation's rule-based syntax to define validation rules.

For example, in the `FeatureName.Validator.cs` file:

```csharp
using FluentValidation;

public partial class FeatureName
{
    public class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.PropertyName).NotEmpty().WithMessage("Property is required.");
            // Add more rules as needed
        }
    }
}
```

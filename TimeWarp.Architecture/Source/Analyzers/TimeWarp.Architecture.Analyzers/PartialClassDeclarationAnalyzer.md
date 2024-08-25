# TWPA0001: Partial Class Declaration Analyzer

## Summary

The Partial Class Declaration Analyzer ensures that partial classes follow a consistent declaration pattern across multiple files. It enforces rules for primary and secondary partial class declarations to maintain code organization and clarity.

## Rules

1. The primary file should:
  - Be named exactly like the class (e.g., `MyClass.cs` for `MyClass`)
  - Include full access specifiers (public, internal, etc.)
  - Contain any class inheritance

2. Secondary files should:
  - Be named with the class name as a prefix (e.g., `MyClass.PartialImplementation.cs`)
  - Only use the `partial` keyword without additional specifiers
  - Not include any class inheritance
  - Can implement interfaces

## Examples

### Correct Usage

#### Primary File (MyClass.cs)

```csharp
public sealed partial class MyClass : BaseClass, IMyInterface
{
    // Primary implementation
}
```

#### Secondary File (MyClass.PartialImplementation.cs)

```csharp
partial class MyClass
{
    // Additional implementation
}
```

#### Secondary File with Interface (MyClass.AnotherInterface.cs)

```csharp
partial class MyClass : IAnotherInterface
{
    // Implementation of IAnotherInterface
}
```

### Incorrect Usage (Will Trigger Diagnostics)

#### Incorrect Primary File (MyClass.cs)

```csharp
partial class MyClass // Missing access modifier
{
    // Primary implementation
}
```

#### Incorrect Secondary File (MyClass.AdditionalStuff.cs)

```csharp
public partial class MyClass : AnotherBaseClass // Excessive specifier and class inheritance
{
    // Additional implementation
}
```

#### Incorrectly Named File (IncorrectFileName.cs)

```csharp
partial class MyClass
{
    // This will trigger a diagnostic due to incorrect file naming
}
```

## Diagnostic Messages

- "Partial class '{0}' should have full specifiers in the primary file"
- "Partial class '{0}' should have minimal specifiers in secondary files"
- "Partial class '{0}' should not include class inheritance in secondary files"
- "Partial class '{0}' file name '{1}' does not follow the expected naming convention"

## Rationale

This analyzer helps maintain a consistent structure for partial classes across a project. By enforcing these rules, it ensures that:

1. The primary file serves as the main definition of the class, including its access level and class inheritance.
2. Secondary files are clearly identifiable and don't introduce conflicting class structures.
3. Interface implementations can be organized into separate files for better code organization.
4. File naming conventions are consistent, making it easier to locate and understand partial class implementations.

## How to Fix Violations

- For primary files:
  - Ensure the file is named exactly like the class.
  - Add appropriate access modifiers (public, internal, etc.).
  - Move any class inheritance to this file.

- For secondary files:
  - Rename the file to start with the class name followed by a descriptive suffix.
  - Remove any access modifiers, keeping only the `partial` keyword.
  - Remove any class inheritance, but keep interface implementations if present.

By following these guidelines, you'll ensure that your partial classes are well-organized and conform to the project's coding standards.

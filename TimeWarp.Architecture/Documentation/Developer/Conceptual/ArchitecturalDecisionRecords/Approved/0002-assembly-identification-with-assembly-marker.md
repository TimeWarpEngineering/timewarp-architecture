# Standardizing Assembly Identification with AssemblyMarker

* Status: accepted
* Deciders: Architect
* Consulted: Amina
* Date: 2024-03-05
* Updated: 2025-06-25 (Changed from sealed class to interface pattern)

## Context and Problem Statement

In our component-based application, we need a consistent and reliable way to identify and perform reflection-based operations across various assemblies. How can we standardize assembly identification to facilitate easy assembly scanning and other reflection-based tasks?

## Decision Drivers

* Consistency across all assemblies
* Minimize maintenance overhead
* Improve code readability and discoverability
* Avoid naming conflicts and enhance clarity

## Considered Options

* Using a `sealed` class named `AssemblyMarker` in each assembly
* Using an `interface` named `IAssemblyMarker` in each assembly
* Using custom attributes to mark assemblies
* Relying on naming conventions without a specific marker

## Decision Outcome

**Updated 2025-06-25**: Changed to "Using an `interface` named `IAssemblyMarker` in each assembly" after discovering that sealed classes cannot be used as generic type arguments in scenarios like FluentValidation's `AddValidatorsFromAssemblyContaining<T>()` method.

Original decision: "Using a `sealed` class named `AssemblyMarker` in each assembly", because it provides a clear, consistent, and low-overhead method for marking assemblies for reflection-based operations, aligns with .NET best practices, and avoids the complexity and potential limitations of the other options.

**Revised rationale**: The interface pattern maintains all the benefits of the sealed class approach while being compatible with generic type constraints that require reference types. This ensures the assembly marker can be used in all reflection scenarios without compilation errors.

### Positive Consequences

* Provides a clear and consistent mechanism for identifying assemblies
* Simplifies reflection-based operations like assembly scanning
* Enhances code discoverability and maintainability
* Avoids the ambiguity and potential errors associated with less explicit marking methods

### Negative Consequences

* Requires adding a small piece of code to each assembly, which could be considered overhead

## Pros and Cons of the Options

### Using an `interface` named `IAssemblyMarker` in each assembly (Current Choice)

* Good, because it clearly signifies the assembly's purpose and contents
* Good, because it is simple to implement and requires minimal code
* Good, because it avoids the use of reflection for identifying custom attributes, which can be more error-prone and less performant
* Good, because it works as a generic type argument in all scenarios (unlike sealed classes)
* Good, because interfaces prevent instantiation, reinforcing the marker-only purpose
* Bad, because it introduces a small amount of additional code to each assembly

### Using a `sealed` class named `AssemblyMarker` in each assembly (Original Choice)

* Good, because it clearly signifies the assembly's purpose and contents
* Good, because it is simple to implement and requires minimal code
* Good, because it avoids the use of reflection for identifying custom attributes, which can be more error-prone and less performant
* Bad, because it introduces a small amount of additional code to each assembly
* Bad, because sealed classes cannot be used as generic type arguments in some scenarios

### Using custom attributes to mark assemblies

* Good, because it can carry additional metadata if required
* Bad, because it requires more complex reflection code to identify and read the attributes
* Bad, because it may not be as immediately visible or discoverable as a class

### Relying on naming conventions without a specific marker

* Good, because it requires no additional code
* Bad, because it is prone to errors and inconsistencies
* Bad, because it lacks explicitness and clarity, making it harder for new developers to understand and maintain

## Code

**Updated Implementation (2025-06-25)**: The file `AssemblyMarker.cs` should be added to each assembly using the interface pattern:

```csharp
/// <summary>
/// Serves as a marker for the assembly, facilitating easy identification and reflection-based operations.
/// </summary>
/// <remarks>
/// This interface is intended to be used as a reference point within the assembly for scenarios such as assembly scanning,
/// where a stable, known type is required to locate the assembly at runtime. The interface prevents instantiation,
/// reinforcing its role as a simple marker.
/// </remarks>
public interface IAssemblyMarker;
```

**Original Implementation**: The sealed class pattern (no longer used):

```csharp
/// <summary>
/// Serves as a marker for the assembly, facilitating easy identification and reflection-based operations.
/// </summary>
/// <remarks>
/// This class is intended to be used as a reference point within the assembly for scenarios such as assembly scanning,
/// where a stable, known type is required to locate the assembly at runtime. The class is sealed to indicate it is not
/// designed for inheritance or extension, reinforcing its role as a simple marker.
/// </remarks>
public sealed class AssemblyMarker;
```

## Usage Examples

```csharp
// FluentValidation assembly scanning (requires interface pattern)
services.AddValidatorsFromAssemblyContaining<IAssemblyMarker>();

// Assembly identification
var assembly = typeof(IAssemblyMarker).Assembly;
var assemblyName = assembly.GetName().Name;
```

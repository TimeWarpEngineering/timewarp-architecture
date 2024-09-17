# Task 002: Implement the Generic Host

## Description

Set up the .NET Generic Host in the console application to manage the application lifecycle, configuration, and logging. This will provide a robust foundation for integrating Dependency Injection and other services in subsequent tasks.

## Requirements

- Add the `Microsoft.Extensions.Hosting` package to the project.
- Modify `Program.cs` to use the Generic Host, adhering to the project's coding standards.
- Ensure the application runs successfully using the Generic Host.
- Follow the coding standards specified in `.ai/coding-standards.md`.

## Checklist

- **Add Hosting Package**
    - [ ] Install the `Microsoft.Extensions.Hosting` NuGet package:
      ```pwsh
      dotnet add Source/ConsoleApp/ConsoleApp.csproj package Microsoft.Extensions.Hosting
      ```
- **Configure Generic Host**
    - [ ] Modify `Program.cs` to use the Generic Host builder, following the coding standards:
      ```csharp
      // File: Program.cs
      namespace ConsoleApp;
  
      using System.Threading.Tasks;
      using Microsoft.Extensions.Hosting;
  
      public class Program
      {
        public static async Task Main(string[] args)
        {
          using IHost host = Host.CreateDefaultBuilder(args)
            .ConfigureServices((context, services) =>
            {
              // TODO: Register services here in future tasks
            })
            .Build();
  
          await host.RunAsync();
        }
      }
      ```
- **Verify Application**
    - [ ] Run the application to ensure it works correctly with the Generic Host implemented:
      ```pwsh
      dotnet run --project Source/ConsoleApp/ConsoleApp.csproj
      ```
- **Update Documentation**
    - [ ] Add notes to `Documentation/README.md` about the integration of the Generic Host.
- **Follow Coding Standards**
    - [ ] Ensure all code follows the coding standards specified in `.ai/coding-standards.md`.

## Notes

- **Coding Standards**: All code should adhere to the organization's coding standards:
    - 2-space indentation.
    - Do not align code; indent only.
    - Align all brackets in the same column if not on the same line.
    - Use explicit type names unless obvious from context.
    - Use file-scoped namespaces.
    - Do not use underscore prefixing for private fields.
    - Use PascalCase for all class-scoped names (public, protected, and private fields, properties, methods, events).
    - Use camelCase for local variables inside methods.
- **Async Main Method**: Use `public static async Task Main(string[] args)` to support asynchronous operations.
- **Prepare for DI**: The Generic Host provides the DI container that we'll use in the next task.
- **Code Cleanliness**: Ensure that the code remains clean and adheres to best practices.

## Implementation Notes

- **Program.cs Structure**:
    - Replace the default content of the `Main` method with the Generic Host configuration.
- **Future Considerations**:
    - Leave placeholders or comments where services will be registered in the next task.
- **Logging and Configuration**:
    - The `CreateDefaultBuilder` method sets up default logging and configuration; no additional setup is required at this stage.

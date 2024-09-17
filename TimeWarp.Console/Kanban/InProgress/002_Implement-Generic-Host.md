# Task 002: Implement the Generic Host

## Description

Set up the .NET Generic Host in the console application to manage the application lifecycle, configuration, and logging. This will provide a robust foundation for integrating Dependency Injection and other services in subsequent tasks.

## Requirements

- Add the `Microsoft.Extensions.Hosting` package to the project.
- Modify `Program.cs` to use the Generic Host, ensuring the code adheres to the project's coding standards.
- Ensure the application runs successfully using the Generic Host.

## Checklist

- **Add Hosting Package**
  - [ ] Install the `Microsoft.Extensions.Hosting` NuGet package:

    ```pwsh
    dotnet add Source/ConsoleApp/ConsoleApp.csproj package Microsoft.Extensions.Hosting
    ```

- **Configure Generic Host**
  - [ ] Modify `Program.cs` to use the Generic Host builder:

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

  ```
  # ConsoleApp
  
  ## Updates
  
  - Implemented the .NET Generic Host to manage application lifecycle, configuration, and logging.
  
  ## Next Steps
  
  - Integrate Dependency Injection in upcoming tasks.
  ```

## Notes

- Ensure all code follows the project's coding standards as specified in the `.ai` directory.
- Focus on setting up the Generic Host without adding unnecessary complexity.
- Prepare the project for dependency injection in the next task.

## Implementation Notes

- **Program.cs Structure**:
  - Replace the default content of the `Main` method with the Generic Host configuration.
- **Async Main Method**:
  - Use `public static async Task Main(string[] args)` to support asynchronous operations.
- **Future Considerations**:
  - Leave placeholders or comments where services will be registered in the next task.
- **Logging and Configuration**:
  - The `CreateDefaultBuilder` method sets up default logging and configuration; no additional setup is required at this stage.
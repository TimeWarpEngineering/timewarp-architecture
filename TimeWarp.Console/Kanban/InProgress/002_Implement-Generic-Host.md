# Task 002: Implement the Generic Host

## Description

Set up the .NET Generic Host in the console application to manage the application lifecycle, configuration, and logging. This will provide a robust foundation for integrating Dependency Injection and other services in subsequent tasks.

## Requirements

- Add the `Microsoft.Extensions.Hosting` package to the project.
- Modify `Program.cs` to use the Generic Host.
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
      using System.Threading.Tasks;
      using Microsoft.Extensions.Hosting;
  
      namespace ConsoleApp
      {
          class Program
          {
              public static async Task Main(string[] args)
              {
                  using IHost host = Host.CreateDefaultBuilder(args)
                      .ConfigureServices((context, services) =>
                      {
                          // Services will be registered here in future tasks
                      })
                      .Build();
  
                  await host.RunAsync();
              }
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

## Notes

- **Alignment with `--use-program-main`**: Since `--use-program-main` was specified in Task 001, we're using an explicit `Program` class with a `Main` method.
- **Async Main Method**: Use `static async Task Main(string[] args)` to support asynchronous operations.
- **Code Cleanliness**: Ensure that the code remains clean and adheres to best practices.
- **Prepare for DI**: The Generic Host provides the DI container that we'll use in the next task.

## Implementation Notes

- **Program.cs Structure**:
    - Replace the default content of `Main` method with the Generic Host configuration.
- **Future Considerations**:
    - Leave placeholders or comments where services will be registered in the next task.
- **Logging and Configuration**:
    - The `CreateDefaultBuilder` method sets up default logging and configuration; no additional setup is required at this stage.


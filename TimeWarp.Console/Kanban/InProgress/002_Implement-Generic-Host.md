### Task Document:

# Task 002: Implement the Generic Host

## Description

Set up the .NET Generic Host in the console application to manage the application lifecycle, configuration, and logging. This will provide a robust foundation for integrating Dependency Injection and other services in subsequent tasks.

## Requirements

- Add `Microsoft.Extensions.Hosting` package to the project.
- Configure the Generic Host in `Program.cs`.
- Set up basic logging and configuration if necessary.
- Ensure the application runs successfully using the Generic Host.

## Checklist

- **Add Hosting Package**
    - [ ] Install `Microsoft.Extensions.Hosting` NuGet package:
      ```pwsh
      dotnet add Source/ConsoleApp.csproj package Microsoft.Extensions.Hosting
      ```
- **Configure Generic Host**
    - [ ] Modify `Program.cs` to use the Generic Host builder:
      ```csharp
      using Microsoft.Extensions.Hosting;
  
      await Host.CreateDefaultBuilder(args)
          .ConfigureServices((hostContext, services) =>
          {
              // Register services here in future tasks
          })
          .RunConsoleAsync();
      ```
- **Set Up Logging (Optional)**
    - [ ] Use the default logging provided by the Generic Host.
- **Verify Application**
    - [ ] Run the application to ensure it works correctly with the Generic Host implemented:
      ```pwsh
      dotnet run --project Source/ConsoleApp.csproj
      ```
- **Update Documentation**
    - [ ] Add notes to `Documentation/README.md` about the integration of the Generic Host.

## Notes

- **Simplify Integration**: Focus on getting the Generic Host set up without adding unnecessary complexity.
- **Prepare for DI**: The Generic Host will provide the DI container used in the next task.
- **Code Cleanliness**: Ensure that the code remains clean and adheres to best practices.

## Implementation Notes

- **Program.cs Structure**:
    - Replace the default `Main` method content with the Generic Host configuration.
- **Async Main Method**:
    - Use `async Task Main(string[] args)` to support asynchronous operations.
- **Future Considerations**:
    - Leave placeholders or comments where services will be registered in the next task.

---

### Example `Program.cs` Modification:

```csharp
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;

namespace ConsoleApp
{
    class Program
    {
        static async Task Main(string[] args)
        {
            await Host.CreateDefaultBuilder(args)
                .ConfigureServices((hostContext, services) =>
                {
                    // Services will be registered here in future tasks
                })
                .RunConsoleAsync();
        }
    }
}
```
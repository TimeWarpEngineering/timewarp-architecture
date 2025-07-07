# Task 002: Integrate Cocona into the Console Application

## Description

Integrate **Cocona** into the console application to simplify command-line parsing and Dependency Injection. This will provide a streamlined foundation for managing commands, options, and services throughout the application.

## Requirements

- Add the `Cocona` package to the project.
- Modify `Program.cs` to use Cocona's application builder.
- Define a sample command using Cocona's attribute-based syntax.
- Ensure the application runs successfully with Cocona integrated.
- Adhere to the project's coding standards as specified in the `.ai` directory.

## Checklist

- **Add Cocona Package**
  - [ ] Install the `Cocona` NuGet package:

    ```pwsh
    dotnet add Source/ConsoleApp/ConsoleApp.csproj package Cocona
    ```

- **Modify Program.cs**
  - [ ] Update `Program.cs` to use Cocona's application builder:

    ```csharp
    // File: Program.cs
    namespace ConsoleApp;

    using Cocona;
    using System.Threading.Tasks;

    public class Program
    {
      public static void Main(string[] args)
      {
        CoconaApp.Run<Program>(args);
      }

      // Define a command method
      public void Hello(
        [Option('n', Description = "Your name")] string name = "World")
      {
        Console.WriteLine($"Hello, {name}!");
      }
    }
    ```

- **Verify Application**
  - [ ] Run the application to ensure it works correctly with Cocona integrated:

    ```pwsh
    dotnet run --project Source/ConsoleApp/ConsoleApp.csproj -- hello --name "Alice"
    ```

    - Expected output:

      ```
      Hello, Alice!
      ```

- **Update Documentation**
  - [ ] Add notes to `Documentation/ReadMe.md` about the integration of Cocona and how to use the application.

    ```markdown
    # ConsoleApp

    ## Updates

    - Integrated Cocona for command-line parsing and Dependency Injection.

    ## Usage

    Run the application with the following command:

    ```sh
    dotnet run --project Source/ConsoleApp/ConsoleApp.csproj -- hello --name "YourName"
    ```

    Replace `"YourName"` with your actual name.

    ## Available Commands

    - `hello`: Greets the user.
      - Options:
        - `-n`, `--name`: Specifies the name to greet.
    ```

- **Ensure Coding Standards**
  - [ ] Verify that all code changes comply with the coding standards specified in `.ai/coding-standards.md`.

## Notes

- **Simplicity**: Cocona reduces boilerplate code and simplifies command-line parsing and DI integration.
- **Attribute-Based Commands**: Commands and options are defined using attributes, making the code clean and easy to read.
- **Future Expansion**: This setup allows for easy addition of more commands and services in future tasks.

## Implementation Notes

- **Dependency Injection**:
  - Cocona automatically sets up a service container using `Microsoft.Extensions.DependencyInjection`.
  - You can register services in the `ConfigureServices` method if needed.
- **Asynchronous Commands**:
  - If you need to perform asynchronous operations, you can define commands as `async Task` methods.

  ```csharp
  public async Task FetchData()
  {
    // Asynchronous code here
    await Task.Delay(1000);
  }
  ```

- **Multiple Commands**:
  - You can define multiple command methods within the `Program` class or separate classes.

  ```csharp
  public void Goodbye()
  {
    Console.WriteLine("Goodbye!");
  }
  ```

- **Using Services**:
  - Inject services into command methods via parameters.

  ```csharp
  public void ProcessData(IMyService myService)
  {
    myService.Execute();
  }
  ```

## Example of Registering Services

If you need to register services for Dependency Injection, create a `ConfigureServices` method:

```csharp
// File: Program.cs
namespace ConsoleApp;

using Cocona;
using Microsoft.Extensions.DependencyInjection;
using System.Threading.Tasks;

public class Program
{
  public static void Main(string[] args)
  {
    CoconaApp.CreateBuilder()
      .ConfigureServices(services =>
      {
        services.AddSingleton<IMyService, MyService>();
      })
      .Build()
      .Run();
  }

  public void UseService(IMyService myService)
  {
    myService.Execute();
  }
}

public interface IMyService
{
  void Execute();
}

public class MyService : IMyService
{
  public void Execute()
  {
    Console.WriteLine("Service Executed.");
  }
}
```

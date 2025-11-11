# Runfiles

This directory contains .NET 10 file-based applications (runfiles) that replace traditional PowerShell/Bash scripts.

## What are Runfiles?

Runfiles are **fully compiled .NET applications** written as single C# files with the `.cs` extension. They use .NET 10's native support for single-file apps - NO external tools required.

**IMPORTANT**: These are NOT "scripts" - they are compiled applications with full access to:
- All .NET features (async/await, LINQ, etc.)
- NuGet packages
- Source generators and analyzers
- Ahead-of-time (AOT) compilation
- Full IDE support (IntelliSense, debugging, refactoring)

## Shebang and Directives

Each runfile starts with a shebang and uses built-in .NET 10 directives:

```csharp
#!/usr/bin/dotnet --
#:package TimeWarp.Amuru@1.0.0-beta.5
#:package Spectre.Console@0.49.1

using TimeWarp.Amuru;
using Spectre.Console;

// Your code here
```

### Available Directives

- `#:package PackageName@Version` - Add NuGet package (@ for version)
- `#:project path/to/project.csproj` - Reference local project
- `#:property PropertyName=Value` - Set MSBuild property (= for assignment)
- `#:sdk SdkName@Version` - Add SDK reference (@ for version)

**Note**: Use `#:` directives (native .NET 10), NOT `#r` directives (obsolete dotnet-script syntax)

## Execution

### Make Executable
```bash
chmod +x runfile.cs
```

### Run Directly
```bash
./runfile.cs
```

### Run with dotnet
```bash
dotnet run runfile.cs
```

### Publish as Binary
```bash
dotnet publish runfile.cs -o bin/
```

## Shell Execution with TimeWarp.Amuru

TimeWarp.Amuru provides shell-like execution capabilities:

```csharp
#!/usr/bin/dotnet --
#:package TimeWarp.Amuru@1.0.0-beta.5

using TimeWarp.Amuru;

// Stream output to console (like running the command directly)
await Shell.Builder("dotnet", "build").RunAsync();

// Capture output for processing
var result = await Shell.Builder("git", "status").CaptureAsync();
if (result.Success)
{
    Console.WriteLine($"Exit code: {result.ExitCode}");
    Console.WriteLine($"Output: {result.Stdout}");
}

// Pipeline commands
await Shell.Builder("find", ".", "-name", "*.cs")
    .Pipe("grep", "async")
    .Pipe("wc", "-l")
    .CaptureAsync();
```

## Why Runfiles Instead of Scripts?

### Advantages over PowerShell/Bash:
1. **Type Safety**: Compile-time checking catches errors early
2. **IDE Support**: Full IntelliSense, refactoring, and debugging
3. **Cross-Platform**: Same code runs on Windows, Linux, macOS
4. **Modern Language**: C# latest features vs legacy scripting syntax
5. **Package Ecosystem**: Access to entire NuGet ecosystem
6. **Testable**: Can write unit tests for runfile logic
7. **Performance**: Compiled code is faster than interpreted scripts
8. **Maintainable**: Refactoring tools work, can extract methods/classes

### Comparison:
```powershell
# PowerShell - No type safety, limited tooling
$result = dotnet build
if ($LASTEXITCODE -ne 0) {
    Write-Error "Build failed"
}
```

```csharp
// C# Runfile - Type safe, full IDE support
var result = await Shell.Builder("dotnet", "build").CaptureAsync();
if (!result.Success)
{
    throw new Exception($"Build failed with exit code {result.ExitCode}");
}
```

## Migration from Scripts/

When migrating from `Scripts/` directory:
1. Convert `.ps1` files to `.cs` runfiles
2. Replace shell commands with `TimeWarp.Amuru` Shell.Builder
3. Add type safety and error handling
4. Keep same filename (e.g., `Build.ps1` → `build.cs`)
5. Make executable with `chmod +x`

## Examples

See existing runfiles in this directory for examples:
- `build.cs` - Build solution (replaces Build.ps1)

## Resources

- .NET 10 file-based apps: https://learn.microsoft.com/en-us/dotnet/core/whats-new/dotnet-10
- TimeWarp.Amuru: https://www.nuget.org/packages/TimeWarp.Amuru
- Spectre.Console: https://spectreconsole.net/

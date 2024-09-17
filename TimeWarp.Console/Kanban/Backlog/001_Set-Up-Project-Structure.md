# Task 001: Set Up Project Structure

## Description

Create the initial project structure for the .NET 8 console application. This will serve as the foundation for integrating Dependency Injection, Hosting, and System.CommandLine in subsequent tasks.

## Requirements

- Create a new console application project targeting **.NET 8.0**.
- Use `ConsoleApp` as the project name.
- Organize project files and folders logically using the following structure:
  - `Source` for source code.
  - `Tests` for unit tests.
  - `Documentation` for project documentation.
- Ensure the project runs successfully.

## Checklist

- **Organize Structure**
  - [ ] Create the following folders at the root level:
    - [ ] `Source` for source code:
    - [ ] `Tests` for unit tests:
    - [ ] `Documentation` for project documentation:
      ```powershell
      New-Item -Path "Source/Index.md" -ItemType File -Force -Value "TODO"
      New-Item -Path "Tests/Index.md" -ItemType File -Force -Value "TODO"
      New-Item -Path "Documentation/Index.md" -ItemType File -Force -Value "TODO"
      ```
- **Initialize Project**
  - [ ] From the root directory, run the following command to create the project directly in the `Source` folder:
    ```powershell
    dotnet new console --framework net8.0 --name ConsoleApp --output Source
    ```
- **Verify Build**
  - [ ] Run the application to confirm it executes successfully:
    ```powershell
    dotnet run --project Source/ConsoleApp.csproj
    ```
- **Documentation**
  - [ ] Create a `README.md` placeholder file in the `Documentation` folder with the text `TODO`:
    ```powershell
    New-Item -Path "Documentation/README.md" -ItemType File -Force -Value "TODO"
    ```
- **Version Control**
  - [ ] Generate a `.gitignore` file tailored for .NET projects:
    ```powershell
    dotnet new gitignore
    ```

## Notes

- **PowerShell Usage**: PowerShell (pwsh) is cross-platform, so all commands are provided using PowerShell.
- **Namespace Usage**: Use `ConsoleApp` as the namespace in your code files.
- **Clean Codebase**: Keep the initial code minimal to simplify the addition of features in future tasks.

## Implementation Notes

- **Avoid Unnecessary Additions**:
  - Do not add extra packages or code at this stage to keep the project clean and focused.
- **Documentation**:
  - The `README.md` in the `Documentation` folder and the `Index.md` files in each directory should contain `TODO` as their content.

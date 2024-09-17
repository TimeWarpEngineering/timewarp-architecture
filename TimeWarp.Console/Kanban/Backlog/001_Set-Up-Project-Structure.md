# Task 001: Set Up Project Structure

## Description

Create the initial project structure for the .NET 8 console app template. This will serve as the foundation for integrating Dependency Injection, Hosting, and System.CommandLine in subsequent tasks.

## Requirements

- Create a new console application project targeting **.NET 8.0**.
- Use `ConsoleApp` as the project name, which will serve as the placeholder in the template.
- Organize project files and folders logically using the following structure:
  - `Source` for source code.
  - `Tests` for unit tests.
  - `Documentation` for project documentation.
- Ensure the project builds and runs successfully.
- Prepare the project for transformation into a reusable template.

## Checklist

- **Initialize Project**
  - [ ] Use `dotnet new console --framework net8.0 --name ConsoleApp` to create the project.
  - [ ] Verify the project targets .NET 8.0 in the `.csproj` file and uses `ConsoleApp` as the project name.
- **Organize Structure**
  - [ ] Create the following folders at the root level:
    - [ ] `Source` for source code.
    - [ ] `Tests` for unit tests.
    - [ ] `Documentation` for project documentation.
  - [ ] Move the default source code files into the `Source` folder.
  - [ ] Update the `.csproj` file to reflect the new file paths.
  - [ ] Include a `.gitignore` file tailored for .NET projects.
- **Verify Build**
  - [ ] Build the project to ensure there are no errors.
  - [ ] Run the application to confirm it executes successfully.
- **Prepare for Template Conversion**
  - [ ] Ensure all occurrences of `ConsoleApp` in code and configuration files are ready for token replacement.
  - [ ] Avoid hard-coded values that would hinder templating.
- **Documentation**
  - [ ] Create a `README.md` in the `Documentation` folder with initial project information and instructions.
  - [ ] Document the project structure and its purpose.

## Notes

- **Namespace Usage**: Use `ConsoleApp` as the namespace in your code files. This will be replaced with the user's project name when the template is instantiated.
- **Consistency**: Adhere to standard conventions to facilitate ease of use when the project is converted into a template.
- **Simplicity**: Keep the initial code minimal to simplify the addition of features in future tasks.

## Implementation Notes

- **Command to Initialize Project**:
  ```bash
  dotnet new console --framework net8.0 --name ConsoleApp
  ```
- **Namespace in Code Files**:
  - Ensure your `Program.cs` (or any other code files) uses `ConsoleApp` as the namespace:
    ```csharp
    namespace ConsoleApp
    {
        class Program
        {
            static void Main(string[] args)
            {
                // Code here
            }
        }
    }
    ```
- **Updating File Paths**:
  - After moving `Program.cs` to the `Source` folder, update your `.csproj` file if necessary:
    ```xml
    <ItemGroup>
      <Compile Include="Source\Program.cs" />
    </ItemGroup>
    ```
- **Template Configuration**:
  - When you set up the `template.json` file later, specify `ConsoleApp` as the `sourceName` to enable token replacement:
    ```json
    {
      "$schema": "http://json.schemastore.org/template",
      "author": "Your Name",
      "classifications": [ "Console" ],
      "identity": "Your.Template.Identity",
      "name": "Your Template Name",
      "shortName": "yourtemplate",
      "sourceName": "ConsoleApp",
      "preferNameDirectory": true
    }
    ```
- **Generating `.gitignore`**:
  - Use the command `dotnet new gitignore` to create a standard `.gitignore` file for .NET projects.
- **Avoid Unnecessary Additions**:
  - Do not add extra packages or code at this stage to keep the project clean and focused.

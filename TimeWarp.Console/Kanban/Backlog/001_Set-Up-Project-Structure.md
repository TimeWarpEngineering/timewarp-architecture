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
- Ensure the project builds and runs successfully.

## Checklist

- **Organize Structure**
  - [ ] Create the following folders at the root level:
    - [ ] `Source` for source code.
    - [ ] `Tests` for unit tests.
    - [ ] `Documentation` for project documentation.
- **Initialize Project**
  - [ ] From the root directory, run the following command to create the project directly in the `Source` folder:
    ```bash
    dotnet new console --framework net8.0 --name ConsoleApp --output Source
    ```
- **Verify Build**
  - [ ] Navigate to the `Source` directory:
    ```bash
    cd Source
    ```
  - [ ] Build the project to ensure there are no errors:
    ```bash
    dotnet build
    ```
  - [ ] Run the application to confirm it executes successfully:
    ```bash
    dotnet run
    ```
- **Documentation**
  - [ ] Create a `README.md` in the `Documentation` folder with initial project information and instructions.
  - [ ] Document the project structure and its purpose.
- **Version Control**
  - [ ] Generate a `.gitignore` file tailored for .NET projects:
    ```bash
    dotnet new gitignore
    ```

## Notes

- **Namespace Usage**: Use `ConsoleApp` as the namespace in your code files.
- **Clean Codebase**: Keep the initial code minimal to simplify the addition of features in future tasks.

## Implementation Notes

- **Avoid Unnecessary Additions**:
  - Do not add extra packages or code at this stage to keep the project clean and focused.
- **Documentation**:
  - Use the `README.md` to provide an overview of the project and explain the directory structure.

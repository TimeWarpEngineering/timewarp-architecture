# Task 020: Create TimeWarp.Tool And Migrate Sync Script

## Description

Create a new .NET tool project called TimeWarp.Tool and migrate the existing PowerShell script [`.github/scripts/sync-configurable-files.ps1`](.github/scripts/sync-configurable-files.ps1:1) to a C# implementation. This will provide better integration with the .NET ecosystem, improved maintainability, and leverage existing .NET libraries for YAML parsing and HTTP operations.

## Parent
019_Enhance-Sync-Config-With-Advanced-Features

## Requirements

- Create a new .NET tool project `TimeWarp.Tool` in the appropriate location within the solution structure
- Migrate all functionality from [`.github/scripts/sync-configurable-files.ps1`](.github/scripts/sync-configurable-files.ps1:1) to C#
- Implement command-line interface using `System.CommandLine` or similar
- Replace `yq` dependency with YamlDotNet for YAML parsing
- Use `HttpClient` for file downloads with proper async/await patterns
- Maintain compatibility with GitHub Actions environment variables and outputs
- Support the enhanced configuration features from task 019
- Package as a distributable .NET tool via NuGet
- Update [`.github/workflows/sync-configurable-files.yml`](.github/workflows/sync-configurable-files.yml:1) to use the new tool
- Ensure cross-platform compatibility (Windows, Linux, macOS)

## Checklist

### Design
- [ ] Design project structure for TimeWarp.Tool
- [ ] Plan command-line interface and parameters
- [ ] Design configuration models for YAML parsing
- [ ] Plan async/await patterns for file operations

### Implementation
- [ ] Create TimeWarp.Tool project with proper .csproj configuration
- [ ] Add to solution and configure Directory.Build.props integration
- [ ] Implement command-line argument parsing
- [ ] Implement YAML configuration parsing with YamlDotNet
- [ ] Implement parallel file download logic using HttpClient
- [ ] Implement file comparison and copying logic
- [ ] Implement GitHub Actions integration (environment variables, outputs)
- [ ] Implement git staging functionality
- [ ] Add proper error handling and logging
- [ ] Configure tool packaging for NuGet distribution

### Testing
- [ ] Create unit tests for core functionality
- [ ] Test GitHub Actions integration
- [ ] Test cross-platform compatibility
- [ ] Verify functionality with existing sync-config.yml

### Documentation
- [ ] Update tool documentation
- [ ] Update GitHub workflow to use new tool
- [ ] Document installation and usage instructions

## Notes

### Current PowerShell Script Functionality to Migrate:

1. **Parameter Handling** (lines 5-12):
   - ConfigFile path resolution
   - GitHub environment variables integration
   - Token and workspace validation

2. **YAML Configuration Parsing** (lines 155-179):
   - Replace `yq` with YamlDotNet
   - Parse parent repository, branch, and sync files
   - Handle cron schedule configuration

3. **File Download Logic** (lines 213-264):
   - Parallel downloads using background jobs → async/await with HttpClient
   - GitHub API integration with proper headers
   - Error handling for failed downloads

4. **File Comparison and Updates** (lines 276-307):
   - SHA256 hash comparison
   - File copying with directory creation
   - Change tracking

5. **Git Integration** (lines 316-323):
   - Staging changed files
   - Integration with git commands

6. **GitHub Actions Output** (lines 309-347):
   - Environment variable outputs
   - Step summary generation
   - Results reporting

### Technical Implementation Details:

**Project Structure:**
```
Source/Tools/TimeWarp.Tool/
├── TimeWarp.Tool.csproj
├── Program.cs
├── GlobalUsings.cs
├── Commands/
│   └── SyncCommand.cs
├── Models/
│   ├── SyncConfiguration.cs
│   └── SyncResult.cs
├── Services/
│   ├── ConfigurationService.cs
│   ├── FileDownloadService.cs
│   ├── FileComparisonService.cs
│   └── GitHubActionsService.cs
└── ReadMe.md
```

**Key Dependencies:**
- YamlDotNet for YAML parsing
- System.CommandLine for CLI interface
- Microsoft.Extensions.Logging for logging
- Microsoft.Extensions.DependencyInjection for DI

**Command Interface:**
```bash
dotnet tool install -g TimeWarp.Tool
timewarp-tool sync --config .github/sync-config.yml
```

### Integration Points:

- Update [`.github/workflows/sync-configurable-files.yml`](.github/workflows/sync-configurable-files.yml:1) to install and use the tool
- Maintain backward compatibility with existing configuration format
- Support enhanced features from task 019 (default_dest_to_source, path_transform)
- Ensure proper error codes and exit statuses for CI/CD integration

### Benefits of Migration:

- **Performance**: Compiled C# code vs interpreted PowerShell
- **Maintainability**: Strong typing and better IDE support
- **Dependencies**: Eliminate external `yq` dependency
- **Testing**: Better unit testing capabilities
- **Distribution**: NuGet package distribution
- **Integration**: Better .NET ecosystem integration

## Implementation Notes

The tool should follow TimeWarp.Architecture conventions:
- Use 2-space indentation with Allman-style brackets
- Target net9.0 framework
- Follow established naming conventions
- Use file-scoped namespaces
- Implement proper async/await patterns
- Include comprehensive error handling and logging

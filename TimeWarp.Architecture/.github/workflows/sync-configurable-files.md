# Sync Configurable Files Workflow

This workflow automatically synchronizes configurable files from a parent repository to child repositories, ensuring consistency across multiple projects.

## Overview

The sync workflow runs on a scheduled basis (default: every Monday at 9:00 AM UTC) and can also be triggered manually. It downloads specified files from a parent repository and creates pull requests in child repositories when updates are detected.

## Files

- `sync-configurable-files.yml` - The main GitHub Actions workflow
- `sync-config.yml` - Configuration file defining which files to sync
- `SYNC_WORKFLOW.md` - This documentation file

## Configuration

### Using the Configuration File

The workflow can be configured using `.github/sync-config.yml`. This file defines:

- **Parent repository**: The source repository to sync from
- **Files to sync**: List of files or patterns to synchronize
- **Excluded files**: Files to exclude from synchronization
- **Schedule**: Cron expression for automated runs
- **Pull request settings**: Configuration for generated PRs

Example configuration:
```yaml
parent:
  repository: "TimeWarpEngineering/timewarp-architecture"
  branch: "master"

sync_files:
  - ".gitignore"
  - ".editorconfig"
  - ".github/workflow-templates/"

exclude_files:
  - ".github/workflows/sync-configurable-files.yml"
```

### Manual Trigger Options

The workflow can be manually triggered with custom parameters:

- **parent_repo**: Override the parent repository
- **parent_branch**: Override the parent branch
- **files_to_sync**: Comma-separated list of files (overrides config file)
- **use_config_file**: Whether to use the configuration file (default: true)

## How It Works

1. **Configuration Loading**: Loads settings from `sync-config.yml` or manual inputs
2. **File Download**: Downloads specified files from the parent repository
3. **Comparison**: Compares downloaded files with current repository files
4. **PR Creation**: Creates a pull request if differences are found
5. **Summary**: Provides a detailed summary of the sync operation

## Supported File Types

The workflow can sync any type of file, including:

- Configuration files (`.gitignore`, `.editorconfig`, etc.)
- Workflow templates
- Build configuration files
- Documentation templates
- Code quality configurations

## Security Considerations

- Uses `GITHUB_TOKEN` for authentication
- Only syncs explicitly configured files
- Creates PRs for review rather than direct commits
- Excludes the sync workflow itself from synchronization

## Troubleshooting

### Common Issues

1. **Permission Denied**: Ensure the repository has proper permissions for the GitHub Actions bot
2. **File Not Found**: Check that files exist in the parent repository at the specified branch
3. **PR Creation Failed**: Verify that the repository allows PR creation from GitHub Actions

### Debugging

1. Check the workflow run logs for detailed error messages
2. Verify the configuration file syntax
3. Test with manual trigger to isolate issues
4. Review the parent repository permissions

## Best Practices

1. **Start Small**: Begin with a few critical files before expanding
2. **Review PRs**: Always review generated PRs before merging
3. **Test Configuration**: Use manual triggers to test configuration changes
4. **Regular Maintenance**: Periodically review and update the sync configuration
5. **Security**: Keep sensitive files out of the sync configuration

## Customization

### Changing the Schedule

Modify the cron expression in either the workflow file or configuration:

```yaml
schedule:
  cron: "0 9 * * 1"  # Every Monday at 9 AM UTC
```

### Adding Custom Logic

The workflow can be extended with additional steps for:

- Custom file processing
- Conditional synchronization
- Integration with other tools
- Notification systems

### Branch Protection

Consider setting up branch protection rules to:

- Require PR reviews
- Run status checks
- Enable auto-merge for trusted updates

## Examples

### Sync Development Configuration

```yaml
sync_files:
  - ".gitignore"
  - ".editorconfig"
  - ".eslintrc.js"
  - ".prettierrc.json"
```

### Sync Build Templates

```yaml
sync_files:
  - "Directory.Build.targets"
  - "NuGet.config"
  - ".github/workflow-templates/"
```

### Sync Documentation Templates

```yaml
sync_files:
  - ".templates/"
  - "README.template.md"
  - "CONTRIBUTING.md"
```
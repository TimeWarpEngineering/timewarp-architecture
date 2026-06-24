# Task 019: Enhance Sync Config With Advanced Features

## Description

Enhance the [`.github/sync-config.yml`](.github/sync-config.yml:1) configuration structure to support advanced features including:
- Default `dest_path` to `source_path` when not specified
- Path transformation mechanisms (prefix removal)
- Repository-specific configurations with flexible sync options
- Improved configuration structure for better maintainability

## Parent
018_Sync-Configurable-Files-from-Parent-Repo

## Requirements

- Update [`.github/sync-config.yml`](.github/sync-config.yml:1) with enhanced configuration structure
- Implement `default_dest_to_source` option under `sync_options`
- Add `path_transform` capability with `remove_prefix` functionality
- Support repository-specific path transformations
- Replace legacy configuration with simplified repos-based structure only
- Update [`.github/scripts/sync-configurable-files.ps1`](.github/scripts/sync-configurable-files.ps1:1) to handle new configuration features
- Ensure the enhanced configuration works with existing [`.github/workflows/sync-configurable-files.yml`](.github/workflows/sync-configurable-files.yml:1)

## Checklist

### Design
- [ ] Design enhanced configuration schema with new features
- [ ] Plan path transformation logic for prefix removal
- [ ] Design default destination path behavior

### Implementation
- [ ] Update [`.github/sync-config.yml`](.github/sync-config.yml:1) with new configuration structure
- [ ] Implement `default_dest_to_source` logic in PowerShell script
- [ ] Implement `path_transform` with `remove_prefix` functionality
- [ ] Add support for repository-specific transformations
- [ ] Update relevant configuration settings
- [ ] Verify functionality with test scenarios

### Documentation
- [ ] Update configuration documentation with new features
- [ ] Add examples of path transformation usage
- [ ] Document new sync options

## Notes

### Complete Sample Configuration:

```yaml
# Sample configuration for syncing files with source and destination paths, grouped by repository
default_repo: 'owner/parent-repo'  # Default parent repository if not specified in repos list
default_branch: 'main'             # Default branch if not specified
schedule:
  cron: '0 9 * * 1'               # Scheduled sync every Monday at 9 AM UTC

# Repository-based file sync configuration
repos:
  - repo: 'owner/parent-repo'
    branch: 'main'
    files:
      - source_path: 'prompts/general-prompt.md'  # Path in the source repository
        dest_path: 'docs/prompts/general.md'      # Explicit destination path
      - source_path: 'config/settings.yml'        # If dest_path is omitted, defaults to source_path
  - repo: 'owner/TimeWarp.Architecture'
    branch: 'main'
    path_transform:
      remove_prefix: 'TimeWarp.Architecture/'    # Remove this prefix from dest_path if present in source_path
    files:
      - source_path: 'TimeWarp.Architecture/templates/architecture-template.md'
        dest_path: 'templates/architecture-template.md'  # Explicitly specified, but prefix removal applies if not
      - source_path: 'TimeWarp.Architecture/docs/guide.md'  # Will default to 'docs/guide.md' after prefix removal
  - repo: 'owner/prompts-repo'
    branch: 'develop'
    files:
      - source_path: 'templates/prompt-template.md'
        dest_path: 'templates/prompt-template.md'
      - source_path: 'prompts/specialized-prompt.md'  # Defaults to same as source_path if dest_path not specified

# Optional settings for the sync process
sync_options:
  overwrite_existing: true   # Whether to overwrite files if they already exist
  ignore_missing: false      # Whether to fail if source files are missing
  default_dest_to_source: true  # If true, dest_path defaults to source_path when not specified
```

### Key Features to Implement:

1. **Default Destination Behavior**:
   - `default_dest_to_source: true` under `sync_options`
   - When `dest_path` is not specified, it defaults to `source_path`
   - Path transformations are applied to the defaulted path

2. **Path Transformation**:
   - `path_transform` section under repository configuration
   - `remove_prefix` functionality to strip specified prefixes
   - Applied to both explicit and defaulted destination paths

3. **Enhanced Repository Configuration**:
   - Support for branch-specific configurations
   - Repository-specific path transformations
   - Flexible file path mappings with sensible defaults

### Configuration Structure Benefits:
- **Defaulting dest_path to source_path**: Reduces configuration overhead when desired destination matches source structure
- **Prefix Removal for TimeWarp.Architecture**: Automatic prefix removal ensures consistent destination paths
- **Flexibility**: Still allows explicit `dest_path` specifications when needed
- **Global Defaults**: `default_repo` and `default_branch` for simplified configuration
- **Scheduling**: Built-in cron scheduling for automated syncing

### Integration Points:
- PowerShell script must parse new configuration structure
- Workflow file should remain largely unchanged
- Maintain compatibility with existing sync logic

## Implementation Notes

The enhanced configuration should provide:
- Simplified configuration when source and destination paths match
- Automatic prefix removal for TimeWarp.Architecture repository files
- Flexible path transformation capabilities for future use cases
- Clear separation between global and repository-specific settings

### Implementation Completed ✅

**Key Features Implemented:**

1. **Enhanced Configuration Structure**: Updated `.github/sync-config.yml` with new repos-based structure supporting:
   - Repository-specific configurations with flexible sync options
   - Path transformation mechanisms with `remove_prefix` functionality
   - `default_dest_to_source` option under `sync_options`

2. **PowerShell Script Simplification**: Updated `.github/scripts/sync-configurable-files.ps1` to:
   - Support only the new repos-based configuration structure (legacy support removed for simplicity)
   - Implement `default_dest_to_source` logic that defaults destination paths to source paths when not specified
   - Apply path transformations including prefix removal automatically
   - Simplified code by removing conditional logic for multiple configuration types

3. **Path Transformation Logic**: Implemented automatic prefix removal that:
   - Removes `TimeWarp.Architecture/` prefix from destination paths when configured
   - Applies transformations to both explicit and defaulted destination paths
   - Supports repository-specific transformation rules

4. **Configuration Testing**: Validated the new configuration structure using yq parser:
   - Confirmed YAML syntax is valid
   - Verified all configuration options are accessible
   - Tested repos structure, file specifications, and sync options

**Simplified Architecture**: The implementation removes legacy configuration support for a cleaner, more maintainable codebase focused solely on the enhanced repos-based configuration structure.

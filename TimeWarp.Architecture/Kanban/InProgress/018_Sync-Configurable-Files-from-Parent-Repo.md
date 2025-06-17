# Task 018: Sync Configurable Files from Parent Repo

## Description

Add a workflow item for child repositories to check for updates in configurable files from a parent repository. This task involves setting up a GitHub Actions workflow in child repos with a cron job to periodically poll the parent repo for updates and automatically create pull requests (PRs) in the child repos to apply those updates. The goal is to ensure that when a template or configuration file is updated in the parent repo, the child repos can sync those updates to maintain consistency.

## Requirements

- Create a GitHub Actions workflow file that runs on a schedule (cron job) and can be manually triggered to check for updates in the parent repository.
- Implement logic to compare configurable files between the parent and child repos.
- Automate the creation of PRs in child repos when updates are detected in the parent repo.
- Ensure the workflow is configurable to support different sets of files to sync.

## Checklist

### Design
- [ ] Update Model (if applicable for any data structures related to configuration tracking)
- [ ] Add/Update Tests for the workflow logic if scripted

### Implementation
- [ ] Update Dependencies (if any new tools or libraries are needed for the workflow)
- [ ] Update Relevant Configuration Settings for GitHub Actions
- [ ] Verify Functionality of the cron job and PR creation process
- [ ] add .gitignore and .editorconfig files to the sync configuration.

### Documentation
- [ ] Update Documentation to explain the sync workflow and how to configure it for different repos

## Notes

The user has indicated a plan to use cron jobs in GitHub for automation. This task should explore GitHub Actions' scheduled workflows to achieve this. Consideration should be given to security and access controls for accessing parent and child repositories, possibly using GitHub tokens or SSH keys for authentication.

## Implementation Notes

### Completed Features

**Core Workflow Implementation**
- Created `sync-configurable-files.yml` workflow with scheduled and manual triggers
- Implemented file download logic using GitHub API with proper authentication
- Added file comparison and change detection capabilities
- Integrated automatic PR creation with detailed descriptions

**Configuration System**
- Created `sync-config.yml` for flexible configuration management
- Implemented YAML parsing with yq for dynamic configuration loading
- Added support for manual overrides via workflow dispatch inputs
- Included exclude patterns to prevent recursive syncing

**Documentation**
- Created comprehensive `SYNC_WORKFLOW.md` documentation
- Included configuration examples and troubleshooting guide
- Documented security considerations and best practices

### Technical Implementation Details

**File Download Strategy**
- Uses GitHub Contents API with raw content acceptance
- Implements proper directory structure creation
- Tracks successful/failed downloads with detailed logging
- Handles empty files and download errors gracefully

**Change Detection**
- Compares file contents using diff command
- Only creates PRs when actual changes are detected
- Maintains list of changed files for PR documentation
- Prevents unnecessary PRs for identical content

**PR Management**
- Uses peter-evans/create-pull-request action for robust PR creation
- Implements timestamped branch naming to avoid conflicts
- Includes comprehensive PR descriptions with file lists
- Automatically deletes sync branches after PR creation

**Security Features**
- Uses repository GITHUB_TOKEN for authentication
- Excludes sync workflow from syncing itself
- Implements permission checks (contents: write, pull-requests: write)
- Safe handling of repository access with proper error handling

### Configuration Options

**Default Sync Files**
- `.gitignore` - Version control exclusions
- `.editorconfig` - Editor configuration
- `.github/workflow-templates/` - GitHub workflow templates
- `.templates/` - Documentation and code templates
- `Directory.Build.targets` - Build configuration
- `NuGet.config` - Package management settings

**Scheduling**
- Default: Every Monday at 9:00 AM UTC
- Configurable via sync-config.yml
- Manual trigger available with custom parameters

### Testing Considerations

**Validation Points**
- Configuration file syntax validation
- Parent repository accessibility
- File download success/failure handling
- PR creation and branch management
- Error handling for missing files or permissions

**Recommended Testing Approach**
1. Test with manual trigger using minimal file list
2. Verify configuration file parsing
3. Test with non-existent files to validate error handling
4. Confirm PR creation and branch cleanup
5. Validate exclusion patterns work correctly

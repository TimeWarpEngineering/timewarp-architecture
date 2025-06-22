# Add Sync Workflow for Configurable Files

## Description

Add the sync-configurable-files workflow to this repository to maintain consistency with organizational standards and shared configurations. This workflow will automatically sync common files from the parent TimeWarp Architecture repository.

## Requirements

- [ ] Add `.github/workflows/sync-configurable-files.yml` workflow file
- [ ] Create `.github/sync-config.yml` configuration file tailored to this repository's technology stack
- [ ] Add PowerShell script `.github/scripts/sync-configurable-files.ps1` 
- [ ] Test workflow functionality via manual trigger
- [ ] Implement `.gitignore` sync strategy (see strategy below)

## Repository Analysis Needed

Please analyze this repository to determine:

1. **Technology Stack**: (.NET, npm/Node.js, documentation, etc.)
2. **File Structure**: Existing `.github/` directory structure
3. **Configuration Files**: Current presence of files like:
   - `Directory.Build.props` / `Directory.Build.targets`
   - `global.json`
   - `.editorconfig`
   - `package.json` (for npm repos)
   - `.nvmrc` (for Node.js repos)
   - Documentation templates
4. **Existing Workflows**: Current GitHub Actions workflows that might conflict

## Sync Configuration Requirements

Based on repository type, the sync config should include:

### For .NET Repositories
- `Directory.Build.props`
- `Directory.Build.targets` 
- `global.json`
- `.editorconfig`
- Common GitHub workflow templates

### For npm/Node.js Repositories  
- `package.json` scripts section
- `.nvmrc`
- ESLint/Prettier configurations
- TypeScript configurations

### For Documentation Repositories
- Markdown templates
- DocFX configurations
- Documentation workflow templates

### For All Repositories
- `.gitignore` (using merge strategy - see below)
- Common GitHub workflow templates
- Security and code quality configurations

## GitIgnore Sync Strategy

**.gitignore files require special handling** to maintain both global consistency and repository-specific needs:

### Merge Strategy
- **Global Section**: Sync common ignore patterns from parent repository
- **Local Section**: Preserve repository-specific ignore patterns
- **Process**: Sync script merges global section while preserving local section

**Implementation using section markers:**
```
# ----- Global .gitignore (synced from TimeWarp Architecture) -----
# This section is automatically synced - do not edit manually
[global ignore patterns]

# ----- Repository-specific .gitignore -----  
# Add repository-specific ignore patterns below this line
[local additions]
```

## Expected Deliverables

1. **Workflow File**: `.github/workflows/sync-configurable-files.yml` 
2. **Configuration**: `.github/sync-config.yml` with repository-specific file list
3. **Script**: `.github/scripts/sync-configurable-files.ps1`
4. **Testing**: Successful manual workflow run demonstrating sync functionality
5. **Documentation**: Brief summary of what files will be synced and from where

## Parent Repository

- **Source**: `TimeWarpEngineering/timewarp-architecture`
- **Branch**: `master`
- **Template Location**: `TimeWarp.Architecture/.github/workflows/sync-configurable-files.yml`

## Implementation Notes

- Use the existing sync workflow from `timewarp-architecture` as the base template
- Customize the `sync-config.yml` based on this repository's specific needs
- Ensure the workflow has proper permissions and PAT configuration
- Test with a dry run before enabling automatic scheduling

---

@Claude Please implement this sync workflow for this repository. Analyze the codebase, create the appropriate configuration, and submit a pull request with the sync workflow implementation.

## Acceptance Criteria

- [ ] Sync workflow files are added and properly configured
- [ ] Manual workflow trigger works successfully  
- [ ] Configuration is appropriate for this repository's technology stack
- [ ] No conflicts with existing workflows or configurations
- [ ] Pull request includes clear documentation of changes
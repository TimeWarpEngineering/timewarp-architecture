# Scripts Directory Overview

This directory contains PowerShell scripts used for repository management and development workflow automation.

## Key Scripts

### profile.ps1
Repository-specific PowerShell profile that sets up the development environment.

#### Usage
To load the repository-specific profile in your PowerShell session:

```powershell
# From the repository root
. Scripts\profile.ps1
```

This will:
1. Initialize the repository environment
2. Set the REPO_ROOT environment variable
3. Load repository-specific functions (like Get-NextTaskNumber)

### Get-NextTaskNumber.ps1
Utility script for managing task numbers in the Kanban workflow.

### BuildDependencyDiagram.ps1
Generates dependency diagrams for the solution.

## Adding New Scripts
When adding new scripts:
1. Place them in this directory
2. Update this overview document
3. If the script needs to be available in the PowerShell session, add it to profile.ps1

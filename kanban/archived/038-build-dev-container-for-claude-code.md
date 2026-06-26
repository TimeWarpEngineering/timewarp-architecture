# 038 Build Dev Container For Claude Code

## Description

Create a Development Container (devcontainer) configuration that allows running Claude Code within a containerized environment with access to a single git worktree. This will provide a consistent development environment for developers using Claude Code, ensuring all required tools and dependencies are pre-configured.

## Requirements

- Create .devcontainer configuration for the TimeWarp.Architecture project
- Container must have Claude Code pre-installed and configured
- Provide access to a single git worktree inside the container
- Include all necessary development tools (.NET SDK, Node.js, PowerShell, etc.)
- Configure appropriate VS Code extensions for the development workflow
- Ensure git credentials and SSH keys can be passed through from host
- Support for running all development commands (Run.ps1, RunTests.ps1, etc.)

## Checklist

### Design
- [ ] Research devcontainer.json configuration options
- [ ] Determine base image (mcr.microsoft.com/devcontainers/dotnet or custom)
- [ ] Plan git worktree mounting strategy
- [ ] Identify all required tools and their versions

### Implementation
- [ ] Create .devcontainer/devcontainer.json configuration
- [ ] Create Dockerfile if custom image is needed
- [ ] Configure volume mounts for git worktree
- [ ] Set up Claude Code installation in container
- [ ] Configure VS Code extensions list
- [ ] Set up git credential helper passthrough
- [ ] Add postCreateCommand for initial setup
- [ ] Test all development commands work inside container

### Testing
- [ ] Verify Claude Code runs properly in container
- [ ] Test git operations (pull, push, commit)
- [ ] Verify all PowerShell scripts execute correctly
- [ ] Test building and running the application
- [ ] Verify test execution works
- [ ] Ensure hot reload and file watching work

### Documentation
- [ ] Create README in .devcontainer folder with usage instructions
- [ ] Document how to open project in dev container
- [ ] Add troubleshooting section for common issues
- [ ] Update main README with dev container option

## Notes

- Consider using Docker Compose if multiple services are needed
- Ensure container has sufficient resources allocated
- May need to configure port forwarding for Aspire and web applications
- Consider adding common aliases and shell customizations
- Ensure timezone and locale are properly configured
## Archived (2026-06-26)
Archived as obsolete: never started; references retired tooling (Run.ps1/RunTests.ps1 → dev CLI)
and the deleted TimeWarp.Architecture/ project path; the legacy .devcontainer/ was removed this
session and Dev Containers aren't used here. Revive only if a containerized Claude Code workflow
is wanted against the current root layout.

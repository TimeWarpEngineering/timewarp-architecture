# TimeWarp Architecture Dev Container with Claude Code

This development container provides a secure, isolated environment for working with the TimeWarp Architecture project using Claude Code. Based on the official Claude Code dev container configuration with additional .NET and TimeWarp-specific tooling.

## Features

- **.NET 10 Preview 6** - Latest preview SDK for cutting-edge development
- **Node.js 20** - Stable LTS base (matching Claude Code requirements)
- **Claude Code** - Pre-installed via npm package
- **PowerShell Core** - For running all project scripts
- **Docker-in-Docker** - For container operations and Aspire
- **Git Worktree Support** - Access to parent git worktree
- **Security Firewall** - Network restrictions for enhanced security
- **Pre-configured VS Code Extensions** - All necessary tools
- **Persistent History** - Command history preserved between restarts

## Quick Start

### Opening in VS Code

1. Install the "Dev Containers" extension in VS Code
2. Open the TimeWarp.Architecture folder in VS Code
3. Press `F1` and select "Dev Containers: Reopen in Container"
4. Wait for the container to build (first time takes ~5-10 minutes)

### Using Claude Code

Once inside the container, Claude Code is available globally:

```bash
# Start Claude Code
claude-code

# Check version
claude-code --version
```

### Directory Structure

- `/workspace/timewarp-architecture` - Main project directory
- `/workspace/git-worktree` - Access to parent git worktree
- `/home/vscode/.ssh` - Your SSH keys (mounted read-only)
- `/home/vscode/.gitconfig` - Your git config (mounted read-only)

## Available Commands

The container includes helpful aliases:

- `tw` - Navigate to TimeWarp.Architecture directory
- `run` - Execute `dev run` (starts Aspire)
- `test` - Execute `dev test`
- `build` - Execute `dev build`
- `worktree` - Navigate to git worktree

## Port Forwarding

The following ports are automatically forwarded:

- **5147** - Web Blazor Server (auto-opens browser)
- **5100** - API Service
- **5200** - gRPC Service
- **5300** - YARP Gateway
- **15888** - Aspire Dashboard (auto-opens browser)
- **18889** - Aspire Dashboard gRPC

## Troubleshooting

### Container Build Fails

If the container build fails:

1. Check Docker is running and has sufficient resources
2. Ensure you have internet connectivity for package downloads
3. Try rebuilding without cache: "Dev Containers: Rebuild Container Without Cache"

### Claude Code Not Found

If Claude Code isn't available after container creation:

```bash
# Manual installation
npm install -g @anthropic-ai/claude-code
```

### Git Operations Fail

If git operations fail:

1. Ensure your SSH keys are properly configured on the host
2. Check that your .gitconfig exists on the host
3. Verify the git worktree mount is accessible

### Performance Issues

For better performance:

1. Increase Docker memory allocation (8GB+ recommended)
2. Use WSL2 backend on Windows
3. Ensure the project is on a local disk (not network drive)

## Customization

### Adding Tools

Edit the Dockerfile to add additional tools:

```dockerfile
# Add your tools here
RUN apt-get update && apt-get install -y <package-name>
```

### VS Code Extensions

Add extensions to devcontainer.json:

```json
"customizations": {
  "vscode": {
    "extensions": [
      "extension.id.here"
    ]
  }
}
```

### Environment Variables

Add environment variables to devcontainer.json:

```json
"containerEnv": {
  "MY_VAR": "value"
}
```

## Security

This dev container implements network security following Claude Code's best practices:

- **Firewall Rules**: Restricts outbound connections to whitelisted domains only
- **Allowed Domains**: GitHub, npm registry, Anthropic API, Microsoft/NuGet services
- **Default Deny**: All other network connections are blocked
- **Local Access**: Host network and localhost connections are permitted

**Important**: Only use this dev container with trusted repositories. When running with `--dangerously-skip-permissions`, the container cannot prevent malicious code from accessing Claude Code credentials.

## Known Limitations

1. File watching may be slower in containers - the config uses polling mode
2. First build takes longer due to image creation
3. Some host-specific tools may not work inside the container
4. Network access is restricted by firewall - some external services may be blocked

## Support

For issues with:
- Dev Container setup: Check VS Code Dev Containers documentation
- Claude Code: Visit https://github.com/anthropics/claude-code/issues
- TimeWarp Architecture: See main project documentation
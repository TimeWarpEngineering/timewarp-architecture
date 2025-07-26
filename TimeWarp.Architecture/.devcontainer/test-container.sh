#!/bin/bash
echo "Testing dev container build..."

# Build the container
docker build -t timewarp-devcontainer .devcontainer/

# Run a test command
echo "Testing installed tools:"
docker run --rm timewarp-devcontainer /bin/bash -c "
    echo '=== .NET Version ==='
    dotnet --version
    echo '=== PowerShell Version ==='
    pwsh --version
    echo '=== Node Version ==='
    node --version
    echo '=== Claude Code ==='
    claude-code --version || echo 'Claude Code not found'
    echo '=== Docker ==='
    docker --version || echo 'Docker not available'
"
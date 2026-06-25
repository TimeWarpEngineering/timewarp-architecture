#!/bin/bash
set -e

echo "🔧 Running post-create setup..."

# Initialize firewall for security
if [ -f "/workspace/timewarp-architecture/.devcontainer/init-firewall.sh" ]; then
    echo "🔒 Initializing security firewall..."
    sudo /workspace/timewarp-architecture/.devcontainer/init-firewall.sh || echo "⚠️  Firewall initialization failed, continuing..."
fi

# Navigate to the workspace
cd /workspace/timewarp-architecture

# Update workloads to prevent verification warnings
echo "🔧 Updating .NET workloads..."
dotnet workload update --skip-sign-check || echo "Workload update completed"

# Just restore .NET packages to populate the package cache
echo "📦 Restoring .NET package cache..."
dotnet restore || echo "Package restore completed (some packages may be unavailable offline)"

# Set up git worktree symlink if needed
if [ -d "/workspace/git-worktree" ] && [ ! -L "/workspace/git-worktree-link" ]; then
    ln -s /workspace/git-worktree /workspace/git-worktree-link
    echo "🔗 Created git worktree symlink"
fi

# Verify Claude is installed - CRITICAL for agentic workflow
if command -v claude &> /dev/null; then
    echo "✅ Claude is installed and available"
    claude --version || echo "Claude installed but version check failed"
else
    echo "❌ CRITICAL ERROR: Claude not found!"
    echo "   The container build failed to install Claude properly."
    echo "   This dev container is not functional for agentic workflows."
    exit 1
fi

# Create helpful aliases
cat >> ~/.bashrc << 'EOF'

# TimeWarp Architecture aliases
alias tw='cd /workspace/timewarp-architecture'
alias run='dev run'
alias test='dev test'
alias build='dev build'

# Git worktree alias
alias worktree='cd /workspace/git-worktree'

EOF

echo "✅ Post-create setup complete!"
echo ""
echo "📝 Quick tips:"
echo "  - Use 'tw' to navigate to TimeWarp.Architecture"
echo "  - Use 'run' to start the Aspire orchestrator"
echo "  - Use 'test' to run all tests"
echo "  - Use 'worktree' to access the git worktree"
echo "  - Claude is available via 'claude' command"
echo ""
echo "🧪 Running validation tests..."
echo ""
/workspace/timewarp-architecture/.devcontainer/validate-container.sh || echo "⚠️  Some validation tests failed, but continuing..."
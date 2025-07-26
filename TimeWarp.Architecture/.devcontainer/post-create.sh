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

# Make all PowerShell scripts executable
chmod +x *.ps1 2>/dev/null || true

# Install npm dependencies for Web.Spa if they exist
if [ -f "Source/ContainerApps/Web/Web.Spa/package.json" ]; then
    echo "📦 Installing npm dependencies for Web.Spa..."
    cd Source/ContainerApps/Web/Web.Spa
    npm install
    cd /workspace/timewarp-architecture
fi

# Restore .NET dependencies
echo "📦 Restoring .NET dependencies..."
dotnet restore

# Build the solution to ensure everything is set up
echo "🔨 Building the solution..."
./Build.ps1 || echo "Initial build may have warnings, continuing..."

# Set up git worktree symlink if needed
if [ -d "/workspace/git-worktree" ] && [ ! -L "/workspace/git-worktree-link" ]; then
    ln -s /workspace/git-worktree /workspace/git-worktree-link
    echo "🔗 Created git worktree symlink"
fi

# Configure Claude Code if needed
if command -v claude-code &> /dev/null; then
    echo "✅ Claude Code is installed and available"
    claude-code --version || echo "Claude Code installed but version check failed"
else
    echo "⚠️  Claude Code not found. You may need to install it manually."
fi

# Create helpful aliases
cat >> ~/.bashrc << 'EOF'

# TimeWarp Architecture aliases
alias tw='cd /workspace/timewarp-architecture'
alias run='./Run.ps1'
alias test='./RunTests.ps1'
alias build='./Build.ps1'
alias tailwind='./RunTailwind.ps1'

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
echo "  - Claude Code should be available via 'claude-code' command"
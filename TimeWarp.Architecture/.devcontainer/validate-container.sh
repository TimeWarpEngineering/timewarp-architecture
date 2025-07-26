#!/bin/bash
set -e

echo "🧪 Validating TimeWarp Dev Container Setup..."
echo "============================================"

# Color codes
GREEN='\033[0;32m'
RED='\033[0;31m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Test counters
PASSED=0
FAILED=0

# Test function
test_command() {
    local name="$1"
    local command="$2"
    local expected="$3"
    
    echo -n "Testing $name... "
    if eval "$command" >/dev/null 2>&1; then
        echo -e "${GREEN}✓${NC}"
        ((PASSED++))
        return 0
    else
        echo -e "${RED}✗${NC}"
        echo "  Command failed: $command"
        ((FAILED++))
        return 1
    fi
}

# Test function with output
test_with_output() {
    local name="$1"
    local command="$2"
    
    echo -n "Testing $name... "
    if output=$(eval "$command" 2>&1); then
        echo -e "${GREEN}✓${NC}"
        echo "  Output: $output"
        ((PASSED++))
        return 0
    else
        echo -e "${RED}✗${NC}"
        echo "  Error: $output"
        ((FAILED++))
        return 1
    fi
}

echo -e "\n📦 Core Tools:"
test_command ".NET SDK" "dotnet --version"
test_command "PowerShell" "pwsh --version"
test_command "Node.js" "node --version"
test_command "npm" "npm --version"
test_command "Git" "git --version"

echo -e "\n🛠️ Development Tools:"
test_command "Claude Code" "command -v claude-code"
test_command "Tailwind CSS" "command -v tailwindcss"
test_command "TypeScript" "command -v tsc"
test_command "ESLint" "command -v eslint"
test_command "Prettier" "command -v prettier"

echo -e "\n🐳 Docker Integration:"
test_command "Docker CLI" "docker --version"
test_command "Docker daemon connection" "docker version --format '{{.Server.Version}}'"
test_command "List containers" "docker ps"
test_command "Docker socket readable" "[ -r /var/run/docker.sock ]"

# Test if we're in the docker group
echo -n "Testing Docker group membership... "
if groups | grep -q docker; then
    echo -e "${GREEN}✓${NC}"
    ((PASSED++))
else
    echo -e "${YELLOW}⚠${NC} (may need sudo for docker commands)"
fi

echo -e "\n📁 File Permissions:"
test_command "Workspace writable" "touch /workspace/timewarp-architecture/.test-write && rm /workspace/timewarp-architecture/.test-write"
test_command "Claude config writable" "[ -w /home/vscode/.claude ]"
test_command "PowerShell scripts executable" "[ -x /workspace/timewarp-architecture/Build.ps1 ]"

echo -e "\n🌐 Network & Security:"
# Test allowed domains
echo -n "Testing npm registry access... "
if curl -s --connect-timeout 3 https://registry.npmjs.org >/dev/null 2>&1; then
    echo -e "${GREEN}✓${NC}"
    ((PASSED++))
else
    echo -e "${YELLOW}⚠${NC} (firewall may be active)"
fi

echo -n "Testing GitHub access... "
if curl -s --connect-timeout 3 https://api.github.com/zen >/dev/null 2>&1; then
    echo -e "${GREEN}✓${NC}"
    ((PASSED++))
else
    echo -e "${RED}✗${NC} (required for git operations)"
    ((FAILED++))
fi

echo -e "\n🔧 Aspire Readiness:"
test_command ".NET Aspire workload" "dotnet workload list | grep -q aspire || echo 'Aspire workload not installed'"
test_command "Docker for Aspire deps" "docker version --format '{{.Server.Version}}' | grep -E '[0-9]+\.[0-9]+'"

# Test creating a test container (proves Docker works)
echo -n "Testing container creation... "
if docker run --rm hello-world >/dev/null 2>&1; then
    echo -e "${GREEN}✓${NC} (Docker can create containers)"
    ((PASSED++))
else
    echo -e "${RED}✗${NC} (Aspire won't be able to run dependencies)"
    ((FAILED++))
fi

echo -e "\n============================================"
echo -e "Results: ${GREEN}$PASSED passed${NC}, ${RED}$FAILED failed${NC}"

if [ $FAILED -eq 0 ]; then
    echo -e "${GREEN}✅ Dev container is fully functional!${NC}"
    exit 0
else
    echo -e "${RED}❌ Some tests failed. Check the output above.${NC}"
    exit 1
fi
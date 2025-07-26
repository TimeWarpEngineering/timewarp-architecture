# Start with Node.js base for Claude Code compatibility
FROM node:20

ARG TZ="America/Los_Angeles"
ENV TZ="$TZ"

# Install basic development tools and security tools
RUN apt update && apt install -y \
    less git procps sudo fzf zsh man-db unzip gnupg2 gh \
    iptables ipset iproute2 dnsutils aggregate jq \
    vim htop ripgrep fd-find \
    wget curl ca-certificates software-properties-common \
    && apt clean -y && rm -rf /var/lib/apt/lists/*

# Install .NET SDKs
RUN wget https://dot.net/v1/dotnet-install.sh -O dotnet-install.sh \
    && chmod +x ./dotnet-install.sh \
    && ./dotnet-install.sh --channel 9.0 --install-dir /usr/share/dotnet \
    && ./dotnet-install.sh --channel 8.0 --install-dir /usr/share/dotnet \
    && rm ./dotnet-install.sh \
    && ln -s /usr/share/dotnet/dotnet /usr/local/bin/dotnet

# Install .NET 10 Preview manually from direct download
RUN wget https://builds.dotnet.microsoft.com/dotnet/Sdk/10.0.100-preview.6.25358.103/dotnet-sdk-10.0.100-preview.6.25358.103-linux-x64.tar.gz \
    && mkdir -p /usr/share/dotnet \
    && tar -zxf dotnet-sdk-10.0.100-preview.6.25358.103-linux-x64.tar.gz -C /usr/share/dotnet \
    && rm dotnet-sdk-10.0.100-preview.6.25358.103-linux-x64.tar.gz

# Install PowerShell for Debian 12 (bookworm)
RUN apt-get update \
    && apt-get install -y ca-certificates curl \
    && curl -sSL https://packages.microsoft.com/keys/microsoft.asc | gpg --dearmor > /usr/share/keyrings/microsoft.gpg \
    && echo "deb [arch=amd64 signed-by=/usr/share/keyrings/microsoft.gpg] https://packages.microsoft.com/repos/microsoft-debian-bookworm-prod bookworm main" > /etc/apt/sources.list.d/microsoft.list \
    && apt-get update \
    && apt-get install -y powershell \
    && apt-get clean -y && rm -rf /var/lib/apt/lists/*

# Create vscode user with UID 1000 (replacing the node user)
RUN userdel -r node \
    && useradd -ms /bin/bash -u 1000 vscode \
    && usermod -aG sudo vscode \
    && echo 'vscode ALL=(ALL) NOPASSWD:ALL' >> /etc/sudoers

# Set up npm permissions for vscode user
RUN mkdir -p /usr/local/share/npm-global \
    && chown -R vscode:vscode /usr/local/share

# Switch to vscode user for npm installations
USER vscode

# Install Claude Code and global npm packages
RUN npm config set prefix /usr/local/share/npm-global \
    && npm install -g \
        @anthropic-ai/claude-code \
        tailwindcss \
        typescript \
        prettier \
        eslint

# Add npm global bin to PATH
ENV PATH="/usr/local/share/npm-global/bin:${PATH}"

# Switch back to root for system configuration
USER root

# Set up git safe directories
RUN git config --system --add safe.directory /workspace/timewarp-architecture \
    && git config --system --add safe.directory /workspace/git-worktree

# Create workspace directories
RUN mkdir -p /workspace/timewarp-architecture /workspace/git-worktree /commandhistory \
    && chown -R vscode:vscode /workspace /commandhistory

# Copy and set up firewall script
COPY init-firewall.sh /usr/local/bin/
RUN chmod +x /usr/local/bin/init-firewall.sh

# Install Docker CLI only (not the daemon) to communicate with host Docker
RUN apt-get update && apt-get install -y \
    apt-transport-https \
    ca-certificates \
    curl \
    gnupg \
    lsb-release \
    && curl -fsSL https://download.docker.com/linux/debian/gpg | gpg --dearmor -o /usr/share/keyrings/docker-archive-keyring.gpg \
    && echo "deb [arch=$(dpkg --print-architecture) signed-by=/usr/share/keyrings/docker-archive-keyring.gpg] https://download.docker.com/linux/debian $(lsb_release -cs) stable" | tee /etc/apt/sources.list.d/docker.list > /dev/null \
    && apt-get update \
    && apt-get install -y docker-ce-cli \
    && apt-get clean -y && rm -rf /var/lib/apt/lists/*

# Ensure vscode user has permissions for Docker socket and sudo
RUN usermod -aG sudo vscode && echo 'vscode ALL=(ALL) NOPASSWD:ALL' >> /etc/sudoers \
    && (groupadd -g 999 docker 2>/dev/null || groupadd docker 2>/dev/null || true) \
    && usermod -aG docker vscode || true

# Switch to vscode user
USER vscode

# Set environment variables
ENV DOTNET_CLI_TELEMETRY_OPTOUT=1 \
    DOTNET_NOLOGO=true \
    POWERSHELL_TELEMETRY_OPTOUT=1 \
    DOTNET_ROOT=/usr/share/dotnet \
    PATH="/usr/share/dotnet:/usr/local/share/npm-global/bin:${PATH}" \
    NODE_OPTIONS="--max-old-space-size=4096"

# Keep container running
CMD ["sleep", "infinity"]
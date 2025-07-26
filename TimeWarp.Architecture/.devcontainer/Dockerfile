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

# Set up node user permissions
RUN mkdir -p /usr/local/share/npm-global \
    && chown -R node:node /usr/local/share

# Create vscode user (for .NET dev containers compatibility)
RUN useradd -ms /bin/bash vscode \
    && usermod -aG sudo vscode \
    && echo 'vscode ALL=(ALL) NOPASSWD:ALL' >> /etc/sudoers

# Switch to node user for npm installations
USER node

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

# Ensure vscode user has sudo permissions for any needed operations
RUN usermod -aG sudo vscode && echo 'vscode ALL=(ALL) NOPASSWD:ALL' >> /etc/sudoers

# Switch to vscode user
USER vscode

# Set environment variables
ENV DOTNET_CLI_TELEMETRY_OPTOUT=1 \
    DOTNET_NOLOGO=true \
    POWERSHELL_TELEMETRY_OPTOUT=1 \
    DOTNET_ROOT=/usr/share/dotnet \
    PATH="/usr/share/dotnet:/usr/local/share/npm-global/bin:${PATH}" \
    NODE_OPTIONS="--max-old-space-size=4096"
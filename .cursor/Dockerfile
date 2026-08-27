# Cursor Cloud Agent image for timewarp-architecture (and reusable TimeWarp app shape).
# Toolchain only: Ubuntu 24.04, .NET 10, git, sudo, Docker-in-Docker, Aspire CLI.
# Do not COPY the repository — Cursor checks out the requested revision after the image is built.
FROM ubuntu:24.04

ENV DEBIAN_FRONTEND=noninteractive \
    DOTNET_ROOT=/usr/share/dotnet \
    DOTNET_NOLOGO=1 \
    DOTNET_CLI_TELEMETRY_OPTOUT=1 \
    DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1 \
    PATH="/usr/share/dotnet:/usr/local/share/dotnet-tools:${PATH}"

COPY bootstrap-toolchain.sh /tmp/bootstrap-toolchain.sh
RUN chmod +x /tmp/bootstrap-toolchain.sh \
    && /tmp/bootstrap-toolchain.sh \
    && rm -f /tmp/bootstrap-toolchain.sh \
    && rm -rf /var/lib/apt/lists/*

USER ubuntu
WORKDIR /workspace

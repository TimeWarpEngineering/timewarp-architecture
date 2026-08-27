#!/usr/bin/env bash
# Idempotent Cloud Agent install: repo-derived restore after checkout.
# Do not start Docker, Aspire, or any other long-running process here.
set -euo pipefail

export DOTNET_NOLOGO=1
export DOTNET_CLI_TELEMETRY_OPTOUT=1
export DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1

if ! command -v dotnet >/dev/null 2>&1; then
  echo "dotnet is missing. The Cloud Agent image (.cursor/Dockerfile) must supply .NET 10." >&2
  exit 1
fi

echo "dotnet $(dotnet --version)"
dotnet restore timewarp-architecture.slnx
dotnet tool restore
dotnet run tools/dev-cli/dev.cs -- self-install
dotnet dev-certs https --trust || true

if command -v aspire >/dev/null 2>&1; then
  echo "aspire $(aspire --version 2>/dev/null || true)"
fi

echo "Cloud Agent install complete."

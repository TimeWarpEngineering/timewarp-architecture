#!/bin/bash
# Purpose: provision a Claude Code on the web container so `dev build`, `dev test`, and
# `dev run` work: pinned .NET SDK, Aspire CLI, Docker daemon, restored packages, the AOT
# `dev` CLI, and (best-effort) `ganda` built from source.
#
# Design:
# - Web-only (CLAUDE_CODE_REMOTE=true); local machines keep their own toolchains.
# - Idempotent: each step checks before acting, so resumes and cached containers are cheap.
# - The SDK comes from the official install script driven by global.json. Ubuntu's apt
#   dotnet-sdk-10.0 is a 1xx-band build whose Roslyn is too old for the pinned analyzers
#   (CS9057). Requires builds.dotnet.microsoft.com in the environment's network allowlist.
# - Aspire CLI version tracks the AppHost's Aspire.AppHost.Sdk pin.
# - The Aspire CLI crashes rendering its first-run banner without a TTY; touching the
#   first-use sentinel skips the banner so `dev run` works from a non-interactive shell.
# - ganda is not on nuget.org (private operator tooling) and the web container cannot
#   reach GitHub Packages, so it is built from a timewarp-ganda clone with that feed
#   disabled locally. Skipped with a warning when the repo is not reachable from the
#   session. `ganda kanban create/reserve` still cannot push refs/ganda/claims from a
#   cloud session (git proxy allows only the session branch); `ganda repo audit` works.
set -euo pipefail

if [ "${CLAUDE_CODE_REMOTE:-}" != "true" ]; then
  exit 0
fi

REPO_ROOT="${CLAUDE_PROJECT_DIR:-$(git rev-parse --show-toplevel)}"
cd "$REPO_ROOT"

DOTNET_DIR="$HOME/.dotnet"
GANDA_BIN_DIR="$HOME/.timewarp/bin"
GANDA_SRC="$HOME/.cache/timewarp-ganda"

export DOTNET_ROOT="$DOTNET_DIR"
export PATH="$REPO_ROOT/bin:$DOTNET_DIR:$DOTNET_DIR/tools:$GANDA_BIN_DIR:$PATH"
export DOTNET_NOLOGO=1
export DOTNET_CLI_TELEMETRY_OPTOUT=1
export ASPIRE_CLI_TELEMETRY_OPTOUT=1

log() { echo "[session-start] $*" >&2; }

# ── .NET SDK (exact global.json pin) ─────────────────────────────────────────────────────
sdk_version=$(sed -n 's/.*"version": *"\([^"]*\)".*/\1/p' global.json | head -1)
if ! "$DOTNET_DIR/dotnet" --list-sdks 2>/dev/null | grep -q "^$sdk_version "; then
  log "Installing .NET SDK $sdk_version"
  installer=$(mktemp)
  curl -sSL https://dot.net/v1/dotnet-install.sh -o "$installer"
  bash "$installer" --jsonfile global.json --install-dir "$DOTNET_DIR" >&2
  rm -f "$installer"
fi

# ── Aspire CLI (matches Aspire.AppHost.Sdk pin) ──────────────────────────────────────────
aspire_version=$(sed -n 's/.*Sdk="Aspire.AppHost.Sdk\/\([^"]*\)".*/\1/p' \
  source/container-apps/aspire/projects/aspire-app-host/aspire-app-host.csproj | head -1)
if ! aspire --version 2>/dev/null | grep -q "^$aspire_version"; then
  log "Installing Aspire CLI $aspire_version"
  dotnet tool update -g Aspire.Cli --version "$aspire_version" >&2
fi
mkdir -p "$HOME/.aspire/cli"
touch "$HOME/.aspire/cli/cli.firstUseSentinel"

# ── Docker daemon (Aspire runs postgres as a container) ──────────────────────────────────
if command -v dockerd >/dev/null 2>&1 && ! docker info >/dev/null 2>&1; then
  log "Starting dockerd"
  nohup dockerd >/tmp/dockerd.log 2>&1 &
  for _ in $(seq 1 30); do
    docker info >/dev/null 2>&1 && break
    sleep 1
  done
  docker info >/dev/null 2>&1 || log "WARNING: dockerd did not start (see /tmp/dockerd.log)"
fi

# ── Package restore (warms ~/.nuget/packages for the cached container) ───────────────────
log "Restoring timewarp-architecture.slnx"
dotnet restore timewarp-architecture.slnx >&2

# ── dev CLI (AOT binary in ./bin, rebuilt when its sources change) ───────────────────────
if [ ! -x bin/dev ] || [ -n "$(find tools/dev-cli -newer bin/dev -type f -print -quit)" ]; then
  log "Building dev CLI"
  dotnet run tools/dev-cli/dev.cs -- self-install >&2
fi

# ── ganda (best-effort, from source) ─────────────────────────────────────────────────────
if [ ! -x "$GANDA_BIN_DIR/ganda" ]; then
  if [ ! -d "$GANDA_SRC/.git" ]; then
    git clone --depth 1 https://github.com/TimeWarpEngineering/timewarp-ganda "$GANDA_SRC" >&2 \
      || log "WARNING: could not clone timewarp-ganda; ganda unavailable this session"
  fi
  if [ -d "$GANDA_SRC/.git" ]; then
    log "Building ganda"
    (
      cd "$GANDA_SRC"
      dotnet nuget disable source github-timewarp --configfile nuget.config >/dev/null 2>&1 || true
      dotnet run tools/dev-cli/dev.cs -- self-install >&2
      ./bin/dev build-ganda >&2
    ) || log "WARNING: ganda build failed; ganda unavailable this session"
  fi
fi

# ── Persist environment for the session ──────────────────────────────────────────────────
if [ -n "${CLAUDE_ENV_FILE:-}" ]; then
  {
    echo "export DOTNET_ROOT=\"$DOTNET_DIR\""
    echo "export PATH=\"$REPO_ROOT/bin:$DOTNET_DIR:$DOTNET_DIR/tools:$GANDA_BIN_DIR:\$PATH\""
    echo "export DOTNET_NOLOGO=1"
    echo "export DOTNET_CLI_TELEMETRY_OPTOUT=1"
    echo "export ASPIRE_CLI_TELEMETRY_OPTOUT=1"
  } >> "$CLAUDE_ENV_FILE"
fi

log "Done"

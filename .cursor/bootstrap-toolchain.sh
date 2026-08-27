#!/usr/bin/env bash
# Idempotent host toolchain for Cursor Cloud Agents and the .cursor/Dockerfile.
# Installs Ubuntu packages + .NET 10 (global.json channel) + Docker CE (DinD) + Aspire CLI.
# Safe to re-run. Does not start dockerd and does not copy application source.
set -euo pipefail

if [ "$(id -u)" -ne 0 ]; then
  echo "bootstrap-toolchain.sh must run as root (sudo bash .cursor/bootstrap-toolchain.sh)" >&2
  exit 1
fi

export DEBIAN_FRONTEND=noninteractive
export DOTNET_NOLOGO=1
export DOTNET_CLI_TELEMETRY_OPTOUT=1
export DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1

DOTNET_ROOT="${DOTNET_ROOT:-/usr/share/dotnet}"
ASPIRE_TOOL_PATH="${ASPIRE_TOOL_PATH:-/usr/local/share/dotnet-tools}"

APT_DPKG_OPTS=(-o Dpkg::Options::=--force-confdef -o Dpkg::Options::=--force-confold)

install_base_packages() {
  apt-get update
  apt-get "${APT_DPKG_OPTS[@]}" install -y --no-install-recommends \
    ca-certificates \
    curl \
    git \
    gnupg \
    sudo \
    fuse-overlayfs \
    iptables
}

install_dotnet() {
  if command -v dotnet >/dev/null 2>&1; then
    local version
    version="$(dotnet --version 2>/dev/null || true)"
    case "${version}" in
      10.*)
        echo "dotnet ${version} already present"
        return 0
        ;;
    esac
  fi

  curl -fsSL https://dot.net/v1/dotnet-install.sh -o /tmp/dotnet-install.sh
  bash /tmp/dotnet-install.sh --channel 10.0 --install-dir "${DOTNET_ROOT}"
  rm -f /tmp/dotnet-install.sh
  ln -sfn "${DOTNET_ROOT}/dotnet" /usr/local/bin/dotnet
}

write_dotnet_path() {
  mkdir -p /etc/profile.d
  cat > /etc/profile.d/dotnet.sh <<EOF
export DOTNET_ROOT=${DOTNET_ROOT}
export PATH="${DOTNET_ROOT}:${ASPIRE_TOOL_PATH}:\$PATH"
export DOTNET_NOLOGO=1
export DOTNET_CLI_TELEMETRY_OPTOUT=1
export DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1
EOF
  chmod 644 /etc/profile.d/dotnet.sh

  if [ -f /etc/environment ]; then
    grep -q '^DOTNET_ROOT=' /etc/environment || echo "DOTNET_ROOT=${DOTNET_ROOT}" >> /etc/environment
  fi
}

install_docker() {
  install -m 0755 -d /etc/apt/keyrings
  if [ ! -f /etc/apt/keyrings/docker.gpg ]; then
    curl --retry 3 --retry-delay 5 -fsSL https://download.docker.com/linux/ubuntu/gpg \
      | gpg --dearmor -o /etc/apt/keyrings/docker.gpg
    chmod a+r /etc/apt/keyrings/docker.gpg
  fi

  local codename
  codename="$(. /etc/os-release && echo "${VERSION_CODENAME}")"
  echo "deb [arch=$(dpkg --print-architecture) signed-by=/etc/apt/keyrings/docker.gpg] https://download.docker.com/linux/ubuntu ${codename} stable" \
    > /etc/apt/sources.list.d/docker.list

  apt-get update
  apt-get "${APT_DPKG_OPTS[@]}" install -y --no-install-recommends \
    docker-ce \
    docker-ce-cli \
    containerd.io \
    docker-buildx-plugin \
    docker-compose-plugin

  mkdir -p /etc/docker
  cat > /etc/docker/daemon.json <<'EOF'
{
  "storage-driver": "fuse-overlayfs"
}
EOF

  if [ -x /usr/sbin/iptables-legacy ]; then
    update-alternatives --set iptables /usr/sbin/iptables-legacy || true
  fi
  if [ -x /usr/sbin/ip6tables-legacy ]; then
    update-alternatives --set ip6tables /usr/sbin/ip6tables-legacy || true
  fi
}

configure_ubuntu_user() {
  id -u ubuntu >/dev/null 2>&1 || useradd -m -s /bin/bash ubuntu
  groupadd -f docker
  usermod -aG docker ubuntu
  usermod -aG sudo ubuntu
  echo "ubuntu ALL=(ALL) NOPASSWD:ALL" > /etc/sudoers.d/ubuntu
  chmod 440 /etc/sudoers.d/ubuntu
}

install_aspire_cli() {
  export DOTNET_ROOT
  export PATH="${DOTNET_ROOT}:${PATH}"
  mkdir -p "${ASPIRE_TOOL_PATH}"
  if [ -x "${ASPIRE_TOOL_PATH}/aspire" ]; then
    echo "Aspire CLI already present at ${ASPIRE_TOOL_PATH}/aspire"
  else
    "${DOTNET_ROOT}/dotnet" tool install Aspire.Cli --tool-path "${ASPIRE_TOOL_PATH}"
  fi
  chmod -R a+rX "${ASPIRE_TOOL_PATH}"
  ln -sfn "${ASPIRE_TOOL_PATH}/aspire" /usr/local/bin/aspire
}

install_base_packages
install_dotnet
write_dotnet_path
install_docker
configure_ubuntu_user
install_aspire_cli

echo "Toolchain ready:"
"${DOTNET_ROOT}/dotnet" --info | sed -n '1,20p'
docker --version
aspire --version || "${ASPIRE_TOOL_PATH}/aspire" --version
git --version

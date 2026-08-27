#!/usr/bin/env bash
# Per-boot Docker daemon for Aspire / container tests. Must return after readiness.
set -euo pipefail

docker_ready() {
  docker info >/dev/null 2>&1 || sudo docker info >/dev/null 2>&1
}

print_docker_version() {
  if docker info >/dev/null 2>&1; then
    docker version --format '{{.Server.Version}}'
  else
    sudo docker version --format '{{.Server.Version}}'
  fi
}

if docker_ready; then
  echo "Docker daemon already running."
  print_docker_version
  exit 0
fi

if command -v service >/dev/null 2>&1; then
  sudo service docker start || true
fi

if ! docker_ready; then
  sudo dockerd --host=unix:///var/run/docker.sock >/tmp/dockerd.log 2>&1 &
fi

for _ in $(seq 1 40); do
  if docker_ready; then
    echo "Docker daemon is ready."
    print_docker_version
    exit 0
  fi
  sleep 1
done

echo "Docker daemon failed to become ready." >&2
if [ -f /tmp/dockerd.log ]; then
  tail -n 50 /tmp/dockerd.log >&2 || true
fi
exit 1

#!/usr/bin/env bash
# Per-boot Docker daemon for Aspire / container tests. Must return after readiness.
set -euo pipefail

if docker info >/dev/null 2>&1; then
  echo "Docker daemon already running."
  docker version --format '{{.Server.Version}}'
  exit 0
fi

if command -v service >/dev/null 2>&1; then
  sudo service docker start || true
fi

if ! docker info >/dev/null 2>&1; then
  sudo dockerd --host=unix:///var/run/docker.sock >/tmp/dockerd.log 2>&1 &
fi

for _ in $(seq 1 40); do
  if docker info >/dev/null 2>&1; then
    echo "Docker daemon is ready."
    docker version --format '{{.Server.Version}}'
    exit 0
  fi
  sleep 1
done

echo "Docker daemon failed to become ready." >&2
if [ -f /tmp/dockerd.log ]; then
  tail -n 50 /tmp/dockerd.log >&2 || true
fi
exit 1

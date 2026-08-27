# Cursor Cloud Agent environment

Git-owned Cloud Agent environment for `timewarp-architecture`. Future TimeWarp
app repos can reuse this shape.

## What is in git

| File | Role |
|------|------|
| `environment.json` | Cursor Cloud config. `dockerfile` / `context` are relative to `.cursor`. |
| `Dockerfile` | Ubuntu 24.04 + .NET 10 + git + sudo + Docker-in-Docker + Aspire CLI. |
| `bootstrap-toolchain.sh` | Idempotent toolchain installer used by the Dockerfile and first-run VMs. |
| `install.sh` | After checkout: restore the solution, restore local tools, self-install `bin/dev`. |
| `start.sh` | Per-boot: start `dockerd` so Aspire can run containers. |

The image does **not** `COPY` application source. Cursor checks out the requested
revision, then runs `install`.

## How to reuse on another TimeWarp repo

1. Copy this `.cursor/` directory into the other repo.
2. Keep `Dockerfile` / `bootstrap-toolchain.sh` unless that repo needs a different
   OS or SDK channel. SDK channel must stay aligned with that repo's `global.json`.
3. Edit `install.sh` to match that repo's bootstrap (`dotnet restore <sln/slnx>`,
   `dotnet run tools/dev-cli/dev.cs -- self-install` when a `dev` CLI exists).
4. Keep `start.sh` if the repo uses Aspire, Testcontainers, or any Docker-backed
   host. Drop it only when the repo never starts containers.
5. Commit `.cursor/` and open a PR. Agents that start on that revision pick up
   `.cursor/environment.json` first (ahead of personal or team dashboard envs).

Do not copy the Amina AOA image. That image adds extra agent CLIs, an `amina`
user, `docker.sock` GID mapping, and a `sleep infinity` entrypoint — wrong for
Cursor Cloud.

## What stays dashboard-only

- Secrets and environment-scoped credentials (NuGet tokens, cloud keys, test logins)
- Network allowlists / egress policy
- Saved snapshots and Builds created from the dashboard
- Team vs personal environment assignment

A dashboard snapshot is a convenience disk image. It is not a substitute for
committing `.cursor/` when other repos or branches need the same toolchain.

## Commands this environment is built for

From the repo root, same pipeline as `AGENTS.md`:

```bash
dotnet run tools/dev-cli/dev.cs -- build   # or ./bin/dev build after self-install
dotnet run tools/dev-cli/dev.cs -- test    # needs Docker for some host suites
dotnet run tools/dev-cli/dev.cs -- run     # Aspire AppHost; needs Docker
```

`dev build` compiles `timewarp-architecture.slnx` (warnings are errors). The
AppHost SDK comes from NuGet (`Aspire.AppHost.Sdk`); Docker is required to
*run* Aspire resources, not to compile them.

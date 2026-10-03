# Replace Docker Desktop with WSL containers in the dev loop once Aspire supports wslc

## Description

WSL containers (`wslc.exe`, alias `container.exe`) went GA on 2026-09-29
(https://blogs.windows.com/windowsdeveloper/2026/09/29/wsl-containers-now-generally-available/).

- **Shipped:** the CLI, a Windows API, and networking (`wslc network create/connect`, the
  `consomme` mode, `--mount`). VS Code dev containers support it, and the post lists .NET Aspire
  as supporting WSL containers "as a first-class container runtime".
- **Not shipped:** `wsl compose up`, which is the stated next focus.
- **Not mentioned:** a Docker-compatible API or socket.

Steve's goal (2026-10-03): with Aspire support, the dev loop should not need Docker Desktop.
There is a concrete payoff on this machine: Docker Desktop repeatedly wipes the WSLInterop binfmt
registration, which breaks `code .`. A watchdog timer works around it today.

**This task is in backlog because its spec depends on answers we don't have yet.** Start with
the spike, then elaborate the task and move it to to-do.

## Known blockers (2026-10-03)

1. **Aspire 13.6 does not support it yet.** The docs list `ASPIRE_CONTAINER_RUNTIME` = `docker`
   (default) or `podman`, and `IContainerRuntime` (experimental, ASPIRECONTAINERRUNTIME001) has
   only Docker and Podman implementations. The blog's claim is either a later Aspire release or
   undocumented.
2. **Testcontainers.** The Postgres integration suites (role-store, site-settings table probe,
   …) use the Docker API. They need a Docker-compatible socket, or a Testcontainers provider for
   wslc.
3. **`dev db nuke` (tasks 266/269)** calls `docker ps`, `docker rm` and `docker volume rm`
   directly. Move it behind the Aspire runtime abstraction, or a small runtime switch.
4. **Where the AppHost runs.** It runs inside the WSL distro. Is `wslc` usable from inside a
   distro, or only from Windows?

CI (GitHub ubuntu runners with Docker) is unaffected either way.

## Spike (first step)

- [ ] Which Aspire release adds wslc as a container runtime, and how is it selected (env value,
      CLI setting)?
- [ ] Is `wslc` / its API usable from inside a WSL distro (where the AppHost and `dev` run)?
- [ ] Does wslc expose a Docker-compatible socket or API? Does Testcontainers .NET work against
      it, or is there a provider?
- [ ] Volumes and networking parity: named volumes (the postgres data volume identity from task
      266), the container network the YARP ingress uses, and the dashboard REPL (`WithRepl`).
- [ ] Record the findings in Notes, then elaborate this task (or split it) and move it to to-do.

## Likely work after the spike

- Select wslc in the dev loop: `dev run` or AppHost configuration, plus `aspire doctor` checks.
- Make `dev db nuke` runtime-agnostic.
- Testcontainers path (socket, provider, or keep Docker for tests only if unavoidable; record
  the decision).
- Update the skills that mention Docker Desktop as a prerequisite, and the `dev db` help text.
- Uninstall-Docker-Desktop validation checklist for the maintainer.

## Session

- Created: 557999 (2026-10-03)

## Notes

- Related: task 070 (Aspire publish). Its Compose output is a standard `compose.yaml` that
  Docker, Podman or `wsl compose` can all run, so 070 should go through the Aspire runtime
  setting and never hard-code the `docker` CLI in dev tooling.
- Related: task 269 (`dev db nuke` container cleanup), task 266 (postgres volume identity).

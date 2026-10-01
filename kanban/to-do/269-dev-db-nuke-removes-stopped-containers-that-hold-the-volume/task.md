# dev db nuke removes stopped containers that hold the volume

## Description

`dev db nuke --yes` (task 266, PR #420) failed on its first real use on 2026-10-01.
`aspire stop --force --volumes` succeeded, but the follow-up `docker volume rm
aspire-app-host-bbe41f2e49-postgres-data` failed with:

```
Error response from daemon: remove aspire-app-host-bbe41f2e49-postgres-data: volume is in use - [4314040f65cc…, cd7dcf8babde…, 71d11d30f574…, ef0446d92d8e…]
```

All four were **stopped** Aspire-created postgres containers (`postgres-<suffix>`, image
`postgres:18.3`, label `com.microsoft.developer.usvc-dev.group-version`). Three had
`Exited (255)` from AppHost runs that ended abruptly and were never cleaned up. One had
`Exited (0)`. Docker refuses to remove a volume while **any** container references it, including
stopped ones. The cockpit fixed it by hand: `docker rm` of the four, then a re-run that succeeded.

The command should handle this itself, safely.

## Requirements

1. **Before `docker volume rm`, for each volume to delete, list containers that reference it**
   (`docker ps -a --filter volume=<name>`).
   - **Stopped** (exited/created/dead): remove them (`docker rm`), print each one removed
     (id, name, status), then remove the volume.
   - **Running:** do **not** remove anything. Stop with a clear message naming the running
     container(s), and tell the user to stop the AppHost or container first. Never `docker rm -f`.
2. **The dry listing without `--yes` must show the stopped containers it would remove**, next to
   the volumes, so the user sees the full blast radius before confirming.
3. **Scope safety.** Only touch containers that reference **this checkout's** volumes, which the
   AppHost-prefix resolution from 266 already scopes. Never touch a sibling worktree's containers
   or volumes. Keep the "Docker unreachable → stop without acting" behavior.
4. Keep everything else from 266: the `--yes` gate, the version guard (shared `aspire-cli.cs`),
   the `aspire stop` arguments, and the handling of adopted pre-13.6 volumes.
5. **Tests**, without real Docker, using the stand-in-process pattern from 263/266. Pure helpers
   parse `docker ps -a` output and decide:
   - stopped-only: remove the containers, then the volume;
   - any running: refuse, remove nothing;
   - no containers: remove the volume directly;
   - the dry listing includes the containers.
6. Reconcile the Design regions in `db-nuke-command.cs` / `db-nuke.cs`, and update the AGENTS.md
   `dev db nuke` line if its wording changes.
7. **Stale binary:** rebuild `bin/dev` and gate on the fresh binary.

## Checklist

- [ ] Stopped containers referencing the volume are removed (listed), then the volume
- [ ] Any running container → refuse with a clear message; nothing removed; never `rm -f`
- [ ] Dry listing (no `--yes`) shows the containers too
- [ ] Scope limited to this checkout's volumes; Docker-unreachable still stops without acting
- [ ] Tests for the decision helpers (no real Docker)
- [ ] Design regions reconciled; AGENTS.md line updated if wording changes
- [ ] Gates: `dev build` 0/0, dev-cli tests, `ganda repo audit`; fresh `bin/dev`
- [ ] Do **not** run `dev db nuke --yes` for real, and do not start an AppHost (both affect the
      maintainer's data)
- [ ] Implementation review; host `open-pr`

## Session

- Created: 145993 (2026-10-01)

## Notes

- Follow-up to 266. Evidence: the maintainer's first run output, pasted in the cockpit
  2026-10-01, and `docker ps -a` showing the four exited `postgres-*` containers.
- Memory discipline: run builds serially; `dotnet build-server shutdown` before finishing.

## Results

*(fill when done)*

### How to validate

*(required before done)*

Maintainer, after merge (only when happy to lose dev data):
1. Leave a stopped postgres container behind, for example by killing an AppHost.
2. `dev db nuke` lists the volume and the container to remove.
3. `dev db nuke --yes` removes both.
4. With the AppHost running, `dev db nuke --yes` refuses and names the running container.

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

- [x] Stopped containers referencing the volume are removed (listed), then the volume
- [x] Any running container → refuse with a clear message; nothing removed; never `rm -f`
- [x] Dry listing (no `--yes`) shows the containers too
- [x] Scope limited to this checkout's volumes; Docker-unreachable still stops without acting
- [x] Tests for the decision helpers (no real Docker)
- [x] Design regions reconciled; AGENTS.md line updated if wording changes
- [x] Gates: `dev build` 0/0, dev-cli tests, `ganda repo audit`; fresh `bin/dev`
- [x] Do **not** run `dev db nuke --yes` for real, and do not start an AppHost (both affect the
      maintainer's data)
- [x] Implementation review (disposition: clean)
- [ ] Host `open-pr`

## Session

- Created: 145993 (2026-10-01)
- 2026-10-01 implement (ganda task work): helpers + command + tests + AGENTS.md line; gates green.
- 2026-10-01 review (ganda task work, effort 2, roster: general): 1 round, 4 findings fixed, disposition clean.

## Notes

- Follow-up to 266. Evidence: the maintainer's first run output, pasted in the cockpit
  2026-10-01, and `docker ps -a` showing the four exited `postgres-*` containers.
- Memory discipline: run builds serially; `dotnet build-server shutdown` before finishing.

## Results

- `tools/dev-cli/services/db-nuke.cs`: new pure helpers — `BuildContainerListArguments`
  (`docker ps --all --filter volume=<name> --format id\tname\tstate\tstatus`),
  `BuildContainerRemoveArguments` (plain `docker rm`, never `-f`), `ParseContainers`,
  `PlanContainerCleanup` (dedup by id; exited/created/dead → remove; anything else → block),
  `BuildRunningContainerRefusalLines`, `Describe`; records `VolumeContainer` /
  `ContainerCleanupPlan`. `BuildRefusalLines` now lists each volume's containers under it
  ("remove stopped container …" / "in use by …; must be stopped by the AppHost stop, else nuke refuses").
- `tools/dev-cli/endpoints/db-nuke-command.cs`: dry path lists containers per volume (Docker
  unreachable → stop). `--yes` path: after `aspire stop`, for the remaining (pre-listed) volumes,
  list containers; any running → print refusal naming them, exit 1, remove nothing; else
  `docker rm` the stopped ones (each printed), then `docker volume rm`. Containers are found per
  volume, so only containers mounting this checkout's prefixed volumes are considered.
- Tests (`tests/tools/dev-cli-tests/db-nuke-tests.cs`): parse, stopped-only, any-running refuses,
  paused/restarting block, no-containers, dedup, dry listing shows containers, rm never forces.
  dev-cli-tests 102/102.
- Design regions reconciled in both files; AGENTS.md `dev db nuke` line updated.
- Gates: `dev build` 0 warnings / 0 errors (fresh `bin/dev` via `self-install`), aspire-tests
  build (compile-includes db-nuke.cs), `ganda repo audit`. Fresh `bin/dev db nuke` dry run checked
  (no volumes in this worktree); real `docker ps` format output verified tab-separated.
  `dev db nuke --yes` was not run, and no AppHost was started.

### Review disposition

- Rounds: 1. Effort 2. Roster: general.
- Final counts: bug 0. Suggestion 2 fixed. Nit 2 fixed. 0 open, 0 wontfix.
- Disposition: **clean**. Fixes:
  - M1/M2: the refusal and `docker rm`-failure messages no longer say "nothing removed" after `aspire stop --volumes` has already run. They now say "No container or remaining volume was removed" / "The remaining volume(s) were not removed".
  - M3: the sweep message changed to "volume(s) `aspire stop` left behind".
  - M4: rewrapped the Design comment.
- Re-gated after the fixes: dev-cli-tests 102/102, `dev build` 0/0 on a fresh `bin/dev`, `ganda repo audit`.
- Artifacts: `review/review-framework.md`, `review/round-1/general.md`, `review/round-1/merged.md`, `review/disposition.md`.

### How to validate

Smoke (safe, no data loss): `dev self-install && dev db nuke` → exit 1, lists the AppHost stop,
each volume, and under each volume any containers (stopped: "remove stopped container <id> <name>
(<status>)"; running: "in use by …"). Expect: nothing is stopped or removed.
`cd tests/tools/dev-cli-tests && dotnet test -c Release` → Expect all pass, including
`ContainerCleanup_Given_`.

Maintainer, after merge (only when happy to lose dev data):
1. Leave a stopped postgres container behind, for example by killing an AppHost.
2. `dev db nuke` lists the volume and the container to remove.
3. `dev db nuke --yes` removes both.
4. With a container still running on the volume after the AppHost stop (for example a persistent
   container, or one started by hand with `docker start <id>`), `dev db nuke --yes` refuses, names
   the running container, and removes nothing. (A normal running AppHost is stopped first by
   `aspire stop`, so its own containers do not block.)

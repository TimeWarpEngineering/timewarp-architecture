# `dev db nuke` delegates to `aspire stop --volumes`: remove reconstructed Aspire volume names and the Docker sweep

## Description

`dev db nuke` (`tools/dev-cli/services/db-nuke.cs`, `endpoints/db-nuke-command.cs`) reconstructs
Aspire's **private** volume-naming scheme: it mirrors `VolumeNameGenerator` as
`{app}-{first 10 hex of SHA256(lower-cased .csproj path)}-{resource}-{suffix}`. It uses that to sweep
Docker volumes that Aspire "adopted" from before 13.6, plus the stopped containers that still mount
them (task 269). That couples the dev CLI to undocumented Aspire internals, and it is the same
anti-pattern just removed from `dev deprovision` (070-006).

Rule: dev-CLI wrappers **run the tool and react to its exit code**. They never pre-compute or
mirror a tool's private state.

## Requirements

- `dev db nuke [--yes]` becomes:
  1. Preflight: Aspire CLI ≥ 13.6. Keep the existing refusal text and update command.
  2. Without `--yes`: print what will happen ("stops the AppHost and deletes its Aspire-owned
     volumes") and exit without acting.
  3. With `--yes`: run `aspire stop --apphost <csproj> --force --volumes`. Exit with its exit code.
  4. After it runs, print one hint: a volume created before Aspire 13.6 may survive because Aspire
     adopted it rather than owning it. To remove it by hand, use `docker volume ls` and then
     `docker volume rm <name>` (honour `ASPIRE_CONTAINER_RUNTIME` for the CLI name; no hard-coded
     `docker` invocation in code).
- Delete:
  - the volume-name resolution (hash, prefix, application-name logic);
  - the Docker volume sweep;
  - the per-volume container lookup and `PlanContainerCleanup` (task 269 machinery);
  - their tests in `tests/tools/dev-cli-tests/db-nuke-tests.cs`;
  - any compile-include of `db-nuke.cs` in `tests/container-apps/aspire/aspire-tests/aspire-tests.csproj`.
- Keep the tests for argument building, the CLI-version refusal and the `--yes` gating.
- Check `postgres-volume-model-tests.cs`. If it only exists to prove the reconstructed volume name,
  delete it. If it guards something real in the AppHost model (for example the fixed published
  `postgres-data` volume from 070-003), keep that part and drop the hash assertions.
- Reconcile the Purpose and Design regions in every edited file, so no region still describes the
  removed approach.
- Update the `dev db nuke` description in `AGENTS.md` (Build / run / test). Drop the "adopted pre-13.6
  volume, after `docker rm` of stopped containers" clause and replace it with the manual-hint wording.
  Update the `tw-dev-cli` text in this repo if any.
- `dev db reset` is out of scope; leave it unchanged.

## Checklist

- [x] `dev db nuke` = preflight + `aspire stop --force --volumes` + manual hint
- [x] Hash/prefix/sweep/container-cleanup code and tests deleted
- [x] `postgres-volume-model-tests.cs` triaged
- [x] Regions + AGENTS.md reconciled
- [x] Gates: `dev build` 0/0, dev-cli-tests, aspire-tests build, `ganda repo audit`;
      `grep -rn "VolumeNameGenerator\|SHA256" tools/dev-cli` returns nothing

## Notes

- Workers never run `dev db nuke`, `dev run`, or anything that touches the maintainer's AppHost,
  containers or volumes. Verification is by tests and by running `dev db nuke` without `--yes`.
- Steve, 2026-10-07, on the 070-006 version of this pattern: "Why the hell did you need to
  decompile aspire and reconstruct some shit over complicated?"

## Session

- Created: 2026-10-07 (cockpit; found while simplifying 070-006 `dev deprovision`)
- 2026-10-07 implementer (ganda task work): nuke rewritten as preflight + `aspire stop` + hint.
- 2026-10-07 review oracle (ganda task work, Claude Opus 5.5): tw-implementation-review, effort 2, roster general; 1 round; disposition clean.

## Results

- `tools/dev-cli/services/db-nuke.cs` now holds only `BuildStopArguments`, `BuildRefusalLines`,
  `BuildAdoptedVolumeHintLines` and `ContainerRuntimeCli` (from `ASPIRE_CONTAINER_RUNTIME`, default
  `docker`). Gone: the `VolumeNamePrefix` hash and sanitizer, the volume list/filter/rm builders,
  `VolumeContainer`, `ContainerCleanupPlan`, `PlanContainerCleanup` and the container rm/refusal text.
- `db-nuke-command.cs`: repo root + AppHost → Aspire CLI ≥ 13.6 guard (unchanged text) → without
  `--yes`, prints the `aspire stop …` it would run and exits 1 → with `--yes`, runs
  `aspire stop --apphost <csproj> --force --volumes` and exits with its code, then prints the
  pre-13.6 adopted-volume hint. The command never calls a container CLI.
- `db-nuke-tests.cs`: kept the stop-argument and CLI-version tests. The `--yes` refusal test was
  rewritten for the new text. Added hint tests (default, `podman`, blank). Removed the
  prefix/filter/container tests.
- `postgres-volume-model-tests.cs` triaged: dropped only `DevDbNukePrefix_Should_MatchAspireGeneratedVolumeName`.
  The WithDataVolume identity, env-var and UseDataVolume=false tests guard the real AppHost model
  and stay. `aspire-tests.csproj` no longer compile-includes `db-nuke.cs` or defines `DEV_CLI_SOURCE`.
- Purpose/Design regions reconciled in all edited files. The `AGENTS.md` `dev db nuke` line was
  updated. No `tw-dev-cli` text in this repo mentions nuke, and `db-group.cs` was still accurate.
- Gates: `dev build` 0 warnings / 0 errors. dev-cli-tests 87/87. aspire-tests builds with 0/0.
  `ganda repo audit` passes. `grep -rn "VolumeNameGenerator\|SHA256" tools/dev-cli` finds nothing.
  `dev db nuke` without `--yes` was checked after `dev self-install`. `--yes` was not run.

### Review disposition

- Rounds: 1; effort 2 (by-diff budget); roster: general.
- Final counts: bug 0 · suggestion 0 · nit 0 (0 open / 0 fixed / 0 wontfix).
- Disposition: **clean** — no findings; reviewer re-ran dev-cli-tests (87/87), `ganda repo audit` (pass) and the SHA256/VolumeNameGenerator grep (empty).
- Artifacts: `review/review-framework.md`, `review/round-1/general.md`, `review/round-1/merged.md`, `review/disposition.md`.

### How to validate

Smoke (safe, no data loss):
```bash
dev self-install && dev db nuke; echo $?
```
Expect: exit 1. It prints "Refusing to nuke without --yes", the exact
`aspire stop --apphost …/aspire-app-host.csproj --force --volumes …` line, and the
`dev db reset --yes` alternative. Nothing is stopped or removed. An Aspire CLI older than 13.6 is
refused first, with `aspire update --self`.

Maintainer only (destroys dev data): `dev db nuke --yes`. Expect the `aspire stop` output, then the
"created before Aspire 13.6 … `docker volume ls`, then `docker volume rm <name>`" hint. The exit
code equals aspire's. With `ASPIRE_CONTAINER_RUNTIME=podman`, the hint names `podman`.

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

- [ ] `dev db nuke` = preflight + `aspire stop --force --volumes` + manual hint
- [ ] Hash/prefix/sweep/container-cleanup code and tests deleted
- [ ] `postgres-volume-model-tests.cs` triaged
- [ ] Regions + AGENTS.md reconciled
- [ ] Gates: `dev build` 0/0, dev-cli-tests, aspire-tests build, `ganda repo audit`;
      `grep -rn "VolumeNameGenerator\|SHA256" tools/dev-cli` returns nothing

## Notes

- Workers never run `dev db nuke`, `dev run`, or anything that touches the maintainer's AppHost,
  containers or volumes. Verification is by tests and by running `dev db nuke` without `--yes`.
- Steve, 2026-10-07, on the 070-006 version of this pattern: "Why the hell did you need to
  decompile aspire and reconstruct some shit over complicated?"

## Session

- Created: 2026-10-07 (cockpit; found while simplifying 070-006 `dev deprovision`)

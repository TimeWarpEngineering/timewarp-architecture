# Postgres WithVolume env path and dev db nuke over aspire stop --volumes

## Description

Adopt the two Aspire 13.6 items that task 262 had marked skip / later. Steve decided
(2026-10-01) to implement both:

1. **`WithVolume(env:)` path standardization** for the Postgres data volume.
2. **`aspire stop --force --volumes`**, exposed as a deliberate `dev db nuke --yes` verb.

(The third 262 item, `AddDotnetProject()`, belongs to task 267, which updates to .NET 11.)

## Requirements

### A. Postgres volume via `WithVolume(env:)`

1. **Current state.** The AppHost uses `postgres.WithDataVolume()`, gated by
   `Postgres:UseDataVolume` (`aspire-app-host/program.cs` ~L126–141; the test gate uses `false`).
2. **Adopt 13.6 `WithVolume(..., env: …)`** so the data path is standardized the 13.6 way.
   Confirm the exact API in the Aspire 13.6 docs/source (Aspire docs MCP `search_docs` /
   `get_doc`); do not guess the signature.
   - Postgres reads `PGDATA` from its image path, so 262 found little gain. Implement it so the
     env-bound path is correct for Postgres: the volume mounts where `PGDATA` points, and the
     env var is consistent.
   - The **same named volume** must keep being used, so the maintainer's existing data survives
     the change. Verify the volume name and mount path are unchanged, or document the
     migration.
   - Keep the `Postgres:UseDataVolume=false` gate behavior exactly, and keep the template flag
     regions intact.
3. **Design region:** record what `env:` buys here and why the volume identity is preserved.

### B. `dev db nuke --yes`

4. **New verb** in the `dev db` group (`tools/dev-cli/endpoints/db-*.cs`). It wraps
   `aspire stop --force --volumes` for this AppHost (`--apphost <aspire-app-host.csproj>`), which
   stops the AppHost and removes **every** volume Aspire created for it. The next `dev run`
   starts from empty: migrations re-run and seeds re-apply.
5. **Safety:**
   - Require `--yes`. Without it, print what will be destroyed (the volume names, resolved from
     Docker or Aspire before acting) and exit non-zero without acting.
   - Require Aspire CLI ≥ 13.6. Reuse the version guard from task 263 (`AspireRun` service)
     rather than duplicating it.
   - State clearly that it stops the running AppHost.
6. **Distinguish it from `dev db reset`**, which drops and migrates inside the running server and
   keeps the volume. Say this in the verb's description, in `dev --capabilities`, and in the
   AGENTS.md `dev` command line.
7. **Tests** cover the argument building, the `--yes` refusal path and the version guard
   **without** running Aspire or Docker. Reuse the stand-in-`aspire` pattern from 263.
8. **Stale binary:** rebuild `bin/dev` (`dev self-install` / `ganda repo audit --fix --checks bin-dev`)
   and gate on the fresh binary.

## Checklist

- [x] Postgres volume uses 13.6 `WithVolume(env:)`; same volume identity; `UseDataVolume=false`
      gate unchanged; Design region
- [x] AppHost model test (aspire-tests lane or model inspection) asserting the volume and env
      wiring, and that there is no volume when `UseDataVolume=false`
- [x] `dev db nuke --yes` (dry listing without `--yes`, version guard reused, stops AppHost +
      removes volumes)
- [x] Docs: verb description, capabilities, AGENTS.md `dev` lines (nuke vs reset)
- [x] Tests for nuke argument building, refusal and the guard (no Aspire/Docker execution)
- [x] Gates: `dev build` 0/0, `dev test` (including aspire-tests), `dev template-smoke`,
      `ganda repo audit`. Gate on a fresh `bin/dev`
- [x] Do **not** start an AppHost and do **not** run `dev db nuke` for real: it would destroy the
      maintainer's data. Record the manual check as not performed
- [x] Implementation review (disposition: clean); host `open-pr`

## Session

- Created: 22224 (2026-10-01)
- 2026-10-01: implemented (ganda task work implement oracle, Claude Opus 5.5)
- 2026-10-01: implementation review (review oracle, Claude Opus 5.5; effort 2, roster general)

## Notes

- Origin: task 262's evaluate-only recommendations (Notes in 262).
- The maintainer validates after merge:
  1. `dev run` still finds existing data.
  2. `dev db nuke` without `--yes` lists the volumes.
  3. `dev db nuke --yes`, then `dev run`, starts empty and migrations apply.
- Memory discipline: run builds serially; `dotnet build-server shutdown` before finishing.

## Results

### A. Postgres volume via `WithVolume(env:)`

- **API, confirmed and not guessed:** Aspire.Hosting 13.6
  `VolumeResourceBuilderExtensions.WithVolume<T>(builder, name, target, env, isReadOnly)`
  (`where T : IComputeResource, IResourceWithEnvironment`). The aspire.dev page says `env:` is
  for projects and executables only, but the decompiled 13.6 source shows that containers accept
  it too. A container receives `target` in run and publish modes.
- **Why the env var is NOT `PGDATA`:** the default image is postgres **18.3**. `WithDataVolume`
  mounts at `/var/lib/postgresql` for 18+ (docker-library/postgres#1259), and the image's own
  `PGDATA` is `/var/lib/postgresql/18/docker`, which is *inside* that mount. Binding
  `env: "PGDATA"` to the mount would point postgres at the volume root, which re-runs initdb and
  orphans existing data. So `env:` names a dedicated `POSTGRES_DATA_VOLUME` variable (the mount
  path), and `PGDATA` stays with the image.
- **Volume identity preserved:** the name is still `VolumeNameGenerator.Generate(postgres, "data")`
  → `aspire-app-host-{sha256(lower csproj path)[..10]}-postgres-data`, and the target is still
  `/var/lib/postgresql`. Read-only check: the master checkout path hashes to `bbe41f2e49`, which
  matches the existing `aspire-app-host-bbe41f2e49-postgres-data` volume in Docker. No migration
  is needed.
- The `Postgres:UseDataVolume=false` gate and the template flag regions are unchanged. The Design
  regions in `program.cs` and `constants.cs` record what `env:` buys and why the identity holds.
- **Model test:** `tests/container-apps/aspire/aspire-tests/postgres-volume-model-tests.cs` runs
  4 tests that inspect the model only (the AppHost is never built or started):
  - the name and target equal what `WithDataVolume()` produces on a probe resource,
  - `POSTGRES_DATA_VOLUME=/var/lib/postgresql` is set and `PGDATA` is not overridden,
  - `UseDataVolume=false` gives no volume and no variable,
  - the `dev db nuke` prefix equals Aspire's real generated name (`db-nuke.cs` is compile-linked,
    only where it exists).
  `template.json` excludes the file under `!web` and `!postgres`.

### B. `dev db nuke --yes`

- `tools/dev-cli/endpoints/db-nuke-command.cs` plus pure helpers in
  `tools/dev-cli/services/db-nuke.cs`. Steps:
  1. Resolve this checkout's volumes from Docker by AppHost prefix. A sibling worktree's volumes
     are never listed. If Docker is unreachable, the command stops without acting.
  2. Without `--yes`: print the AppHost stop and every volume name, then exit 1.
  3. Version guard.
  4. Run `aspire stop --apphost <csproj> --force --volumes --non-interactive --nologo`.
  5. Sweep with `docker volume rm` whatever volumes still carry the prefix.
- **Why the sweep:** the aspire.dev `aspire stop` docs say `--volumes` removes only volumes that
  Aspire *owns*. A volume that existed before the AppHost started (any pre-13.6 dev volume,
  including the maintainer's) is adopted and left intact. Without the sweep, "next `dev run`
  starts empty" would not hold. The sweep never goes beyond the list printed without `--yes`.
- **Version guard reused, not duplicated:** `AspireRun.ValidateCliVersionForLaunchProfile` became
  the generic `AspireRun.ValidateCliVersion(output, minimum, requirement)`. The `aspire --version`
  probe moved to `services/aspire-cli.cs` (`AspireCli.ValidateVersionAsync`), which `dev run -lp`
  and `dev db nuke` share. The `dev run` messages are unchanged.
- **Docs:** the nuke description and examples, a reset description that now says "keeps the
  volume; use `db nuke` to delete it", the `db` group description and Design region (visible in
  `dev --capabilities`), and an AGENTS.md `dev` line contrasting reset and nuke.
- **Tests:** `tests/tools/dev-cli-tests/db-nuke-tests.cs` covers stop/list/rm argument building,
  the prefix, filtering, the refusal text and the guard. The suite now has 93 tests, all passing.
  The `aspire-run-tests` guard tests were moved to the generic method.
- **Stand-in `aspire` + `docker` shims on PATH** (the 263 pattern; nothing real touched), using
  a fresh `bin/dev`:
  1. no `--yes` → lists only the 2 `a929d5d5ed` volumes and exits 1; only `docker volume ls`
     was called;
  2. `--yes` with CLI 13.5.4 → refused with the update command, exits 1, no stop;
  3. `--yes` with CLI 13.6 → `aspire stop … --force --volumes`, then `docker volume rm` of the
     adopted volume, exits 0. The master checkout's `bbe41f2e49` volume was untouched.

### Gates

- `ganda repo audit --fix --checks bin-dev` rebuilt `bin/dev`, and every gate ran on that fresh
  binary.
- `dev build`: 0 warnings, 0 errors.
- `dev test`: 22 suites passed, including aspire-tests and dev-cli-tests.
- `dev template-smoke`: SUCCEEDED for SmokeDefault, SmokeNoPostgres and SmokeNoApi. The first run
  caught the missing `tools/` tree and a blank-line IDE2000 issue in generated apps; both are
  fixed.
- `ganda repo audit`: passes.
- **Not performed (by design):** no AppHost was started and `dev db nuke` was not run against real
  Aspire or Docker. Both would risk the maintainer's data. The maintainer runs the post-merge
  checks in Notes.

### How to validate

Smoke:
1. `cd tests/container-apps/aspire/aspire-tests && dotnet test -c Release -- --filter-class PostgresVolumeModel`
2. `cd tests/tools/dev-cli-tests && dotnet test -c Release -- --filter-class DbNuke`
   (also `--filter-class CliVersionGuard`)
3. `dev self-install` (or `ganda repo audit --fix --checks bin-dev`), then `dev db nuke`
   **without** `--yes`.
4. After merge, from the master checkout: `dev run` (existing data still present), stop it, then
   `dev db nuke` (lists `aspire-app-host-bbe41f2e49-postgres-data`), then `dev db nuke --yes`,
   then `dev run`.

Expect:
1. 4/4 pass: same volume name and target as `WithDataVolume`, `POSTGRES_DATA_VOLUME` set, no
   volume when `UseDataVolume=false`, and the nuke prefix equals Aspire's name.
2. All pass.
3. Prints "Refusing to nuke without --yes", the AppHost stop line and this checkout's volume names
   (or "None exist right now"), and exits 1 without stopping or removing anything.
4. The first `dev run` keeps existing data. Nuke without `--yes` lists the volume. `--yes` stops
   the AppHost and removes the volume (by the Docker sweep if Aspire treats it as adopted). The
   next `dev run` starts empty: web-migrations applies every migration and seeds re-run.

### Review disposition

- **Rounds:** 1. **Roster:** general. **Effort:** 2 (by-diff budget, 757 lines).
- **Final counts:** bug 0; suggestion 1 fixed; nit 1 fixed; 0 open; 0 wontfix.
- **Disposition:** **clean**.
  - M1: the nuke sweep now removes only the volumes resolved before acting, and the `--yes` path
    prints them before the stop.
  - M2: Sanitize now accepts ASCII digits only, matching Aspire.
- **Paths:** `review/review-framework.md`, `review/round-1/general.md`,
  `review/round-1/merged.md`, `review/disposition.md`.
- **Re-gated after fixes:**
  - dev-cli-tests: 93/93.
  - aspire-tests PostgresVolumeModel: 4/4.
  - `dev build`: 0/0 on a fresh `bin/dev`.
  - `ganda repo audit`: passes.

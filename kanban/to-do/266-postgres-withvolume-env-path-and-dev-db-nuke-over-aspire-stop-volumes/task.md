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

- [ ] Postgres volume uses 13.6 `WithVolume(env:)`; same volume identity; `UseDataVolume=false`
      gate unchanged; Design region
- [ ] AppHost model test (aspire-tests lane or model inspection) asserting the volume and env
      wiring, and that there is no volume when `UseDataVolume=false`
- [ ] `dev db nuke --yes` (dry listing without `--yes`, version guard reused, stops AppHost +
      removes volumes)
- [ ] Docs: verb description, capabilities, AGENTS.md `dev` lines (nuke vs reset)
- [ ] Tests for nuke argument building, refusal and the guard (no Aspire/Docker execution)
- [ ] Gates: `dev build` 0/0, `dev test` (including aspire-tests), `dev template-smoke`,
      `ganda repo audit`. Gate on a fresh `bin/dev`
- [ ] Do **not** start an AppHost and do **not** run `dev db nuke` for real: it would destroy the
      maintainer's data. Record the manual check as not performed
- [ ] Implementation review; host `open-pr`

## Session

- Created: 22224 (2026-10-01)

## Notes

- Origin: task 262's evaluate-only recommendations (Notes in 262).
- The maintainer validates after merge:
  1. `dev run` still finds existing data.
  2. `dev db nuke` without `--yes` lists the volumes.
  3. `dev db nuke --yes`, then `dev run`, starts empty and migrations apply.
- Memory discipline: run builds serially; `dotnet build-server shutdown` before finishing.

## Results

*(fill when done)*

### How to validate

*(required before done)*

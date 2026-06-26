# Migrate remaining .ps1 scripts to dev-cli endpoints (or delete dead ones)

## Why

The goal: **the dev-cli ([[048-create-dev-cli-and-migrate-ps1-scripts-to-nuru-runfiles]]) should
replace ALL `.ps1` scripts.** 048 delivered the CLI + the dev-loop commands (`run`/`test`/`build`/
`clean`), and the Tailwind/npm + Docker + dev-loop wrapper scripts were already deleted. What
remains is the long tail of operational/utility `.ps1` under root `scripts/` (relocated +
kebab-cased from `TimeWarp.Architecture/Scripts/` on 2026-06-26) and `TimeWarp.Templates/`.
The former `TimeWarp.Architecture/DevOps/` `.ps1` were **deleted, not ported** — see below.

Each remaining script should either become a `dev <verb>` Nuru endpoint or be deleted if dead.
Most also carry **stale pre-migration paths** (`Source/ContainerApps/...`, wrapper-root working
dirs) — porting is the moment to fix those, not patch them in place.

## Already done
- Core dev loop in dev-cli: `dev run` / `dev test` / `dev build` / `dev clean` (+ check-version,
  workflow, verify-samples, self-install).
- Deleted: Run/RunTests/Build/RunRelease/Watch.ps1, RunDocker.ps1, docker-timewarp-build.ps1,
  RunTailwind/RunNpmInstall/NpmOutdated.ps1, **cline.ps1** (dead Cline `.clinerules` generator).

## Remaining inventory (~47 files) — port or delete

### DevOps deploy / IaC — DONE (deleted, NOT ported)
- [x] The entire `TimeWarp.Architecture/DevOps/` `.ps1` tree (Bicep provision/deprovision/validate/
      what-if, `Docker/BuildImages.ps1`, Kubernetes `deploy.ps1` + per-resource scripts, top-level
      `provision-build-deploy`/`deprovision`/`rollout-restart-all`/`variables`) was **deleted** under
      [[063-relocate-devops-deploy-infra-to-root]]. The deploy strategy changed: the hand-rolled
      Azure-locked flow is obsolete; deployment is now generated from the Aspire AppHost via
      `aspire publish` (portable compose + k8s) — tracked in [[070-wire-aspire-publish-for-portable-deploy-compose--kubernetes]].
      So these are NOT migration targets for dev-cli.

### Scripts utilities (now root `scripts/`, kebab-cased)
- [ ] Postgres EF → `dev db …`: `scripts/postgres/add-migration.ps1`, `drop-database.ps1`,
      `reset-database-migrations.ps1`, `update-database.ps1`, `ef-shared-variables.ps1`
- [ ] Git stats (likely DELETE or `dev git …`): `scripts/git/count-lines-by-author.ps1`,
      `scripts/git/stats.ps1`, `scripts/git/summarize-git-blame.ps1`
- [ ] `scripts/run-cosmos-db-emulator.ps1` → `dev …` (or drop if cosmos emulator unused)
- [ ] `scripts/build-dependency-diagram.ps1`, `scripts/describe.ps1`,
      `scripts/get-next-task-number.ps1` → port or drop (note: `get-next-task-number` is now
      superseded by the `kanban` CLI; likely DELETE)
- [ ] `scripts/windows/enable-long-paths.ps1`, `scripts/profile.ps1` → evaluate (env setup; may not
      belong in dev-cli). Also `scripts/overview.md` + `scripts/postgres/overview.md` docs.

### TimeWarp.Templates (`TimeWarp.Templates/`)
- [ ] `Build/PublishToGitHubPages.ps1`, `RunDocServer.ps1` → docs publishing (`dev docs …`?)
- [ ] `Source/.../BuildAndInstallTemplate.ps1` → `dev …` template packaging
- [ ] `templates/**/MoveIntoProjects.ps1` (Feature.* codegen helpers) — template-internal
      generated-code movers; decide if these belong in dev-cli at all or stay as template assets.

### Repo sync
- [ ] `.github/scripts/sync-configurable-files.ps1` + the copy under `TimeWarp.Architecture/.github/`
      — ganda-managed repo-sync tooling; likely OUT of scope (not a dev task). Confirm and exclude.

## Notes
- Port pattern is established: see `tools/dev-cli/endpoints/*.cs` (Nuru routes) and
  `runfiles/overview.md` (the ps1 → .cs mapping convention).
- `System.Console` / `System.Diagnostics.ProcessStartInfo` are BannedSymbols at root — endpoints
  use the dev-cli's terminal/shell abstractions (TimeWarp.Amuru), as the existing commands do.
- Fix migrated paths (`source/container-apps/...` kebab) while porting; many scripts still point at
  the old `Source/ContainerApps/...` layout.
- After each port, `dev self-install` and smoke-test the new verb; delete the `.ps1` in the same change.

# Migrate remaining .ps1 scripts to dev-cli endpoints (or delete dead ones)

## Why

The goal: **the dev-cli ([[048-create-dev-cli-and-migrate-ps1-scripts-to-nuru-runfiles]]) should
replace ALL `.ps1` scripts.** 048 delivered the CLI + the dev-loop commands (`run`/`test`/`build`/
`clean`), and the Tailwind/npm + Docker + dev-loop wrapper scripts were already deleted. What
remains is the long tail of operational/utility `.ps1` under `TimeWarp.Architecture/DevOps/`,
`Scripts/`, and `TimeWarp.Templates/`.

Each remaining script should either become a `dev <verb>` Nuru endpoint or be deleted if dead.
Most also carry **stale pre-migration paths** (`Source/ContainerApps/...`, wrapper-root working
dirs) — porting is the moment to fix those, not patch them in place.

## Already done
- Core dev loop in dev-cli: `dev run` / `dev test` / `dev build` / `dev clean` (+ check-version,
  workflow, verify-samples, self-install).
- Deleted: Run/RunTests/Build/RunRelease/Watch.ps1, RunDocker.ps1, docker-timewarp-build.ps1,
  RunTailwind/RunNpmInstall/NpmOutdated.ps1, **cline.ps1** (dead Cline `.clinerules` generator).

## Remaining inventory (~47 files) — port or delete

### DevOps deploy / IaC (`TimeWarp.Architecture/DevOps/`) → likely a `dev deploy …` group
- [ ] Bicep: `provision.ps1`, `deprovision.ps1`, `validate.ps1`, `what-if.ps1`
- [ ] Docker: `Docker/BuildImages.ps1` (builds per-service images for K8s — has stale paths)
- [ ] Kubernetes: `deploy.ps1` + the per-resource manifest scripts under `2_Workloads`,
      `3_Network`, `4_Storage`, `7_Helm_Releases`, `PowerShell/TimeWarp.Charts/*`, `0_Namespaces`
- [ ] Top-level: `provision-build-deploy.ps1`, `deprovision.ps1`, `rollout-restart-all.ps1`,
      `variables.ps1`
- NOTE: this is the deployment story the user chose to KEEP — port carefully, don't delete.

### Scripts utilities (`TimeWarp.Architecture/Scripts/`)
- [ ] Postgres EF → `dev db …`: `Add-Migration`, `Drop-Database`, `Reset-DatabaseMigrations`,
      `Update-Database`, `EfSharedVariables`
- [ ] Git stats (likely DELETE or `dev git …`): `Git/CountLinesByAuthor`, `Git/Stats`,
      `Git/SummarizeGitBlame`
- [ ] `RunCosmosDbEmulator.ps1` → `dev …` (or drop if cosmos emulator unused)
- [ ] `BuildDependencyDiagram.ps1`, `Describe.ps1`, `Get-NextTaskNumber.ps1` → port or drop
- [ ] `Windows/EnableLongPaths.ps1`, `profile.ps1` → evaluate (env setup; may not belong in dev-cli)

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

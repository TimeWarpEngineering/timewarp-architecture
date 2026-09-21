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
      `aspire publish` (portable compose + k8s) — tracked in [[070-wire-aspire-publish-for-portable-deploy-compose-kubernetes]].
      So these are NOT migration targets for dev-cli.

### Scripts utilities (now root `scripts/`, kebab-cased)
- [x] Postgres EF → `dev db …`: `scripts/postgres/add-migration.ps1` → **`dev db add-migration`**.
      `drop-database.ps1` / `update-database.ps1` already covered by `dev db drop` / `dev db update`
      (Aspire `web-migrations`). `reset-database-migrations.ps1` was a squash (delete migrations
      folder + rescaffold) — **deleted, not ported** (147-007 D9: accrete). `ef-shared-variables.ps1`
      folded into `tools/dev-cli/services/db-ef.cs`.
- [x] Git stats — **DELETE**: `stats.ps1` and `summarize-git-blame.ps1` were hardcoded to
      `Cramer/2024-05-08/UserSearchAndDetails`. `count-lines-by-author.ps1` was a generic blame
      walk, not a first-class `dev` verb.
- [x] `scripts/build-dependency-diagram.ps1` — **DELETE** (stale `TimeWarp.Architecture.sln` +
      retired `Documentation/Developer/Reference` output). `scripts/describe.ps1` — **DELETE**
      (Oakton one-liner; hosts still expose `RunOaktonCommands`). `scripts/get-next-task-number.ps1`
      already gone (superseded by `ganda kanban`).
- [x] `scripts/windows/enable-long-paths.ps1`, `scripts/profile.ps1` — **DELETE** (one-time OS /
      PowerShell session setup; direnv + `.envrc` is the repo env). `scripts/overview.md` +
      `scripts/postgres/overview.md` removed with the tree.

### TimeWarp.Templates (`TimeWarp.Templates/`)
- [x] `Build/PublishToGitHubPages.ps1` — **DELETE** (Azure DevOps `##vso` leftover).
      `RunDocServer.ps1` already gone (210-005).
- [x] `Source/.../BuildAndInstallTemplate.ps1` → **`dev template-install`** (packs to
      `artifacts/template-install/`, not the template source folder).
- [x] `templates/**/MoveIntoProjects.ps1` already gone (not a dev-cli target).

### Repo sync
- [x] `.github/scripts/sync-configurable-files.ps1` + the copy under `TimeWarp.Architecture/.github/`
      — **OUT OF SCOPE / already gone** (ganda-managed; archived tasks 019/020).

## Notes
- Port pattern: `tools/dev-cli/endpoints/*.cs` (Nuru routes). `runfiles/overview.md` was retired
  with the documentation tree (210-005).
- `System.Console` / `System.Diagnostics.ProcessStartInfo` are BannedSymbols at root — endpoints
  use ITerminal + TimeWarp.Amuru.
- Scaffolding paths are kebab `source/container-apps/...` (see `DbEf`).
- After each port, `dev self-install` and smoke-test the new verb; delete the `.ps1` in the same change.

## Session

- Implementer: grok (2026-09-21)
- Review: grok 01a0c229-02a2-75d2-a91b-3a1c31d377f6 (2026-09-21), effort 1 general; round-2 re-review after M1 fix

## Results

Zero `.ps1` files remain in the worktree. Live AppHost EF verbs (`dev db update` / `drop` /
`reset` / `status`) already existed; this pass added scaffolding + local template install and
deleted the dead tail.

### Ported
- `dev db add-migration {name}` — design-time `dotnet ef migrations add` (kebab paths, C#
  identifier name, `--dry-run`). Apply remains `dev db update` against a running AppHost.
- `dev template-install` — pack `timewarp-architecture-template` to `artifacts/template-install/`
  and `dotnet new install` into the user's template cache (complements isolated `template-smoke`).

### Deleted (not ported)
| Script | Why |
|--------|-----|
| `scripts/postgres/drop-database.ps1`, `update-database.ps1` | Superseded by Aspire `dev db drop` / `update` |
| `scripts/postgres/reset-database-migrations.ps1` | Squash; 147-007 D9 accretes migrations. `dev db reset` is drop+reapply, not squash |
| `scripts/postgres/ef-shared-variables.ps1` | Constants moved to `DbEf` |
| `scripts/git/stats.ps1`, `summarize-git-blame.ps1` | Hardcoded to a 2024 PR branch |
| `scripts/git/count-lines-by-author.ps1` | One-off blame walk, not a `dev` verb |
| `scripts/build-dependency-diagram.ps1` | Stale `.sln` + retired documentation output path |
| `scripts/describe.ps1` | Oakton one-liner; not the repo CLI |
| `scripts/profile.ps1` | PowerShell session; direnv replaces it |
| `scripts/windows/enable-long-paths.ps1` | One-time OS/Git config, not a repo command |
| `timewarp-templates/build/publish-to-github-pages.ps1` | Azure DevOps leftover |
| `timewarp-templates/.../build-and-install-template.ps1` | Replaced by `dev template-install` |

Already gone before this pass: `get-next-task-number.ps1`, `RunDocServer.ps1`,
`MoveIntoProjects.ps1`, `.github/scripts/sync-configurable-files.ps1`.

### Files changed
- Added: `tools/dev-cli/services/db-ef.cs`, `tools/dev-cli/endpoints/db-add-migration-command.cs`,
  `tools/dev-cli/services/template-nupkg.cs`, `tools/dev-cli/endpoints/template-install-command.cs`,
  `tests/tools/dev-cli-tests/db-ef-tests.cs`, `tests/tools/dev-cli-tests/template-nupkg-tests.cs`
- Updated: `tools/dev-cli/endpoints/db-group.cs`, `skills/tw-aggregate-pattern/SKILL.md`
  (schema evolution prefers `dev db add-migration`), `tests/tools/dev-cli-tests/dev-cli-tests.csproj`
- Removed: entire `scripts/` tree; two template `.ps1` files

### Test outcomes
- `./bin/dev build` — 0 Warning(s), 0 Error(s)
- `cd tests/tools/dev-cli-tests && dotnet test -c Release` — 68 passed (66 + 2 nupkg-selection facts after M1)
- `dotnet run tools/dev-cli/dev.cs -- self-install` — installed `./bin/dev`
- `./bin/dev db add-migration AddOrders --dry-run` — kebab-path `dotnet ef` line, exit 0
- `./bin/dev db add-migration bad-name` — identifier error, exit 1
- `./bin/dev template-install --dry-run` — pack path under `artifacts/template-install/`, exit 0
- `find . -name '*.ps1' -not -path './.git/*'` — 0 files

### Review disposition

- **Rounds:** 2 · **Effort:** 1 · **Roster:** general
- **Counts (final):** bug 0 open / 1 fixed / 0 wontfix; suggestion 0 / 0 / 0; nit 0 / 0 / 0
- **Disposition:** `clean` — M1 (`template-install` leftover nupkg + lexicographic sort) fixed on this task id; round 2 re-verified; no wontfix; no escalation
- **Paths:**
  - `review/review-framework.md`
  - `review/round-1/general.md`
  - `review/round-1/merged.md`
  - `review/round-2/general.md`
  - `review/round-2/merged.md`
  - `review/disposition.md`
- **Wontfix / escalations:** none. Disposition stayed on this task id (no sibling apply-review task).

### How to validate

**Smoke**
```bash
./bin/dev db add-migration AddOrders --dry-run
./bin/dev db add-migration 'Add-Orders'; echo "exit:$?"
./bin/dev template-install --dry-run
find . -name '*.ps1' -not -path './.git/*' | wc -l
```

**Expect**
- Dry-run prints one line: `dotnet ef migrations add AddOrders --project source/container-apps/web/projects/web-infrastructure/web-infrastructure.csproj --startup-project source/container-apps/web/projects/web-server/web-server.csproj --context PostgresDbContext --output-dir ../../platform/postgres/migrations --namespace TimeWarp.Architecture.Persistence.Migrations`
- `Add-Orders` prints the identifier error and exits 1
- `template-install --dry-run` mentions `timewarp-architecture-template.csproj` and `artifacts/template-install`
- `wc -l` is `0`

**Automated gate**
```bash
./bin/dev build
# expect: 0 Warning(s), 0 Error(s)

cd tests/tools/dev-cli-tests && dotnet test -c Release
# expect: 68 passed (or current total, failed 0)
```

**Not in scope:** actually scaffolding a migration (dirties `platform/postgres/migrations/`);
`template-install` without `--dry-run` (mutates the user's `dotnet new` cache).

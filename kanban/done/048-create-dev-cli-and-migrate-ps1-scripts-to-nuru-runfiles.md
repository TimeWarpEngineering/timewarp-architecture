# Create dev-cli and migrate ps1 scripts to Nuru runfiles

## Parent
047_migrate-timewarparchitecture-to-root

## Description

Scaffold dev-cli using `ganda repo enforce-dev-cli --fix` and establish the foundation for migrating away from PowerShell scripts. The dev-cli provides a consistent build/test interface that will work during and after the repository migration.

## Status

**Scaffolded via `ganda repo enforce-dev-cli --fix`** - All compliance checks pass.

## Checklist

### Completed (via enforce-dev-cli)
- [x] Create `/tools/dev-cli/` directory structure
- [x] Create `/tools/dev-cli/dev.cs` - Nuru entry point
- [x] Create `/tools/dev-cli/Directory.Build.props` - AOT config
- [x] Create `/tools/dev-cli/endpoints/` directory
- [x] Scaffold 7 required commands: `build`, `check-version`, `clean`, `self-install`, `test`, `verify-samples`, `workflow`
- [x] Create `/msbuild/repository.props` with path definitions
- [x] Create `/source/Directory.Build.props` with package metadata
- [x] Create root `Directory.Build.props` with BannedApiAnalyzers
- [x] Create `Directory.Packages.props` for centralized versions
- [x] Create `.envrc` with `PATH_add bin`
- [x] Create `BannedSymbols.txt` (bans System.Console)
- [x] Run `dev self-install` to create `./bin/dev`
- [x] Verify all compliance checks pass
- [x] Create `timewarp-architecture.slnx` at repo root via `dotnet new sln`

### Completed (post-scaffold updates)
- [x] Update `build-command.cs` to target `timewarp-architecture.slnx`

### Remaining (during/after migration)
- [x] `test-command.cs` updated from the template stub → runs `dotnet test --configuration Release`. (Making it execute *real* tests = migrating test projects into the root slnx, owned by **058**.)
- [x] Add `run` command (replaces `Run.ps1`) - wraps `aspire run --apphost <apphost>`
- [x] `.ps1` scripts kept working in `TimeWarp.Architecture/` during migration (still present + functional)
- [x] ~~Remove `.ps1` scripts after migration complete~~ → **047** (post-migration; script removal folded into 047 acceptance criteria)

### Dropped (no longer needed)
- ~~Add `watch` command (replaces `Watch.ps1`)~~ - superseded by `aspire run`'s built-in
  watch (`defaultWatchEnabled` feature flag); `dev run` inherits it. No per-invocation
  CLI flag exists, so a separate command would only toggle global config.
- ~~Add `tailwind` command (replaces `RunTailwind.ps1`)~~ - moving away from Tailwind CSS.
- ~~Add `npm-install` command (replaces `RunNpmInstall.ps1`)~~ - no longer needed (dropping
  the npm/Tailwind frontend toolchain).

## Notes

- Dev-cli commands use `Git.FindRoot()` to discover repository root dynamically
- Commands will work correctly once projects are migrated to root structure
- `.ps1` scripts remain in `TimeWarp.Architecture/` as fallback during migration
- `run` wraps the first-party Aspire CLI (`aspire run`) rather than the legacy
  `dotnet run --project` from Run.ps1, matching the move to first-party Aspire. Uses
  Amuru `PassthroughAsync` for interactive dashboard/logs/Ctrl+C. Requires the aspire
  CLI on PATH.
- For hot reload, enable Aspire's built-in watch once:
  `aspire config set features.defaultWatchEnabled true`. `dev run` then restarts on file
  changes, replacing the old `dotnet watch` (Watch.ps1) workflow.
- Tailwind/npm frontend toolchain is being dropped, so the `tailwind` and `npm-install`
  commands (and eventually RunTailwind.ps1 / RunNpmInstall.ps1) are not being migrated.

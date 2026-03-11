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

### Remaining (during/after migration)
- [ ] Update `build-command.cs` to build actual solution (currently generic template)
- [ ] Update `test-command.cs` to run actual tests (currently generic template)
- [ ] Add `run` command (replaces `Run.ps1`)
- [ ] Add `watch` command (replaces `Watch.ps1`)
- [ ] Add `tailwind` command (replaces `RunTailwind.ps1`)
- [ ] Add `npm-install` command (replaces `RunNpmInstall.ps1`)
- [ ] Keep `.ps1` scripts working in `TimeWarp.Architecture/` during migration
- [ ] Remove `.ps1` scripts after migration complete

## Notes

- Dev-cli commands use `Git.FindRoot()` to discover repository root dynamically
- Commands will work correctly once projects are migrated to root structure
- `.ps1` scripts remain in `TimeWarp.Architecture/` as fallback during migration

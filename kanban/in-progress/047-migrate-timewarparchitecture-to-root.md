# Epic: Migrate TimeWarp.Architecture to root

> This is an Epic containing child tasks for migrating the TimeWarp.Architecture template to the root of the repository.

## Description

Migrate the TimeWarp.Architecture template project from its current subdirectory location to the root of the repository. This involves restructuring directories, updating paths, and ensuring all references are updated correctly.

## Child Tasks

- [x] 048: Create dev-cli and migrate ps1 scripts to Nuru runfiles (done)
- [x] 049: Analyze dependency graph to identify leaf projects (done)
- [x] 050: Establish root directory structure (source, tests in kebab-case) — done

## Checklist

- [x] Scaffold dev-cli via `ganda repo enforce-dev-cli --fix`
- [x] Analyze project dependency graph (41 projects, 8 phases)
- [x] Create root directory structure (`source/`, `tests/`, `tools/`, `msbuild/` at root)
- [ ] Migrate projects leaf-to-root — app `source/` migrated; **tests + `GenTester` + `TimeWarp.Automation` still under `TimeWarp.Architecture/`** (owned by 058 + 053-050-019)
- [ ] Verify build succeeds
- [ ] Update documentation

## Notes

- Namespaces remain unchanged (no coupling to folder structure)
- Directory naming: `kebab-case` (e.g., `source/`, `tests/`, `tools/`)
- Migration order: leaf projects first, working up dependency chain

## Acceptance criterion folded in from 059-007 (2026-06-24)

The root `source/` tree (which becomes the template) is already on FluentUI v5 + plain CSS with no
Tailwind. When reworking the template plumbing as part of this migration, ensure the **template ships
clean** — i.e. remove the dead frontend toolchain that still lives under `TimeWarp.Architecture/`:

- [ ] Delete `RunTailwind.ps1`, `RunNpmInstall.ps1`, `NpmOutdated.ps1`.
- [ ] Remove the `RunNpmInstall.ps1` `postAction` from `.template.config/template.json`.
- [ ] Remove the `npm install` block + `alias tailwind='./RunTailwind.ps1'` from `.devcontainer/post-create.sh`.
- [ ] Drop the Tailwind/npm dev-command lines from root `CLAUDE.md` + `TimeWarp.Architecture/Claude.md`.
- [ ] Delete the obsolete `CSS Bundle Hash Management in Blazor with Tailwind.md`.
- [ ] Verify `dotnet new timewarp-architecture -n MyApp` builds/runs with FluentUI v5 and no Tailwind/npm.

(Originally tracked as 059-007, now superseded by this epic.)

## Note (2026-06-24)

TimeWarp.Automation (lib + contracts + test) was **dropped, not migrated** — unused RPA demo superseded by future AI desktop automation; tripped the ProcessStartInfo ban. See archived task 011. One fewer project to migrate leaf-to-root.
- GenTester (empty Hello-World stub referencing the analyzers) also **dropped** (2026-06-24) — unused, superseded by the real SourceGenerator/Analyzers test projects; would have tripped the System.Console ban. With the orphaned `Source/Program.cs` (stale partial AppHost), `Source/Directory.Build.props`, and empty `Source/Overview.md` also deleted, **`TimeWarp.Architecture/Source/` is now fully gone** — the entire source side is migrated-to-root or dropped. Only `TimeWarp.Architecture/Tests/` remains (→ 058 / 053-050-019).

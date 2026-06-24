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
- [x] Migrate projects leaf-to-root — DONE. **0 `.csproj` remain under `TimeWarp.Architecture/`.**
      App `source/` migrated; tests migrated to root `tests/` (058); `GenTester` + `TimeWarp.Automation` dropped.
- [ ] Verify build succeeds (root `timewarp-architecture.slnx` builds; integration/E2E host strategy pending — 058/060)
- [ ] Update documentation
- [ ] Tear down / relocate the remaining `TimeWarp.Architecture/` wrapper (296 non-project files — see status below)

## Status (2026-06-24)

**Code migration: COMPLETE** — no projects left in the wrapper. What remains under
`TimeWarp.Architecture/` is 296 non-`.csproj` files to relocate-to-root or delete:

- `Documentation/` (142), `DevOps/` (75), `Scripts/` (16) — bulk content; relocate to root or archive.
- `.ai/` (13), `.github/` (11, incl. CI workflows), `.devcontainer/` (7), `.vscode/`, `.config/`,
  `.editorconfig`, `.gitignore`, etc. — reconcile vs the root copies (don't duplicate).
- Build plumbing: `Directory.Build.props`/`.targets`, `Directory.Packages.props`, `global.json`,
  `NuGet.config`, old `TimeWarp.Architecture.slnx` (orphaned — points at gone `Source/`) → delete/merge.
- `.template.config/template.json` — **the template definition; still has stale `Source/...` paths.**
  Repointing it to the root kebab layout (so `dotnet new timewarp-architecture` works) is the
  biggest remaining piece.
- Misc one-offs: `ReadMe.md`, `Claude.md`, `Assets/`, `Samples/`, `Spikes/`, `Tools/`, `runfiles/`,
  `msbuild/`, `FixAnalyzerDebug.reg`, `ApiDependencies.dgml`, `Priority-Analysis-Report.md`.

**Done this session:** Tailwind/npm scrub, Tye-era Docker removal, dev-loop `.ps1` deleted +
references repointed to dev-cli (`dev run/test/build`). Remaining `.ps1` → dev-cli tracked in
[[061-migrate-remaining-ps1-scripts-to-dev-cli-endpoints]].

## Notes

- Namespaces remain unchanged (no coupling to folder structure)
- Directory naming: `kebab-case` (e.g., `source/`, `tests/`, `tools/`)
- Migration order: leaf projects first, working up dependency chain

## Acceptance criterion folded in from 059-007 (2026-06-24)

The root `source/` tree (which becomes the template) is already on FluentUI v5 + plain CSS with no
Tailwind. When reworking the template plumbing as part of this migration, ensure the **template ships
clean** — i.e. remove the dead frontend toolchain that still lives under `TimeWarp.Architecture/`:

- [x] Delete `RunTailwind.ps1`, `RunNpmInstall.ps1`, `NpmOutdated.ps1`.
- [x] Remove the `RunNpmInstall.ps1` `postAction` from `.template.config/template.json`.
- [x] Remove the `npm install` block + `alias tailwind='./RunTailwind.ps1'` from `.devcontainer/post-create.sh`.
- [x] Drop the Tailwind/npm dev-command lines from root `CLAUDE.md` + `TimeWarp.Architecture/Claude.md`.
- [x] Delete the obsolete `CSS Bundle Hash Management in Blazor with Tailwind.md`.
- [ ] Verify `dotnet new timewarp-architecture -n MyApp` builds/runs with FluentUI v5 and no Tailwind/npm.
      (blocked on the template-plumbing repoint — `template.json` still has stale `Source/` paths.)

(Originally tracked as 059-007, now superseded by this epic.)

### Tailwind/npm cleanup DONE (2026-06-24)

Beyond the 059-007 list above, also scrubbed the dead toolchain from:
- `.devcontainer/`: removed `npm install -g tailwindcss typescript prettier eslint` from `Dockerfile`,
  the `bradlc.vscode-tailwindcss` extension from `devcontainer.json`, the tailwind/tsc/eslint/prettier
  checks from `validate-container.sh`, and the `tailwind` alias line from `README.md`. (Node/npm kept —
  needed for Claude Code.)
- Deleted the stale how-to `Documentation/Developer/HowToGuides/HowToBuildUIWithFluentUIAndTailwind.md`
  and removed the dangling `CSS Bundle Hash …Tailwind.md` `<File>` entry from the old `TimeWarp.Architecture.slnx`.
- Fixed user-facing template docs: `TimeWarp.Templates/.../Features.md` (`TailwindCss` → FluentUI v5 + plain CSS)
  and `Overview.md` (Tailwind link → FluentUI Blazor).
- Rewrote the stale `source/container-apps/web/web-spa/overview.md` ("How to rebuild Tailwind") to describe
  the real frontend (TS via MSBuild, plain CSS + tokens).
- Remaining "tailwind" hits in tracked files are now only intentional "we removed it" notes.

Still flagged (NOT tailwind — broader DevOps cleanup): `DevOps/Docker/timewarp.software.dockerfile` is a
fully dead build file (references `Source/Server`, .NET 5, a nonexistent `package.json`); delete it as part
of the DevOps/ migration, not here.

## Note (2026-06-24)

TimeWarp.Automation (lib + contracts + test) was **dropped, not migrated** — unused RPA demo superseded by future AI desktop automation; tripped the ProcessStartInfo ban. See archived task 011. One fewer project to migrate leaf-to-root.
- GenTester (empty Hello-World stub referencing the analyzers) also **dropped** (2026-06-24) — unused, superseded by the real SourceGenerator/Analyzers test projects; would have tripped the System.Console ban. With the orphaned `Source/Program.cs` (stale partial AppHost), `Source/Directory.Build.props`, and empty `Source/Overview.md` also deleted, **`TimeWarp.Architecture/Source/` is now fully gone** — the entire source side is migrated-to-root or dropped. Only `TimeWarp.Architecture/Tests/` remains (→ 058 / 053-050-019).

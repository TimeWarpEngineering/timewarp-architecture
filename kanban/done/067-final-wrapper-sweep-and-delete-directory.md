# Final wrapper sweep + delete TimeWarp.Architecture/

Spun out of [[047-migrate-timewarparchitecture-to-root]] (wrapper teardown). **The closer** —
do this LAST, after 062–066 + 061.

## Why

After Documentation (062), DevOps (063), template (064), env config (065), build plumbing (066),
and the remaining `.ps1` (061) are handled, a tail of one-off files and small dirs remains. Triage
them, then delete the now-empty `TimeWarp.Architecture/` directory entirely.

## Scope (relocate to root or delete)

Most of this was swept on 2026-06-26 (the legacy tree had far fewer files than first scoped — no
`Assets/Samples/Spikes/Tools/runfiles/msbuild`). Remaining work is just `.template.config/`.

- [x] `ReadMe.md` — deleted (root `readme.md` is canonical; also kebab-cased).
- [x] `Claude.md` (wrapper) — deleted.
- [x] `Scripts/` — MOVED to root `scripts/` + kebab-cased (incl. both `Overview.md` → `overview.md`)
      under the scripts relocation. `.ps1` → dev-cli conversion still tracked by 061.
- [x] One-offs: `install-dependencies-windows-11.md` (deleted), `UNLICENSE.md` (→ root `unlicense.md`),
      `Priority-Analysis-Report.md` (deleted), `FixAnalyzerDebug.reg` (→ `tools/fix-analyzer-debug.reg`),
      `ApiDependencies.dgml` (deleted). Also deleted: `.ai/`, `.devcontainer/`, `.rooignore`,
      `Directory.Build.props`/`.targets`, `global.json`, `NuGet.config`, `.sln.DotSettings`, `DevOps/`.
- [ ] `.template.config/` (`template.json` + `icon.png`) — the live `dotnet new` manifest. The ONLY
      remaining tracked content. Resolve under template task [[064]] (keep at root vs. repackage from
      root `source/`), then this dir can be removed.
- [ ] When `.template.config/` is resolved and nothing tracked remains, `git rm -r` the
      `TimeWarp.Architecture/` directory and confirm a clean `dev build` at root.

## Notes

- This task is the verification gate: 047 is truly "migrated to root" only once this directory is gone.
- Update root `CLAUDE.md` "Repository Structure" (it still lists `TimeWarp.Architecture/` as a main
  project) once the dir is removed.

## Result (2026-06-26)
`TimeWarp.Architecture/` is fully removed. The final piece — the template's
`.template.config/` and the packaging content-root sourcing from the tree —
was resolved by task 064 (content-root repoint to root `source/`+`tests/`,
verified by generating + building a solution). Directory no longer exists.

# Final wrapper sweep + delete TimeWarp.Architecture/

Spun out of [[047-migrate-timewarparchitecture-to-root]] (wrapper teardown). **The closer** —
do this LAST, after 062–066 + 061.

## Why

After Documentation (062), DevOps (063), template (064), env config (065), build plumbing (066),
and the remaining `.ps1` (061) are handled, a tail of one-off files and small dirs remains. Triage
them, then delete the now-empty `TimeWarp.Architecture/` directory entirely.

## Scope (relocate to root or delete)

- [ ] `ReadMe.md` — merge into / reconcile with the root README.
- [ ] `Claude.md` (wrapper) — fold anything unique into root `CLAUDE.md`, then delete.
- [ ] `Scripts/` leftovers — the `.ps1` go via 061; the two `Overview.md` + emptied dirs removed here.
- [ ] `Assets/`, `Samples/`, `Spikes/`, `Tools/`, `runfiles/`, `msbuild/` — relocate to root or drop.
- [ ] One-offs: `install-dependencies-windows-11.md`, `UNLICENSE.md`, `Priority-Analysis-Report.md`,
      `FixAnalyzerDebug.reg`, `ApiDependencies.dgml` — keep-at-root vs delete.
- [ ] When nothing tracked remains, `git rm -r` the `TimeWarp.Architecture/` directory and confirm a
      clean `dev build` + `dev test` at root.

## Notes

- This task is the verification gate: 047 is truly "migrated to root" only once this directory is gone.
- Update root `CLAUDE.md` "Repository Structure" (it still lists `TimeWarp.Architecture/` as a main
  project) once the dir is removed.

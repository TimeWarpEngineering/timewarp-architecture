# Reconcile / remove wrapper build plumbing

Spun out of [[047-migrate-timewarparchitecture-to-root]] (wrapper teardown).

## Why

The wrapper still has MSBuild/NuGet/solution plumbing that the repo root already provides. With no
projects left under `TimeWarp.Architecture/`, these are orphaned or duplicate and should be merged
into the root equivalents and deleted.

## Scope — deletions were already done; this task performed the RECONCILE review (2026-07-03)

All wrapper build files were deleted during earlier migration commits (`b2333a43` slnx,
`ee85fd40` Directory.Packages.props, `8069c7a2` the rest) without the diff-vs-root review this
task required. That review is now done; **nothing of value was lost**:

- [x] `Directory.Build.props` (93 lines): TFM/ImplicitUsings/LangVersion/warnings-as-errors/
      EmitCompilerGeneratedFiles/package metadata all present at root (or `source/` props).
      Superseded on purpose: `Nullable=disable` → root enables; `Version 0.0.1` → 2.0.0-beta
      train; `Generated/`-folder emission → default `obj/` output. Obsolete: binding redirects,
      `EnablePreviewFeatures`+CA2252, git-timestamp metadata.
- [x] `Directory.Build.targets` (6 lines): only excluded the `Generated/` folder from compile —
      unnecessary with `obj/` emission. Root deliberately has NO targets file; fixed the stale
      CLAUDE.md line that still claimed generated code is "excluded via Directory.Build.targets".
- [x] `Directory.Packages.props`: root CPM authoritative; wrapper copy was orphaned.
- [x] `global.json`: root copy exists. `NuGet.config`: wrapper was nuget.org-only — identical to
      default behavior; root intentionally has none.
- [x] `TimeWarp.Architecture.slnx` + `.sln.DotSettings`: orphaned, deleted.
- [x] Clean `dev build` at root: 0 warnings / 0 errors. `git ls-files` shows **zero** tracked
      files under `TimeWarp.Architecture/` — the wrapper tree is entirely gone.

## Notes

- Likely implication for [[065-reconcile-dev-environment-config-vs-root]]: its scope (wrapper
  `.github`/`.devcontainer`/`.vscode`/dotfiles) also shows zero tracked wrapper files — probably
  overtaken by the same teardown; needs its own (smaller) reconcile pass before closing.

# Reconcile / remove wrapper build plumbing

Spun out of [[047-migrate-timewarparchitecture-to-root]] (wrapper teardown).

## Why

The wrapper still has MSBuild/NuGet/solution plumbing that the repo root already provides. With no
projects left under `TimeWarp.Architecture/`, these are orphaned or duplicate and should be merged
into the root equivalents and deleted.

## Scope

- [ ] `TimeWarp.Architecture/Directory.Build.props` + `Directory.Build.targets` — diff vs the root
      ones; fold any still-relevant settings into root, then delete.
- [ ] `Directory.Packages.props` — the root CPM is authoritative now; confirm no unique pins are
      lost, then delete the wrapper copy.
- [ ] `global.json`, `NuGet.config` — reconcile vs root; delete duplicates.
- [ ] `TimeWarp.Architecture.slnx` + `TimeWarp.Architecture.sln.DotSettings` — orphaned (references
      the gone `Source/` tree and old `Tests/`); root `timewarp-architecture.slnx` is authoritative.
      Delete.

## Notes

- Verify a clean `dev build` at root after each deletion — these files can silently change build
  behavior (warnings-as-errors, analyzer level, package versions).
- The old slnx is already broken (points at deleted `Source/`); low risk to remove.

# Reconcile dev-environment config vs root

Spun out of [[047-migrate-timewarparchitecture-to-root]] (wrapper teardown).

## Why

The wrapper still carries editor / CI / devcontainer / agent config that mostly duplicates (or
should merge into) the repo-root equivalents. These must be reconciled, not blindly moved, so we
don't end up with two competing copies.

## Scope — executed across earlier chore(065) commits; final verification pass done 2026-07-03

- [x] `.github/` — wrapper tree deleted (`ceed7800`). **Active-CI check passed**: everything
      deleted was `.disabled` (claude, claude-code-review, sync-configurable-files) or belonged to
      the dead sync feature (`test-sync.yml`, `sync-config.yml.disabled`, the ps1). Root `.github/`
      carries the three live workflows: `workflow.yml` (canonical CI), `skill-lint.yaml`,
      `timewarp-architecture-documentation.yml`. The configurable-files sync feature is retired on
      both sides (root has no `.github/scripts/` either).
- [x] `.devcontainer/` — wrapper deleted as legacy (`cf7a199a`); **no root devcontainer exists, so
      the repo currently has no devcontainer support at all**. Recorded as the standing decision:
      if wanted later, build fresh for the root layout (new task), don't resurrect the legacy one.
- [x] `.vscode/` — wrapper launch/settings removed (`d44e774c`); root `.vscode/` authoritative.
- [x] `.config/` — `dotnet-tools.json` moved to root + stale tools pruned (`9da28d7d`).
      `.editorconfig` — moved to root + code conformed (`b5deecfa`). `.gitignore` — root
      authoritative (`de8b5f79`). `.gitattributes` — moved + simplified to LF-everywhere
      (`0bd49c5b`). `.mailmap` — merged into root (`c28963fe`). `.rooignore` — deleted with the
      legacy config batch (`8069c7a2`).
- [x] `.ai/` dropped (2026-06-26); `.clinerules` deleted (`0b32cb71`); no `.agent` existed.
- [x] Final verification: `git ls-files` shows **zero** tracked files under
      `TimeWarp.Architecture/` — combined with [[066-reconcile-remove-wrapper-build-plumbing]],
      the wrapper teardown (parent 047) is fully complete.

## Notes

- Devcontainer support is now a deliberate gap, not an oversight — spawn a fresh task if wanted.

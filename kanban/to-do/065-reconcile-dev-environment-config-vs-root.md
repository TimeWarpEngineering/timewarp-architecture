# Reconcile dev-environment config vs root

Spun out of [[047-migrate-timewarparchitecture-to-root]] (wrapper teardown).

## Why

The wrapper still carries editor / CI / devcontainer / agent config that mostly duplicates (or
should merge into) the repo-root equivalents. These must be reconciled, not blindly moved, so we
don't end up with two competing copies.

## Scope (reconcile each vs the root copy — merge, then delete the wrapper copy)

- [ ] `.github/`: CI `workflows/` (several `.disabled` + `test-sync.yml`), `issue_template/`,
      `workflow-templates/`, `sync-config.yml.disabled`. Decide which CI belongs at the real repo
      root `.github/` and drop the rest. (`sync-configurable-files.ps1` here overlaps the root
      `.github/scripts/` copy — dedupe.)
- [ ] `.devcontainer/` (7): already Tailwind/npm-cleaned; decide if the canonical devcontainer
      lives at repo root and remove the wrapper duplicate, or promote this one.
- [ ] `.vscode/` (tasks.json etc.) — merge with root `.vscode/`.
- [ ] `.config/`, `.editorconfig`, `.gitignore`, `.gitattributes`, `.mailmap`, `.rooignore` —
      reconcile vs root; delete duplicates.
- [x] `.ai/` (13) — agent-guidance files for the old structure: DROPPED (deleted 2026-06-26,
      obsolete). `.clinerules` + `.agent` (if present) — still decide keep-at-root vs drop.
      (cline.ps1 that generated `.clinerules` was already deleted).

## Notes

- Root already has its own `.editorconfig`, `.gitignore`, `.github`, `.vscode`, `.config` — the job
  is dedupe/merge, not copy.
- Don't silently drop active CI — confirm what actually runs at the repo root first.

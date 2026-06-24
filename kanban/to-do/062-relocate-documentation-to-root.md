# Relocate Documentation/ to root

Spun out of [[047-migrate-timewarparchitecture-to-root]] (wrapper teardown). Likely the easiest slice.

## Why

`TimeWarp.Architecture/Documentation/` (~142 files) is the bulk of the leftover wrapper content.
Now that all projects live at root, the docs should move to a root `documentation/` (kebab) tree
so the wrapper directory can eventually be deleted.

## Scope

- [ ] Move `TimeWarp.Architecture/Documentation/**` → root `documentation/` (kebab-case dirs).
- [ ] Fix internal cross-links and any code/path references that break after the move
      (many docs still reference the old `Source/ContainerApps/...` layout — update to
      `source/container-apps/...` while here, or note as follow-up).
- [ ] Repoint references TO these docs: root `CLAUDE.md` "Documentation" section points at
      `TimeWarp.Architecture/Documentation/`; the docs site config (TimeWarp.Templates docs build,
      `PublishToGitHubPages.ps1`/`RunDocServer.ps1`) and any `Overview.md` indexes.
- [ ] Decide: keep historical ADRs verbatim (don't rewrite decisions); only fix dead file links.

## Notes

- Online docs: https://timewarpengineering.github.io/timewarp-architecture/ — confirm the docs
  publish pipeline still resolves after the move (coordinate with the template/docs task 064).
- Pure content move — no build impact — hence "easiest first."

# Relocate Documentation/ to root

Spun out of [[047-migrate-timewarparchitecture-to-root]] (wrapper teardown). Likely the easiest slice.

## Why

`TimeWarp.Architecture/Documentation/` (~142 files) is the bulk of the leftover wrapper content.
Now that all projects live at root, the docs should move to a root `documentation/` (kebab) tree
so the wrapper directory can eventually be deleted.

## Scope

- [x] Move `TimeWarp.Architecture/Documentation/**` → root `documentation/` (142 files,
      structure-preserving `git mv` — zero internal-link breakage).
- [x] Repoint references TO these docs: root `CLAUDE.md` "Documentation" section → `documentation/`.
      (docfx builds the published site from `TimeWarp.Templates/Documentation/`, NOT this tree, so
      the site pipeline is unaffected — no docfx change needed.)
- [x] ADRs kept verbatim (historical records; only dead-link cleanup, deferred below).

## Closure (2026-06-24) — DONE (relocation), polish deferred

Relocation complete (commit `264a510c`). All directory names then kebab-cased per repo convention
(commits `3133adf2` c4/user, `8c3f4553` star-uml/generated, `1a423eba` developer subtree) — the
kebab rename introduced zero new broken links and fixed one; the 2 valid cross-dir links were
repointed. `.structurizr` (tool dir) and the self-contained `star-uml/generated/html-docs` site
internals left as-is.

Deferred doc-hygiene (PRE-EXISTING, not caused by this task):
- [ ] ~21 stale intra-doc links (ADR index `Overview.md` files link to same-dir `0000-*.md`
      but those files live in `approved/`/`examples/` subdirs).
- [ ] Some docs still mention the old `Source/ContainerApps/...` layout.

If desired these become a small "documentation hygiene" task; not blocking the wrapper teardown.

## Notes

- Online docs: https://timewarpengineering.github.io/timewarp-architecture/ — built by docfx from
  `TimeWarp.Templates/Documentation/` (separate tree); unaffected by this move.

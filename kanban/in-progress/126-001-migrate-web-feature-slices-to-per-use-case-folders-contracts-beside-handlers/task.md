# Migrate web feature slices to per-use-case folders (contracts beside handlers)

## Description

Maintainer resolution of task 126's RFC Decision 1 (2026-07-26, third-option-wins): inside a
slice, **operation-specific files are grouped by use case**, side by side — the actual VSA
promise — replacing the current shape where contracts sit in `commands/`/`queries/` subfolders
while their handlers sit flat at slice root.

**The rule (unconditional — no slice-size judgment call):**

- Every operation gets a `<use-case>/` folder holding ALL of its layer files together:

  ```text
  admin/roles/
    create-role/
      create-role-contracts.cs
      create-role-handler-application.cs
    get-roles/
      get-roles-contracts.cs
      get-roles-handler-application.cs
    role-details-contracts.cs          # shared shapes stay at slice root
    role-store-application.cs          # shared/multi-operation files stay at slice root
    roles-feature-annotations-server.cs
  ```

- Files serving multiple operations (shared `*-details-contracts.cs`, stores, feature
  annotations, entity type configurations) stay at slice root.
- `commands/` and `queries/` subfolders are removed entirely (grouping by message kind was a
  layer instinct sneaking back into a slice tree).
- 2-file folders are explicitly fine (maintainer call). Unconditional beats "only when big
  enough" because it removes a judgment call agents would drift on.

**Why this is safe mechanically:** the filename grammar, TWA0015/0016, and the membership guard
key on filename only (`%(Filename)`), never folder path, and the layer globs are recursive
(proven: contracts already build fine from `commands/` subfolders today). Filenames stay fully
self-describing; folders are pure human navigation. No registry edit (no rebuild sensitivity),
no analyzer change, no namespace change (namespaces do not track folders — TWA0009 keys off
`…Features.<Id>`).

## Checklist

- [ ] Inventory all ~73 files under `source/container-apps/web/features/` and classify:
      operation-specific (→ use-case folder) vs shared (→ stays at slice root)
- [ ] `git mv` operation files into `<use-case>/` folders; delete emptied `commands/`/`queries/`
      folders (slices: identity, admin/roles, profile, and any others present)
- [ ] Grep `.template.config/template.json` (and any exclude lists / docs) for feature-tree
      paths referencing `commands/`/`queries/` — update if any exist
- [ ] Update `skills/tw-feature-placement/SKILL.md`: whole-slice worked example showing the
      use-case-folder rule + shared-files-at-root rule (present tense, no task-history narration
      — skill is public)
- [ ] Update AGENTS.md Layout section if it references the old shape
- [ ] Verify: `dev build` 0/0, `dev test`, and `dev template-smoke` (feature tree is template
      content — both matrices must stay green)
- [ ] Consider (optional, discuss first): follow-up analyzer/guard enforcing folder placement —
      NOT in scope here; current decision is convention documented in skill, folders unenforced

## Notes

- Parent: 126 (post-dogfood review). RFC + ballot record:
  `kanban/<column>/126-review-feature-cohesive-folders-and-platform-packaging-after-dogfood-use/rfc/rfc.md`
  Decision 1 — reviewers split 2–1 between "document current asymmetry" and "document +
  symmetrize guidance"; maintainer resolved with a third option (this task) that dissolves the
  asymmetry instead of documenting it.
- Evidence for current shape: `../126-.../findings.md` §1 Frictions (admin/roles 5 subfoldered
  contracts vs 8 flat files; identity 14 subfoldered contracts vs 19 flat).
- Pure mechanical move + doc update; no behavior change expected. If anything non-mechanical
  surfaces (e.g. a glob that is not recursive after all), stop and report per design-issue rules.

## Session

- Created: 2026-07-26 — filed from task 126 RFC Decision 1 maintainer resolution.

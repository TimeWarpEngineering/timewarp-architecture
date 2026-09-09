# Review framework — task 210

**Date:** 2026-09-09
**Host task:** kanban/in-progress/210-post-migration-cleanliness-code-review-of-the-architecture-template/
**Diff scope:** whole repository at origin/master `54c07cbc` (post-migration state), not a single diff
**Plan / brief:** the template has absorbed several migrations in sequence (Fixie/xUnit → Jaribu,
Tailwind → FluentUI v5 + CSS, feature-tree axis-1 rehoming, analyzer/foundation packaging,
SPA identity fold 132-001, permission-centric authz 182). Find what those migrations left behind
or left inconsistent, so the template ships clean to generated apps.
**Effort:** 6 specialists (repo-wide review, no general-only pass)
**Reviewer roster:** leftovers · layout-grammar · tests · build-msbuild-template · docs-skills · code-quality
**Session IDs:** claude session_01QYpqCSgnvvLRpXrMKxu5ED

## Baseline gates

- `ganda repo audit`: blocking FAIL — `kebab-path-names` on
  `kanban/done/205-001-reject-invalid-profile-language-bcp-47--culture-name/` (double hyphen);
  `bin-dev` / `dev-cli-capabilities` fail only because the fresh worktree had no `bin/dev`
  (self-install fixes); warnings: memsearch `.githooks` scaffold, `peacock.color` in
  `.vscode/settings.json`.
- `dev build`: see round-1 merged notes.

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-1/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Every finding names a path (and line where possible) and was verified against the tree
- Prior rounds are immutable; new work goes in `round-(N+1)/`

## Dimensions

| Reviewer | Question |
|----------|----------|
| leftovers | What did the migrations leave behind (dead files, stale names/comments, orphan config, empty trees, retired-framework residue)? |
| layout-grammar | Does every file sit where AGENTS.md/tw-feature-placement says, with a grammar-conformant name and honest context regions? |
| tests | Is the test tree single-framework Jaribu, C-create by default, free of duplicated/orphaned suites, and are all suites actually run by `dev test`? |
| build-msbuild-template | Are props/targets/CPM/slnx/template.json/preprocessor regions consistent, minimal, and correct for both monorepo and generated-app modes? |
| docs-skills | Do AGENTS.md, documentation/, and skills/ describe the tree as it is now? |
| code-quality | Suppressions, TODOs, commented-out code, banned patterns, region honesty, obvious duplication in source/. |

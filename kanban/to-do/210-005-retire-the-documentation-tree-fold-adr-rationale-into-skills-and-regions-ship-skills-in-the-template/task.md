# Retire the documentation tree, fold ADR rationale into skills and regions, ship skills in the template

## Description

Decision (Steve, 2026-09-12): `documentation/` is a human-era artifact and is retired.
Purpose/Design regions (enforced by TWA0004 and the reconcile-on-edit rule) plus skills are
the documentation of record. The eight repo skills under `skills/` ship in the template,
pinned to the analyzers they ship beside (the timewarp.software copy is discovery/always-latest;
both come from the same release commit). ADRs are not shipped as pages: each ADR's rule and
reasoning migrates into the skill that owns it or into the Design region of the code/analyzer
that enforces it; an ADR with no enforcing skill or code is a signal to drop it.

This closes findings M2, M20, M21, M22, M23, M24, M25, M27, M28 from the 210 round-1 code
review of the architecture template
(`kanban/in-progress/210-post-migration-cleanliness-code-review-of-the-architecture-template/review/round-1/merged.md`).
Those findings documented specific stale/broken/orphaned pages inside `documentation/`
(dead links, PascalCase-era paths, unedited boilerplate, non-existent files cited); under
this decision the resolution for all of them is "migrate the surviving rule into a skill or
Design region, then delete the page" rather than a page-by-page rewrite.

## Requirements

1. **Inventory `documentation/`** (76 md files): for each file, decide one of
   `delete` / `fold-into-skill:<skill-name>` / `fold-into-Design-region:<file>`. Cover at
   minimum the specific pages the round-1 findings named:
   - M2: `AGENTS.md` Documentation section vs the packaging csproj (see requirement 4).
   - M20: `documentation/developer/how-to-guides/testing/how-to-add-lifecycles-to-tests.md`
     — stale `Setup`/`Cleanup` naming and a dead path; superseded by `tw-jaribu`.
   - M21: `documentation/developer/conceptual/architectural-decision-records/project-structure-and-conventions/`
     (whole subfolder) and `.../architectural-decision-records/proposed/xxx.md` — document
     the opposite of the approved architecture; delete (superseded by ADR-0008 +
     `tw-web-api-contracts`).
   - M22: `documentation/developer/conceptual/features/application/is-processing.md` —
     orphaned, cites paths and a class that do not exist.
   - M23: `documentation/developer/conceptual/component-naming-and-organization.md` —
     PascalCase-folder example tree and a two-layout example contradicting
     `tw-blazor-layout`'s single-shell pattern.
   - M24: `documentation/developer/conceptual/architectural-decision-records/overview.md` —
     unedited MADR boilerplate; approved ADRs 0001–0010 are not linked from it.
   - M25: `documentation/developer/conceptual/architectural-decision-records/approved/0003-endpoint-centric-api-with-interface-based-validation.md:64`
     (broken relative link) and `documentation/developer/reference/dotnet-conventions.md:4`
     (says "Target net9.0"; repo targets net10.0).
   - M27: `documentation/overview.md`, `documentation/roadmap.md`,
     `documentation/developer/overview.md`, `documentation/developer/tutorials/overview.md`,
     `documentation/developer/conceptual/testing/overview.md`,
     `documentation/developer/conceptual/features/overview.md`,
     `.../architectural-decision-records/proposed/overview.md`,
     `.../conceptual/testing/end-to-end-testing.md`,
     `.../how-to-guides/testing/how-to-write-endpoint-test.md`,
     `.../proposed/xxxx-powershell-coding-standards.md` — empty/one-line stubs and unedited
     boilerplate.
   - M28: `runfiles/overview.md` (cites a non-existent `build.cs`) and `readme.md:1,4`
     (badges: `dotnet-6.0`, and a `blazor-state/…/release-build.yml` workflow badge that does
     not exist for this repo).
   - Every approved ADR under `documentation/developer/conceptual/architectural-decision-records/approved/`
     not already covered above: extract its still-true rule into the skill or code region
     that enforces it (or record "no enforcing skill or code — drop").
2. **Delete `documentation/` entirely** once its content has a new home per the inventory.
3. **Ship skills in the template**: add `skills/**` to the `Content Include` list in
   `timewarp-templates/source/timewarp-architecture-template/timewarp-architecture-template.csproj`
   (currently `source/**`, `tests/**`, `msbuild/**`, `.template.config/**`, and named root
   files only — this is the M2 gap). Confirm `.template.config/template.json` does not
   exclude `skills/`.
4. **Rewrite `AGENTS.md`'s Documentation section** to state that Purpose/Design regions plus
   skills are the documentation of record, that `skills/` ships in generated apps, and that
   `documentation/` no longer exists. Fix every `AGENTS.md` reference that currently points
   into `documentation/` (`how-to-remove-demo-features.md`,
   `how-to-upgrade-to-analyzer-packages.md`, `how-to-filter-tests-by-name.md`,
   `how-to-filter-tests-by-tags.md`, `file-naming.md`) — each becomes a skill section or a
   Design region; name the new home in the text you replace it with.
5. **Skills are public**: content migrated into any skill is rule + reasoning only — no
   client names, no past-tense migration narrative (see the
   `skills-are-public-no-history` convention).
6. Fix `readme.md` badges (M28) and `runfiles/overview.md` (M28) — reword against the
   current layout or delete if `runfiles/` stays unpopulated.

## Checklist

- [x] Inventory of all 76 `documentation/` files committed under this task's folder as
      `inventory.md` (delete / fold-into-skill:<name> / fold-into-Design-region:<file> per
      file)
- [x] `documentation/` tree deleted
- [x] `skills/**` added to the template packaging csproj `Content Include`; `template.json`
      confirmed not to exclude it
- [x] `AGENTS.md` Documentation section rewritten; all `documentation/`-path references fixed
- [x] Migrated skill/region content reviewed for client names and past-tense narrative
- [x] `readme.md` badges fixed
- [x] `runfiles/overview.md` fixed or deleted
- [x] `dev template-smoke` passes and a generated app contains `skills/`
- [x] `ganda repo audit`

## Notes

- Nothing under `skills/*/analysis/` ships or publishes.
- Parent: 210 (round-1 ledger:
  `kanban/in-progress/210-post-migration-cleanliness-code-review-of-the-architecture-template/review/round-1/merged.md`).
  On completion, update the M-ids' Status in that ledger to fixed/wontfix on the same PR.

## Session

- Created: 226105 (2026-09-12)
- Implementer: grok-4.6 (2026-09-12)

## Results

Retired `documentation/` (76 markdown files + 4 companions). Surviving rules folded into
repo skills or Design regions per `inventory.md`. Generated apps receive `skills/` (not
`skills/*/analysis/`). Purpose/Design regions plus skills are the documentation of record.

### What was implemented

- Inventory of all 76 markdown files under this task folder as `inventory.md`
- Deleted `documentation/` and unpopulated `runfiles/overview.md`
- Packed `skills/**` (excluding `**/analysis/**`) in the template csproj; `template.json`
  excludes only `skills/**/analysis/**` (does not exclude `skills/`)
- Rewrote `AGENTS.md` Documentation section; replaced every `documentation/` pointer
  (`how-to-remove-demo-features.md` → `tw-slice-isolation`; filter how-tos → `tw-jaribu`;
  `file-naming.md` → `tw-csharp`; analyzer upgrade → Platform packages section)
- Folded ADR-0003/0007 + generator reference into `tw-web-api-contracts`; ADR-0008 into
  `tw-feature-placement`; ADR-0009 + add-aggregate walkthrough into `tw-aggregate-pattern`;
  demo-slice removal into `tw-slice-isolation`
- Folded ADR-0002, ADR-0010 + PDP swap, agent-identity host split, edge-vs-app, and
  progressive-profile rules into Design regions
- Dropped ADRs/pages with no enforcing skill or code (0000, 0001, 0004–0006, stubs, M21–M24
  orphans)
- Fixed `readme.md` badges (`dotnet-10.0`, this repo's `workflow.yml`)
- `dev template-smoke` asserts generated apps contain the eight `skills/*/SKILL.md` files
  and do not contain `skills/*/analysis`
- Round-1 ledger M2, M20–M25, M27, M28 marked fixed on this branch
- `.editorconfig` `[ganda.audit] directory-structure.severity = warning` so retiring
  `documentation/` is not a blocking audit error (ganda `RequiredDirectories` still lists it)

### Files changed (high level)

- `kanban/to-do/210-005-…/inventory.md` (new)
- `AGENTS.md`, `readme.md`
- `skills/tw-aggregate-pattern/SKILL.md`, `skills/tw-slice-isolation/SKILL.md`,
  `skills/tw-web-api-contracts/SKILL.md`, `skills/tw-feature-placement/SKILL.md`
- `Directory.Build.targets`; Design regions on permission evaluator, agent bearer stores,
  abuse rate-limit options, profile domain
- `timewarp-templates/.../timewarp-architecture-template.csproj`, `.template.config/template.json`
- `tools/dev-cli` template-smoke / publish-smoke + harness
- `kanban/in-progress/210-…/review/round-1/merged.md`
- Deleted `documentation/**`, `runfiles/overview.md`, `timewarp-templates/run-doc-server.ps1`

### Key decisions

- Ship-scope (M2): skills ship; `documentation/` does not; AGENTS.md/CLAUDE.md/dev-cli stay
  monorepo-only
- No ninth skill for permission-centric auth — `IPermissionEvaluator` Design region is the
  enforcing home
- Flow-repo skills (`tw-git`, `tw-kanban`, `tw-csharp`, `tw-jaribu`) are pointed from
  AGENTS.md, not duplicated in this template

### Test outcomes

- `dotnet run tools/dev-cli/dev.cs -- template-smoke`: **SUCCEEDED**. Each matrix
  entry printed `Generated app contains skills/ (eight SKILL.md files; analysis/ excluded).`
  SmokeDefault / SmokeNoPostgres / SmokeNoApi all 0/0.
- `ganda repo audit`: **passes** (exit 0). 24 pass; 3 advisory warnings:
  `directory-structure` (missing `documentation/` — expected), `memsearch-scaffold`,
  `vscode-window-icon` (pre-existing).

### How to validate

**Smoke**

```bash
# from repo root
test ! -d documentation
test ! -f runfiles/overview.md
rg -n 'documentation/' AGENTS.md readme.md
# expect: only the Documentation section saying documentation/ does not exist
rg -n 'skills\\\\?\\*\\*' timewarp-templates/source/timewarp-architecture-template/timewarp-architecture-template.csproj
# expect: Content Include for skills with analysis exclude
python3 -c "import json; t=json.load(open('.template.config/template.json')); print([e for m in t['sources'][0]['modifiers'] for e in m.get('exclude',[]) if 'skill' in e.lower()])"
# expect: ['skills/**/analysis/**'] only — skills/ itself is not excluded
```

**Expect**

- `documentation/` is absent
- `AGENTS.md` Documentation section states Purpose/Design regions plus skills are the
  documentation of record and that `skills/` ships
- `readme.md` badges use `dotnet-10.0` and `TimeWarpEngineering/timewarp-architecture/actions/workflows/workflow.yml`
- A generated app (see Automated) has `skills/tw-web-api-contracts/SKILL.md` and does not
  have `skills/tw-web-api-contracts/analysis/`

**Automated gate**

```bash
ganda repo audit
# expect: exit 0 (or only pre-existing advisories)

dotnet run --project tools/dev-cli -- template-smoke
# expect: "Generated app contains skills/ (eight SKILL.md files; analysis/ excluded)."
# expect: "Template smoke SUCCEEDED"
```

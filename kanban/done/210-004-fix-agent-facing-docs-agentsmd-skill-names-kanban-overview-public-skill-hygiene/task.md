# Fix agent-facing docs: AGENTS.md skill names, kanban overview, public skill hygiene

## Description

Docs that agents actually act on, surfaced by the 210 round-1 code review of the
architecture template
(`kanban/in-progress/210-post-migration-cleanliness-code-review-of-the-architecture-template/review/round-1/merged.md`,
findings M18, M19, M26, M29). Highest leverage of the review's findings since agents follow
these files directly.

## Requirements

### M18

- File: `AGENTS.md:25,72,88,110,143,162`
- Six skill references use bare names that do not match the registered skills:
  `dev-cli`→`tw-dev-cli`, `blazor-css-strategy`→`tw-blazor-css-strategy`,
  `feature-placement`→`tw-feature-placement`, `web-api-contracts`→`tw-web-api-contracts`,
  `slice-isolation`→`tw-slice-isolation`, `agent-context-regions`→`tw-agent-context-regions`.
  Agents invoking by the given name fail. The same file uses the `tw-` names correctly
  elsewhere.
- Fix: prefix all six with `tw-`.
- Also fix: the stale diagram annotation at `AGENTS.md:150` ("api platform/ tree absent") —
  `source/container-apps/api/platform/identity-host/` now has 5 files; update the annotation.
- Also fix: mention the fourth dual-mode switch `UseX402Packages` alongside the three
  listed (`UseFoundationPackages` / `UseAnalyzerPackages` / `UseIdentityPackages`) in the
  Platform packages section.

### M19

- Files: `kanban/overview.md` (whole file); `kanban/task-template.md:9`;
  `scripts/get-next-task-number.ps1`
- Describes hand-numbered ids, `B001_…`/`001_…` underscore filenames, and PascalCase folders
  (`Backlog`, `ToDo`, `InProgress`) — directly contradicting AGENTS.md's "never hand-number,
  always `ganda kanban create`" and the actual kebab folders. The companion script exists
  solely to compute hand-assigned numbers.
- Fix: rewrite `kanban/overview.md` to the `ganda kanban` workflow, or reduce it to a
  pointer at AGENTS.md's Task management section plus the `tw-kanban` skill. Fix the
  `task-template.md:9` example (`<Reference to parent item like 001_user-registration>` — no
  hand-numbered examples). Delete `scripts/get-next-task-number.ps1` and remove its
  reference from `scripts/overview.md`.

### M26

- File: `skills/tw-mock-response-factory/SKILL.md:4`
- Frontmatter trigger list includes `MockCopicApiService` — a client name that also does not
  exist in this repo. Skills publish publicly; no client names allowed.
- Fix: remove the token (`MockWebApiService` is already in the list, so no coverage is
  lost).

### M29

- File: `skills/tw-web-api-contracts/analysis/{composer-skill-analysis,glm52-review}.md`
- Contain a client name and past-tense review narrative. `analysis/` is excluded from
  public sync by convention, but nothing in-repo records that exclusion.
- Fix: add a one-line marker in `analysis/` (e.g. a short `readme.md` or a header comment
  in each file) stating the folder is excluded from publication, per the
  `skills-are-public-no-history` convention.

## Checklist

- [x] M18: six bare skill names in `AGENTS.md` prefixed with `tw-`
- [x] M18: `AGENTS.md:150` api platform annotation fixed
- [x] M18: `UseX402Packages` mentioned alongside the other three dual-mode switches
- [x] M19: `kanban/overview.md` rewritten or reduced to a pointer
- [x] M19: `kanban/task-template.md:9` example fixed
- [x] M19: `scripts/get-next-task-number.ps1` deleted; `scripts/overview.md` reference removed
- [x] M26: `MockCopicApiService` removed from `tw-mock-response-factory/SKILL.md`
- [x] M29: exclusion marker added under `skills/tw-web-api-contracts/analysis/`
- [x] `ganda repo audit`
- [x] Implementation review (effort 1, `review/` kitchen)
- [x] Round-1 M1: parent 210 counts table recounted from headings
- [x] Round-2 re-review; disposition **clean**

## Notes

- Parent: 210 (round-1 ledger:
  `kanban/in-progress/210-post-migration-cleanliness-code-review-of-the-architecture-template/review/round-1/merged.md`).
  On completion, update the M-ids' Status in that ledger to fixed/wontfix on the same PR.
- Also removed the `. scripts\get-next-task-number.ps1` source from `scripts/profile.ps1` so
  deleting the script does not leave a broken profile.
- Implementation review kitchen: `review/review-framework.md`, `review/round-1/`,
  `review/round-2/`, `review/disposition.md` (outcome **clean**).

## Session

- Created: 224866 (2026-09-12)
- Implementer: grok session 264983 (2026-09-12)
- Review oracle: grok session 01a095c2-903c-7453-aca1-a480efd3484e (2026-09-12)

## Results

Fixed agent-facing docs from 210 round-1 findings M18, M19, M26, M29. Parent ledger statuses
updated to **fixed** (heading recount: bug 14 open / 4 fixed; suggestion 15 open; nit 7 open /
1 fixed).

### What was implemented

- **M18:** Prefixed six bare skill names in `AGENTS.md` with `tw-` (`tw-dev-cli`,
  `tw-blazor-css-strategy`, `tw-feature-placement`, `tw-web-api-contracts`,
  `tw-slice-isolation`, `tw-agent-context-regions`). Updated the api layout annotation from
  `platform/ tree absent` to `platform/identity-host/ (agent token auth)`. Listed
  `UseX402Packages` with the other three dual-mode switches.
- **M19:** Rewrote `kanban/overview.md` to point at AGENTS.md Task management + `tw-kanban`
  (kebab columns, `ganda kanban create`, no hand-numbering). Kept product DoR/DoD checklists.
  Changed `kanban/task-template.md` parent example to `<Parent task id, e.g. 210>`. Deleted
  `scripts/get-next-task-number.ps1`; removed its overview entry and `profile.ps1` source.
- **M26:** Removed `MockCopicApiService` from `skills/tw-mock-response-factory/SKILL.md`
  `when-to-use` (`MockWebApiService` remains).
- **M29:** Added `skills/tw-web-api-contracts/analysis/readme.md` stating the folder is
  excluded from public skill publication (`skills-are-public-no-history`).

### Files changed

- `AGENTS.md`
- `kanban/overview.md`
- `kanban/task-template.md`
- `scripts/get-next-task-number.ps1` (deleted)
- `scripts/overview.md`
- `scripts/profile.ps1`
- `skills/tw-mock-response-factory/SKILL.md`
- `skills/tw-web-api-contracts/analysis/readme.md` (added)
- `kanban/in-progress/210-post-migration-cleanliness-code-review-of-the-architecture-template/review/round-1/merged.md` (M18, M19, M26, M29 Status → fixed; counts recounted from headings)
- `kanban/in-progress/210-004-…/task.md` (this file)
- `kanban/in-progress/210-004-…/review/` (framework, round-1, round-2, disposition)

### Key decisions / deviations

- `kanban/overview.md` is a pointer plus the existing API/client DoR/DoD checklists (those
  are product-specific and not encoded in AGENTS.md).
- Extra `profile.ps1` source removal: required so the deleted script is not still dotted-in.
- Parent 210 counts table was updated by heading recount (not a delta from the previous
  table, which already lagged headings).

### Review disposition

- **Outcome:** `clean` (0 open; no wontfix)
- **Effort / roster:** 1 — `general` only
- **Rounds:** 2
- **Final counts:** bug 0 open / 1 fixed; suggestion 0; nit 0
- Round-1 merge raised **M1** (parent 210 counts table vs headings). Fixed on this id;
  round-2 re-verified **fixed**, no new findings.
- Paths: `review/review-framework.md`, `review/round-1/general.md`,
  `review/round-1/merged.md`, `review/round-2/general.md`, `review/round-2/merged.md`,
  `review/disposition.md`

### Test outcomes

- `ganda repo audit`: **passes** (25 pass; 2 pre-existing advisory warnings:
  `memsearch-scaffold` missing `.githooks/pre-commit`/`pre-push`, `vscode-window-icon`
  missing `peacock.color`). Docs-only change — no `dev build` / `dev test` surface.
- Smoke greps: no remaining bare skill names in `AGENTS.md`; no PascalCase kanban folders
  or `get-next-task-number` in overview/template/scripts; `MockCopicApiService` gone from
  the mock-factory skill.

### How to validate

**Smoke**

```bash
rg -n 'dev-cli|blazor-css-strategy|feature-placement|web-api-contracts|slice-isolation|agent-context-regions' AGENTS.md | rg -v 'tw-'
rg -n 'platform/ tree absent|UseX402Packages' AGENTS.md
rg -n 'Backlog|ToDo|InProgress|B001_|001_user-registration|get-next-task-number' kanban/overview.md kanban/task-template.md scripts/overview.md scripts/profile.ps1
test ! -f scripts/get-next-task-number.ps1 && echo 'script gone'
rg -n 'MockCopicApiService' skills/tw-mock-response-factory/SKILL.md
cat skills/tw-web-api-contracts/analysis/readme.md
rg -n '^\| (bug|suggestion|nit) \|' kanban/in-progress/210-post-migration-cleanliness-code-review-of-the-architecture-template/review/round-1/merged.md
rg -n 'Outcome' kanban/in-progress/210-004-fix-agent-facing-docs-agentsmd-skill-names-kanban-overview-public-skill-hygiene/review/disposition.md
ganda repo audit
```

**Expect**

- First `rg` prints nothing (every remaining skill mention in AGENTS.md is already `tw-` prefixed).
- AGENTS.md api diagram mentions `platform/identity-host/`; Platform packages lists
  `UseX402Packages`.
- Kanban overview/template and scripts overview/profile have no PascalCase columns, no
  `B001_`/`001_user-registration` examples, and no `get-next-task-number`.
- `scripts/get-next-task-number.ps1` does not exist.
- `MockCopicApiService` is absent from the mock-factory skill.
- Analysis `readme.md` states exclusion from publication (`skills-are-public-no-history`).
- Parent 210 counts table is `| bug | 14 | 4 | 0 |`, `| suggestion | 15 | 0 | 0 |`,
  `| nit | 7 | 1 | 0 |`. Disposition file says **Outcome:** clean.
- `ganda repo audit` **passes** (25 pass). Two pre-existing advisory warnings remain:
  `memsearch-scaffold` (`.githooks/pre-commit`/`pre-push`) and `vscode-window-icon`
  (`peacock.color`). Fresh worktrees need
  `dotnet run tools/dev-cli/dev.cs -- self-install` first so `bin/dev` exists.

**Automated gate**

None. Docs/skills/kanban only.

**Not in scope:** live skill sync to a public catalog; `dev build` / `dev test`.

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

- [ ] M18: six bare skill names in `AGENTS.md` prefixed with `tw-`
- [ ] M18: `AGENTS.md:150` api platform annotation fixed
- [ ] M18: `UseX402Packages` mentioned alongside the other three dual-mode switches
- [ ] M19: `kanban/overview.md` rewritten or reduced to a pointer
- [ ] M19: `kanban/task-template.md:9` example fixed
- [ ] M19: `scripts/get-next-task-number.ps1` deleted; `scripts/overview.md` reference removed
- [ ] M26: `MockCopicApiService` removed from `tw-mock-response-factory/SKILL.md`
- [ ] M29: exclusion marker added under `skills/tw-web-api-contracts/analysis/`
- [ ] `ganda repo audit`

## Notes

- Parent: 210 (round-1 ledger:
  `kanban/in-progress/210-post-migration-cleanliness-code-review-of-the-architecture-template/review/round-1/merged.md`).
  On completion, update the M-ids' Status in that ledger to fixed/wontfix on the same PR.

## Session

- Created: 224866 (2026-09-12)

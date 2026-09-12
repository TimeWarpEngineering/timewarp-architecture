# Review framework — task 210-004

**Date:** 2026-09-12
**Host task:** kanban/in-progress/210-004-fix-agent-facing-docs-agentsmd-skill-names-kanban-overview-public-skill-hygiene/
**Diff scope:** branch `task/210-004-fix-agent-facing-docs-agentsmd-skill-names-kanban` vs `origin/master` (commits `b3e13952`, `64ec2053`)
**Plan / brief:** Fold 210 round-1 findings M18, M19, M26, M29 into agent-facing docs: prefix six bare skill names in `AGENTS.md`, refresh api `platform/` annotation + `UseX402Packages`, rewrite `kanban/overview.md` to `ganda kanban`, delete `scripts/get-next-task-number.ps1`, strip `MockCopicApiService` from public skill frontmatter, mark `skills/tw-web-api-contracts/analysis/` excluded from publication. Update parent 210 ledger statuses.
**Effort:** 1 (general only)
**Reviewer roster:** general
**Session IDs:** Review oracle grok session 01a095c2-903c-7453-aca1-a480efd3484e (2026-09-12)

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`

## Key paths

- `AGENTS.md`
- `kanban/overview.md`
- `kanban/task-template.md`
- `scripts/get-next-task-number.ps1` (deleted)
- `scripts/overview.md`
- `scripts/profile.ps1`
- `skills/tw-mock-response-factory/SKILL.md`
- `skills/tw-web-api-contracts/analysis/readme.md`
- `kanban/in-progress/210-post-migration-cleanliness-code-review-of-the-architecture-template/review/round-1/merged.md`

## Round 2

Re-review after fixing round-1 M1 (parent 210 counts table recounted from headings; 210-004 Results text updated). Prior `round-1/` is frozen. Carry M1 with updated status; scan the fix delta for new defects.

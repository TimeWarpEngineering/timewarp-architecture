# Post-migration cleanliness code review of the architecture template

## Description

This repo IS the `dotnet new timewarp-architecture` template, so every leftover from the recent
migrations (Fixie/xUnit → Jaribu, Tailwind → FluentUI v5 + plain CSS, axis-1 feature-tree
rehoming, analyzer packaging, SPA identity fold, permission-centric authorization) ships to every
generated app. This task is a repo-wide, review-only pass that produces a severity-tagged,
merged findings ledger under `review/` so the fix work can be dispatched as child tasks or
folded into this one.

Scope is the whole tree at the task branch base (origin/master `54c07cbc`), not a single diff.

## Requirements

- Review artifacts live under `review/` per `tw-implementation-review` (framework, per-reviewer
  round files, merged ledger, disposition).
- Findings are falsifiable: every issue names a path and, where possible, a line.
- Zero invented findings; "clean" is a valid per-dimension outcome.
- Baseline gates recorded: `ganda repo audit`, `dev build`.

## Checklist

- [x] Create task via `ganda kanban create`, move to in-progress
- [x] Baseline: `ganda repo audit` (kebab fail on `205-001` done folder; bin/dev + memsearch/peacock warnings)
- [ ] Baseline: `dev build` 0/0
- [ ] Write `review/review-framework.md`
- [ ] Round 1: specialist reviewers write `review/round-1/<reviewer>.md`
- [ ] Merge into `review/round-1/merged.md` with stable M# ids
- [ ] Record Results + fix-dispatch recommendation on this task
- [ ] Disposition (`review/disposition.md`) once fix loop / wontfix decisions are made

## Session

- Created: 2418574 (2026-09-09)
- Review round 1: https://claude.ai/code/session_01QYpqCSgnvvLRpXrMKxu5ED (2026-09-09)

## Notes

Review-only task: no product code changes land on this branch. Fixes are dispatched from the
merged ledger (child tasks under 210 for anything non-trivial).

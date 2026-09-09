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
- [x] Baseline: `dev build` 0/0; `dev test` pass; `dev template-smoke` pass; `dev check-version` safe (beta.17 vs beta.16)
- [x] Write `review/review-framework.md`
- [x] Round 1: six specialist reviewers wrote `review/round-1/<reviewer>.md`
- [x] Merge into `review/round-1/merged.md` with stable M# ids (M1–M41)
- [x] Record Results + fix-dispatch recommendation on this task
- [ ] Human decision: ship-scope of `documentation/` (M2) and features-vs-platform reclassification (M8)
- [ ] Dispatch fix bundles as child tasks 210-00N (see merged.md table)
- [ ] Disposition (`review/disposition.md`) once fix loop / wontfix decisions are made

## Session

- Created: 2418574 (2026-09-09)
- Review round 1: https://claude.ai/code/session_01QYpqCSgnvvLRpXrMKxu5ED (2026-09-09)

## Results (round 1)

- Rounds run: 1. Roster: leftovers, layout-grammar, tests, build-msbuild-template, docs-skills,
  code-quality (effort 6, one general-purpose subagent each) + orchestrator spot-verification.
- Gates: `dev build` 0/0, `dev test` pass, `dev template-smoke` pass, `dev check-version` safe;
  `ganda repo audit` blocking FAIL on one kebab path (M1).
- Final counts (open / fixed / wontfix): bug 15 / 0 / 0 · suggestion 17 / 0 / 0 · nit 9 / 0 / 0.
- Migrations verified clean: no Fixie/xUnit/FluentAssertions/Tailwind/npm residue; no
  `Features.Authentication`/`Features.Account` remnants; all runfiles aggregated; CPM, template.json,
  preprocessor regions, diagnostic-ID tables and product Design regions all consistent.
- Dominant themes: stale `documentation/` and `kanban/overview.md` (pre-migration content, and a
  ship-scope contradiction in AGENTS.md — M2), namespace discipline at the features/platform
  boundary (M3–M8), missing endpoint tests (M12, M13), unjustified suppressions (M32, M33).
- Disposition: pending fix loop (no `review/disposition.md` yet).
- Paths: `review/review-framework.md`, `review/round-1/merged.md` (live ledger),
  `review/round-1/{leftovers,layout-grammar,tests,build-msbuild-template,docs-skills,code-quality}.md`.

## Notes

Review-only task: no product code changes land on this branch. Fixes are dispatched from the
merged ledger (child tasks under 210 for anything non-trivial).

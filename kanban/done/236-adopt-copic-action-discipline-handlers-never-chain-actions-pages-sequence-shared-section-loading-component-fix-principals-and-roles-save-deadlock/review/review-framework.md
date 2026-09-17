# Review framework — task 236

**Date:** 2026-09-17
**Host task:** kanban/in-progress/236-adopt-copic-action-discipline-handlers-never-chain-actions-pages-sequence-shared-section-loading-component-fix-principals-and-roles-save-deadlock/
**Diff scope:** branch `task/236-adopt-copic-action-discipline-handlers-never-chain` vs merge-base `origin/master` (`6ff6d040`). Product commit `9cfec250` plus round-1 fixes (M1–M3, uncommitted at round-2 start). Results commit `403b8449`.
**Plan / brief:** See `task.md` Requirements. Deadlock: `ApiHandler` per-state semaphore held through `HandleSuccess`; Principals/Roles (and credentials) nested Fetch. Fix: pages sequence Set then Fetch; semaphore covers GetRequest → validate → GetResponse only; shared `Section` replaces hand-written Loading…; source-scan guards + runtime nested-dispatch probe. Locked rules: handlers never dispatch another action; loading UI is Section or FluentDataGrid Loading.
**Effort:** 1 (general only)
**Reviewer roster:** general
**Session IDs:** Review oracle: grok session 01a0ad15-e50b-7c71-9c36-3dbbc391b5fc (2026-09-17). General reviewer (round 1): grok session 01a0ad17-e5d4-7412-97cd-3f27436942a1 (2026-09-17). General reviewer (round 2): grok session 01a0ad22-9a1e-7463-8744-4c3110d1564e (2026-09-17).

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`

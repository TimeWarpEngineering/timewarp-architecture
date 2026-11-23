# Review framework

## Budget (by-diff)

- Lines changed: 1698
- Effort: 3
- TCB hits: none
- Roster axes: general
- Turn cap: 200 (--max-turns; cursor uncapped)

## Round 1 scope

**Date:** 2026-11-23
**Host task:** kanban/in-progress/489-003-role-edit-form-and-move-role-permission-drafts-out-of-the-store/
**Diff scope:** branch `task/489-003-role-edit-form-and-move-role-permission-drafts-out`, implementation
commit `efb40c8e1` against the merge of `origin/feature/edit-forms-whole-object` (`927fe88f4`).
24 files under `source/` and `tests/`.
**Plan / brief:** task.md `## Goal (implementer restatement)` and `## Description`.
**Effort:** 3 (by-diff budget). Roster axis: general only.
**Reviewer roster:** general (Claude Opus 5.5, review oracle session)

## Round 2 scope

**Date:** 2026-11-23
**Diff scope:** fix commit `3df4d6fab` against `efb40c8e1`, 7 files under `source/` and `tests/`.
**Effort:** 2 (by-diff budget, 482 lines). Roster axis: general only.
**Reviewer roster:** general (Claude Opus 5.5, review oracle session)

## Round 3 scope

**Date:** 2026-11-23
**Diff scope:** `659820c8a..a454ad73a`: the merge of `origin/feature/edit-forms-whole-object`
(`1807d3a06`, no conflict hunks, 489-004 and 489-005 content already reviewed on PRs #660 and #661)
and `0fde1227a` (one line in `action-catalog-tests.cs`). The by-diff count (1698) is mostly the
merged feature-branch content.
**Effort:** 3 (by-diff budget). Roster axis: general only.
**Reviewer roster:** general (Claude Opus 5.5, review oracle session)

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims by reading
  the diff and the kitchen `## Results` and `### How to validate` record
- Do not re-run a full build, a full test run, an audit, or the hooks; those belong to
  the implementer's recorded results, the host audit node, and CI
- Exception: one targeted command may check one suspected bug; the finding names the
  command and the reason. A full build, full test run, or audit is never that command
- A missing or thin recorded result is a finding, not a reason to run the gates
- Prior rounds are immutable; new work goes in `round-(N+1)/`

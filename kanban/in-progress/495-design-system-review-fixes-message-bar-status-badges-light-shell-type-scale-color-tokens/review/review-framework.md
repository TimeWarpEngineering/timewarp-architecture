# Review framework

## Budget (by-diff)

- Lines changed: 747
- Effort: 2
- TCB hits: none
- Roster axes: general
- Turn cap: 120 (--max-turns; cursor uncapped)

# Review framework — task 495

**Date:** 2026-11-24
**Host task:** kanban/in-progress/495-design-system-review-fixes-message-bar-status-badges-light-shell-type-scale-color-tokens/
**Diff scope:** branch `task/495-design-system-review-fixes-message-bar-status` vs `master` (merge base), product paths under `source/` and `tests/`, plus the kitchen `task.md` and `assets/`
**Plan / brief:** task.md items 1–7 (message bar tints and inline actions, drop Lost pill, light shell Member label and nav rest fill, type-scale roles and one eyebrow, color-token chips with resolved values, scroll reset on part change, parts list fits viewport)
**Effort:** 2 (by-diff budget), roster axis general
**Reviewer roster:** general
**Session IDs:** review oracle, Claude Code (claude-opus-5-5), ganda task-work review node

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

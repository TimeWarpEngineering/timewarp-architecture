# Review framework

## Budget (by-diff)

- Lines changed: 519
- Effort: 2
- TCB hits: none
- Roster axes: general
- Turn cap: 120 (--max-turns; cursor uncapped)

# Review framework — task 476

**Date:** 2026-11-05
**Host task:** kanban/in-progress/476-spike-generate-application-layer-glue-projection-and-mutation-call-from-contracts-and-domain/
**Diff scope:** branch `task/476-spike-generate-application-layer-glue-projection-a` vs merge-base with `master`
(proposal.md, task.md, `skills/tw-aggregate-pattern/SKILL.md` +21 lines; follow-ups 477–479 verified on master)
**Plan / brief:** spike — proposal only; Steve chose option A (analyzer alone); decision folded into
`tw-aggregate-pattern`; follow-ups 477, 478, 479 filed.
**Effort:** 2 (by-diff budget above)
**Reviewer roster:** general
**Session IDs:** review oracle (claude, claude-opus-5-5), ganda task-work review node, round 1

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`

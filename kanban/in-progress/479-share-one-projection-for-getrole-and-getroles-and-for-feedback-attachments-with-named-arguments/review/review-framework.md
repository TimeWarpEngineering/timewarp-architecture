# Review framework

## Budget (by-diff)

- Lines changed: 445
- Effort: 2
- TCB hits: none
- Roster axes: general
- Turn cap: 120 (--max-turns; cursor uncapped)

# Review framework — task 479

**Date:** 2026-11-05
**Host task:** kanban/in-progress/479-share-one-projection-for-getrole-and-getroles-and-for-feedback-attachments-with-named-arguments/
**Diff scope:** branch `task/479-share-one-projection-for-getrole-and-getroles-and`, commit faf68cf1b (product change) vs its parent 0f99f4819 (master after task 478 merged)
**Plan / brief:** task.md Requirements and `## Goal (implementer restatement)`: one Roles projection for GetRole/GetRoles, one attachment projection for AttachmentsAsync and the upload handler, named arguments at every response/DTO/admin-view/activity/attachment constructor call in the Roles and Feedback application layers, no behavior change.
**Effort:** 2 (by-diff budget, roster axis general)
**Reviewer roster:** general (review oracle, claude-opus-5-5)
**Session IDs:** review oracle, ganda task work (headless print mode)

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`

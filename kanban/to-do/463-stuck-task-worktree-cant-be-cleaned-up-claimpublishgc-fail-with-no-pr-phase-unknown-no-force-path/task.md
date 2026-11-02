# Stuck task worktree can't be cleaned up: claim/publish/gc fail with 'no PR' / 'phase unknown', no force path

## Description

Bug in Ganda (filed here in timewarp-architecture at Steven's request). An interrupted first claim
leaves a task stuck: claim, publish, and `worktree gc` all refuse with "no PR" / "phase unknown".
`release --dead` drops the claim but leaves the worktree and local branch. No supported path collects them.


## Reproduction (observed)

- Repo TimeWarpEngineering/timewarp-taratibu, task ID 062. The first claim was interrupted partway,
  which burned the ID.
- `ganda kanban release 062 --dead` released the claim.
- Still present:
  - worktree `/home/box/worktrees/github.com/TimeWarpEngineering/timewarp-taratibu/task-062-ramani-visualization-emit-mermaid-state-diagrams-a`
  - local branch `task/062-…` at `1fc6ad7`
- Reproduced on ganda `1.0.0-beta.38` builds `6c32f55` and later (current `66e72a9`).
- Do NOT remove the 062 worktree by hand. It is the live repro.

## Expected

- A supported way to collect a dead task worktree and its local branch, e.g. `ganda worktree gc --task-id <id> --force` or
  `ganda kanban abandon <id>`.
- That path should handle a task with no PR and an unknown phase.
- Optionally, recovery or resume for an interrupted first claim, so the ID isn't burned.

## Checklist

- [ ] Reproduce with 062 (read-only diagnosis first)
- [ ] Hand to the Ganda repo bot / file there if Steven wants it mirrored
- [ ] Design force/abandon path
- [ ] Collect 062 through that path

## Session

- Created: 2942758 (2026-11-02)

## Notes

- Same drift class as the worktree-lifecycle sketch (column / claim ref / folder / branch / PR state
  out of sync).
- Second live repro in this repo: the first attempt to file this task, ID 462, was interrupted during its first claim. Its kitchen commit c66e5112c and fill commit 911d8436e sit on local branch task/462-… in worktree task-462-stuck-task-worktree-cant-be-cleaned-up-claimpublis, but nothing landed on origin-home. Publish 462 then refused with "phase is unknown … origin-home has no column kitchen for this id", and claim 462 refused with "not reserved and has no kitchen on this board". The ID is burned. That worktree was left in place.

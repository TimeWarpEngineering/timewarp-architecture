# Review framework — task 217

**Date:** 2026-09-14
**Host task:** kanban/in-progress/217-retire-the-event-stream-demo-and-archive-task-198/
**Diff scope:** branch `task/217-retire-the-event-stream-demo-and-archive-task-198` vs `origin/master` (commit `21ef1e90`). Working tree clean. One product commit: retire event-stream demo, move `[TrackEvent]` to Features substrate, archive task 198.
**Plan / brief:** `kanban/in-progress/217-retire-the-event-stream-demo-and-archive-task-198/task.md` — delete event-stream inventory; update AGENTS.md and tw-slice-isolation demo list; archive 198; move `[TrackEvent]` to substrate namespace and drop counter's `[CrossSliceReference]`.
**Effort:** 1 (general only)
**Reviewer roster:** general
**Session IDs:** grok review oracle 01a09f53-ca92-7db3-a28b-f0d2aa402747 (2026-09-14)

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`

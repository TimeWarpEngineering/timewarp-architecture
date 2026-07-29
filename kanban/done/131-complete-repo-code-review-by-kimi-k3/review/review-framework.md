# Review framework — task 131

**Date:** 2026-07-28
**Host task:** kanban/in-progress/131-complete-repo-code-review-by-kimi-k3/
**Diff scope:** full repo at review commit `2b5dc765` (per review-brief.md — not a PR diff)
**Plan / brief:** `../review-brief.md` (Kimi K3 whole-repo maintainability audit, /code-review bar)
**Effort:** 3 reviewers, sequential primary + parallel independent verification
**Reviewer roster:**
- `kimi-k3` — primary reviewer; findings at `../findings.md` (F-001…F-017, task-root
  location per task 131's own Deliverables spec — grandfathered, not moved)
- `claude-verification` — independent verification pass over each Kimi finding
  (`round-1/claude-verification.md`): re-verify against the tree; confirm, narrow,
  expand, or reject; recommend disposition. Read-only on product code.
- `grok-verification` — second independent verification pass, same contract, writing
  `round-1/grok-verification.md`. **Blind rule:** must NOT read
  `claude-verification.md` until its own 17 verdicts are written (independence beats
  agreement); may then append a short delta section comparing conclusions.

**Session IDs:** Kimi K3 opencode orchestrator session (see task.md); Claude Fable 5
verification session 2026-07-28.

## Ground rules

- Reviewers are read-only on product code; they write only under `review/`
- Verification verdicts per finding: `confirmed` | `confirmed-expanded` |
  `confirmed-narrowed` | `rejected` — each with evidence, not deference
- Zero deference: primary reviewer is competent but not infallible; every falsifiable
  claim is re-checked against the repo before a verdict
- Steward (human) dispositions per finding go to `../disposition.md` (task 131
  deliverable #3), informed by both reviewer files
- Prior artifacts are immutable; verification never edits `../findings.md`

# Review framework

## Budget (by-diff)

- Lines changed: 2157
- Effort: 3
- TCB hits: none
- Roster axes: general
- Turn cap: 200 (--max-turns; cursor uncapped)

# Review framework — task 346

**Date:** 2026-10-13
**Host task:** kanban/to-do/346-ask-tag-pick-an-element-on-the-page-for-the-ai-to-consider/
**Diff scope:** commit 7605d2da9 (`git diff 7605d2da9~1 7605d2da9`) on branch task/346-ask-tag-pick-an-element-on-the-page-for-the-ai-to
**Plan / brief:** task.md Behaviour + Decisions — Ask panel Tag enters pick mode over `data-ask-tag`-marked elements; chosen element becomes a removable chip; semantic payload (incl. 345 `@prefix:id`) appended to the next message via `AskTagChatClient`.
**Effort:** 3 (by-diff budget)
**Reviewer roster:** general (deep pass: correctness, privacy rules, tests, conventions)
**Session IDs:** review oracle (claude, claude-opus-5-5), session id not reported

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`

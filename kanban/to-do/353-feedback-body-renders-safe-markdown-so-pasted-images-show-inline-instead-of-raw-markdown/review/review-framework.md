# Review framework

## Budget (by-diff)

- Lines changed: 781
- Effort: 2
- TCB hits: none
- Roster axes: general
- Turn cap: 120 (--max-turns; cursor uncapped)

# Review framework — task 353

**Date:** 2026-10-14
**Host task:** kanban/to-do/353-feedback-body-renders-safe-markdown-so-pasted-images-show-inline-instead-of-raw-markdown/
**Diff scope:** commit d492f2844 vs master (49261ae29)
**Plan / brief:** task.md Requirements — client-side safe markdown (Markdig, HTML off), link/image allow-lists, inline-attachment dedupe, shared FeedbackBody, tests
**Effort:** 2 (budget by-diff), roster axes: general
**Reviewer roster:** general (review oracle, claude-opus-5-5)
**Session IDs:** not reported

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`

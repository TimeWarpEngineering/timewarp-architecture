# Review framework

## Budget (by-diff)

- Lines changed: 813
- Effort: 3
- TCB hits: none
- Roster axes: general
- Turn cap: 200 (--max-turns; cursor uncapped)

# Review framework — task 326

**Date:** 2026-10-12
**Host task:** kanban/to-do/326-shared-textarea-component-that-fills-its-field-and-resizes-so-pages-cannot-repeat-the-302-narrow-textarea-bug/
**Diff scope:** branch task/326-shared-textarea-component-that-fills-its-field-and, commit 6cf19c84d vs 9e95be869
**Plan / brief:** shared `components/forms/TextArea.razor`, four call sites migrated, TWA0032 analyzer + opt-out attribute, Playwright sizing proof (see task.md Results)
**Effort:** 3 (by-diff budget); roster axes: general
**Reviewer roster:** general (Claude Sonnet subagent), orchestrator review oracle (Claude Opus 5.5)
**Session IDs:** general subagent a40fda73b7c3f58ac

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit. Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`

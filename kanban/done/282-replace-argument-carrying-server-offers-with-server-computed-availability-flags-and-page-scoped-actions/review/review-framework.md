# Review framework

## Budget (by-diff)

- Lines changed: 5724
- Effort: 3
- TCB hits: none
- Roster axes: general
- Turn cap: 200 (--max-turns; cursor uncapped)

# Review framework — task 282

**Date:** 2026-10-06
**Host task:** kanban/to-do/282-replace-argument-carrying-server-offers-with-server-computed-availability-flags-and-page-scoped-actions/
**Diff scope:** branch `task/282-replace-argument-carrying-server-offers-with-serve` vs `master` (commit 7f02904f9)
**Plan / brief:** task.md Change table + Remove list — replace server offers with typed availability flags (`CredentialSummary.CanRevoke`/`CanRename`, `Response.CanLinkMicrosoft365`), page-scoped actions, remove Ctrl-K context hook, retire TWE012–014 / TWA0029–0031
**Effort:** 3 (by-diff budget); roster axes: general
**Reviewer roster:** general
**Session IDs:** review oracle (claude-opus-5-5, headless ganda task work); general reviewer = Claude subagent

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`

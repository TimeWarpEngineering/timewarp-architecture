# Review framework — task 521

**Date:** 2026-11-25
**Host task:** kanban/in-progress/521-feedback-items-record-the-app-version-and-page-at-filing/
**Diff scope:** branch `task/521-feedback-items-record-the-app-version-and-page-at` vs `master` (product diff, 26 files, +1252/−18). Kanban land commits are out of scope. Untracked kitchen files under `review/` are this review.
**Plan / brief:** On filing, store the web-server assembly informational version (server-side, never from the client) and an optional app-relative page path (client sends path only; server validates). Both are fixed after filing, returned on the admin detail and admin list, shown on the admin feedback view, null on older rows. Filer views stay unchanged.
**Effort:** 3 (general only; by-diff budget)
**Reviewer roster:** general
**Session IDs:** implementer claude `756d9681-ca1f-44f6-9d98-a2cafeb99bd8`; reviewer grok `01a212de-df11-79f1-b3fb-ba1372af11da`

## Budget (by-diff)

- Lines changed: 1441
- Effort: 3
- TCB hits: none
- Roster axes: general
- Turn cap: 200 (--max-turns; cursor uncapped)

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims by reading the diff and the kitchen `## Results` and `### How to validate` record
- Do not re-run a full build, a full test run, an audit, or the hooks; those belong to the implementer's recorded results, the host audit node, and CI
- Exception: one targeted command may check one suspected bug; the finding names the command and the reason. A full build, full test run, or audit is never that command
- A missing or thin recorded result is a finding, not a reason to run the gates
- Prior rounds are immutable; new work goes in `round-(N+1)/`

## Goal check

The implementer restatement matches the original task. The task allows a query string or absolute URL to be stored as the path only or refused; the restatement chooses refuse, and the client strips the query and fragment before send so a filing from a page that has a query still succeeds. That is the allowed choice, not a misread.

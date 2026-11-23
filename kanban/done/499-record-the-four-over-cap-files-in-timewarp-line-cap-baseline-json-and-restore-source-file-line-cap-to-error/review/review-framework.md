# Review framework — task 499

**Date:** 2026-11-23
**Host task:** kanban/in-progress/499-record-the-four-over-cap-files-in-timewarp-line-cap-baseline-json-and-restore-source-file-line-cap-to-error/
**Diff scope:** branch `task/499-record-the-four-over-cap-files-in-timewarp-line` vs `master` (product commits `82c53766f` and `cde284eb2`; kanban lands `3307b46cb` and `776445c3f`). One commit behind master is `e8c60e044` (`land kanban 499`, task.md rename only).
**Plan / brief:** task 499. Record the five over-cap files in `.timewarp/line-cap-baseline.json`, restore `source-file-line-cap` to its default Error, and retire `documentation/developer/guides/analyzer-package-split.md` into the convention analyzers' `AnalyzerReleases.Unshipped.md` header. The 2026-11-23 update says the title's "four" is five, including the contracts RFC Markdown.
**Effort:** 2 (general only; host budget named no specialist axis)
**Reviewer roster:** general
**Session IDs:** implementer Claude `fd9188f9-bc65-410c-aae0-804c04e0a418`; reviewer Grok `01a20794-1b56-77a1-8cc7-849af3aa1485`

## Budget (by-diff)

- Lines changed: 376
- Effort: 2
- TCB hits: none
- Roster axes: general
- Turn cap: 120 (--max-turns; cursor uncapped)

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims by reading the diff and the kitchen `## Results` and `### How to validate` record
- Do not re-run a full build, a full test run, an audit, or the hooks; those belong to the implementer's recorded results, the host audit node, and CI
- Exception: one targeted command may check one suspected bug; the finding names the command and the reason. A full build, full test run, or audit is never that command
- A missing or thin recorded result is a finding, not a reason to run the gates
- Prior rounds are immutable; new work goes in `round-(N+1)/`

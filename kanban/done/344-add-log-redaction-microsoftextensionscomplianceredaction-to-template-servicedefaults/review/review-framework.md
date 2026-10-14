# Review framework

## Budget (by-diff)

- Lines changed: 381
- Effort: 2
- TCB hits: none
- Roster axes: general
- Turn cap: 120 (--max-turns; cursor uncapped)

# Review framework — task 344

**Date:** 2026-10-15
**Host task:** kanban/to-do/344-add-log-redaction-microsoftextensionscomplianceredaction-to-template-servicedefaults/
**Diff scope:** branch task/344-add-log-redaction-microsoftextensionscompliancered vs master (commit 6949fdd22)
**Plan / brief:** task.md — Compliance.Redaction taxonomy + EnableRedaction in service defaults, [LoggerMessage] classification on DevelopmentEmailSender, TWA0001 generated-partial fix, Nuru 3.0.0 boy-scout
**Effort:** 2 (Budget.ByDiff, 381 lines)
**Reviewer roster:** general
**Session IDs:** review oracle (claude, claude-opus-5-5, ganda task work), session not reported

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`

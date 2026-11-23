# Review framework

## Budget (by-diff)

- Lines changed: 507
- Effort: 2
- TCB hits: none
- Roster axes: general
- Turn cap: 120 (--max-turns; cursor uncapped)

# Review framework — task 489-004

**Date:** 2026-11-23
**Host task:** kanban/in-progress/489-004-convert-ai-page-assistant-name-and-byok-forms/
**Diff scope:** branch `task/489-004-convert-ai-page-assistant-name-and-byok-forms`, implementation commit 6da30734e (product paths), against the merge of `origin/feature/edit-forms-whole-object` (795812789)
**Plan / brief:** task.md `## Goal (implementer restatement)` and `## Description`. The AiPage assistant-name and xAI key-policy forms copy `Command.From(GetSiteSettings.Response)`, and the `Sync*` and `Synced*` shadow tracking is removed.
**Effort:** 2 (by-diff budget), roster axis general
**Reviewer roster:** general
**Session IDs:** review oracle Claude Opus 5.5 (claude-opus-5-5), headless ganda task work

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims by reading
  the diff and the kitchen `## Results` and `### How to validate` record
- Do not re-run a full build, a full test run, an audit, or the hooks; those belong to
  the implementer's recorded results, the host audit node, and CI
- Exception: one targeted command may check one suspected bug; the finding names the
  command and the reason. A full build, full test run, or audit is never that command
- A missing or thin recorded result is a finding, not a reason to run the gates
- Prior rounds are immutable; new work goes in `round-(N+1)/`

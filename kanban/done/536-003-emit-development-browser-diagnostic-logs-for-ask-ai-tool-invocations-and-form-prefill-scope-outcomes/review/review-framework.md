# Review framework

## Budget (by-diff)

- Lines changed: 1122
- Effort: 3
- TCB hits: none
- Roster axes: general
- Turn cap: 200 (--max-turns; cursor uncapped)

## Scope

**Date:** 2026-11-28
**Host task:** kanban/in-progress/536-003-emit-development-browser-diagnostic-logs-for-ask-ai-tool-invocations-and-form-prefill-scope-outcomes/
**Diff scope:** branch `task/536-003-emit-development-browser-diagnostic-logs-for-ask`, task commits `d178e9460..3f0fd472f` (336f1b70d feat, 8fd51d09a evidence, 3f0fd472f land). The branch merge-base with local `master` is older and also carries 536-001 and 536-002; those are out of scope.
**Plan / brief:** task.md Requirements 1–3 and `## Goal (implementer restatement)`.
**Effort:** 3
**Reviewer roster:** general
**Session IDs:** review oracle (claude, model claude-opus-5-5), 2026-11-28

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

## Round 2 scope

**Diff scope:** fix delta `3f0fd472f..f04ccf592` (commit f04ccf592, implement-fix-2). The round-1 IDs M1 through M6
carry forward in `round-2/merged.md`.
**Reviewer roster:** general (claude, model claude-opus-5-5), effort 3, 2026-11-28.

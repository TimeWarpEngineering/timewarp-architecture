# Review framework

## Budget (by-diff)

- Lines changed: 785
- Effort: 2
- TCB hits: none
- Roster axes: general
- Turn cap: 120 (--max-turns; cursor uncapped)

## Scope

**Date:** 2026-11-28
**Host task:** kanban/in-progress/536-002-enable-microsoft-extensions-ai-open-telemetry-gen-ai-spans-and-upstream-error-body-logging-on-xai-chat-upstream/
**Diff scope:** commit b2f480d13 (`feat: record GenAI spans and the xAI error body`) on branch
`task/536-002-enable-microsoft-extensions-ai-open-telemetry-gen`, i.e. `ff7bfef93..14f182b64`.
The 536-001 commits also ahead of local `master` already merged in PR #717 and are out of scope.
**Plan / brief:** task.md Requirements 1–4 and `## Goal (implementer restatement)`.
**Effort:** 2 (general only)
**Reviewer roster:** general
**Session IDs:** reviewer claude (claude-opus-5-5), headless review oracle under `ganda task work`

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

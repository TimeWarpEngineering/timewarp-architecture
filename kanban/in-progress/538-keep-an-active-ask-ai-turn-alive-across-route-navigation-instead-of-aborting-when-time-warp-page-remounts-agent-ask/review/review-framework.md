# Review framework

## Budget (by-diff)

- Lines changed: 1629
- Effort: 3
- TCB hits: none
- Roster axes: general
- Turn cap: 200 (--max-turns; cursor uncapped)

## Scope — task 538

**Date:** 2026-11-28
**Host task:** kanban/in-progress/538-keep-an-active-ask-ai-turn-alive-across-route-navigation-instead-of-aborting-when-time-warp-page-remounts-agent-ask/
**Diff scope:** branch `task/538-keep-an-active-ask-ai-turn-alive-across-route` vs `master` (merge base), 15 files
**Plan / brief:** task.md `## Requirements` and `## Goal (implementer restatement)`: move the live Ask agent out of `AgentAsk` into a circuit-scoped `AskAgentSession` so a `TimeWarpPage` remount does not cancel the turn; arrival-needs-approval handoff for off-page prefill tools.
**Effort:** 3 (by-diff budget); roster axis general only
**Reviewer roster:** general (review oracle, Claude Code, claude-opus-5-5)
**Session IDs:** not reported by the harness

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

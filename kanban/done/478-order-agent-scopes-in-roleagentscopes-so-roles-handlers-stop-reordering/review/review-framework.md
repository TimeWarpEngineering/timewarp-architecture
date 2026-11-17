# Review framework

## Budget (by-diff)

- Lines changed: 256
- Effort: 2
- TCB hits: none
- Roster axes: general
- Turn cap: 120 (--max-turns; cursor uncapped)

# Review framework — task 478

**Date:** 2026-11-05
**Host task:** kanban/in-progress/478-order-agent-scopes-in-roleagentscopes-so-roles-handlers-stop-reordering/
**Diff scope:** branch `task/478-order-agent-scopes-in-roleagentscopes-so-roles-han`, implementation commit 5e6bb6281 (product paths under `source/` and `tests/`); kanban landing commits not reviewed as product
**Plan / brief:** task.md Requirements and `## Goal (implementer restatement)`: move catalog ordering of role agent scopes into `Role.AgentScopes`; drop `AgentScopeOrder.OrderKnown` from GetRole, GetRoles, SetRoleAgentScopes
**Effort:** 2 (budget by diff); roster axes: general
**Reviewer roster:** general (review oracle, claude-opus-5-5)
**Session IDs:** review oracle under ganda task work (headless claude print mode)

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`

# Review framework

## Budget (by-diff)

- Lines changed: 328
- Effort: 2
- TCB hits: none
- Roster axes: general
- Turn cap: 120 (--max-turns; cursor uncapped)

# Review framework — task 070-005

**Date:** 2026-10-07
**Host task:** kanban/to-do/070-005-deploy-guidance-in-a-skill-for-compose-kubernetes-and-azure-targets/
**Diff scope:** branch task/070-005-deploy-guidance-in-a-skill-for-compose-kubernetes vs master (skills/tw-deploy/SKILL.md, AppHost Design region, template-smoke harness, AGENTS.md, task.md)
**Plan / brief:** task.md Requirements 1–3 and the decision A follow-through
**Effort:** 2 — general (subagent, fact-check against the repo) + plan_alignment (orchestrator)
**Reviewer roster:** general, plan_alignment
**Session IDs:** general subagent af0742154f2eab428; orchestrator = review oracle session

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`

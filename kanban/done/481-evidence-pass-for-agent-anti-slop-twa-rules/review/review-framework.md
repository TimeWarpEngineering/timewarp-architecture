# Review framework

## Budget (by-diff)

- Lines changed: 166
- Effort: 1
- TCB hits: none
- Roster axes: general
- Turn cap: 80 (--max-turns; cursor uncapped)

## Scope

- **Date:** 2026-11-06
- **Host task:** kanban/in-progress/481-evidence-pass-for-agent-anti-slop-twa-rules/
- **Diff scope:** branch `task/481-evidence-pass-for-agent-anti-slop-twa-rules` vs `master`
  (kanban-only; `task.md` Results evidence table)
- **Plan / brief:** research-only evidence pass; ranked list of recurring agent C# anti-patterns
  with citations, no analyzer code (`## Goal (implementer restatement)` in task.md)
- **Reviewer roster:** general (effort 2)
- **Session IDs:** review oracle, ganda task work headless (claude-opus-5-5)
- **Round 2 scope:** the fix delta `82a46d8fd..6c3fa0457` (`task.md` only). Effort 1, roster
  general, turn cap 80 (from the by-diff budget above).

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Re-verify falsifiable claims (citations, rule-coverage) against the repos
- Prior rounds are immutable; new work goes in `round-(N+1)/`

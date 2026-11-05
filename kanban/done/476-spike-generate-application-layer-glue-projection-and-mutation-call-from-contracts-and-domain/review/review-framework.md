# Review framework

## Budget (by-diff)

- Lines changed: 157
- Effort: 1
- TCB hits: none
- Roster axes: general
- Turn cap: 80 (--max-turns; cursor uncapped)

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`

## Round 2 scope

- **Date:** 2026-11-05
- **Diff scope:** the round-1 fix delta, `034e0ea22..f0a4acae4` (`skills/tw-aggregate-pattern/SKILL.md`,
  `proposal.md`, `task.md`)
- **Effort:** 1 (by-diff budget above), roster general
- **Session IDs:** review oracle (claude, claude-opus-5-5), ganda task-work review node, round 2

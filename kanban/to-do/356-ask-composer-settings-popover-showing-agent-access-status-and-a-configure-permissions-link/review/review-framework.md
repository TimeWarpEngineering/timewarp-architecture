# Review framework

## Budget (by-diff)

- Lines changed: 550
- Effort: 2
- TCB hits: none
- Roster axes: general
- Turn cap: 120 (--max-turns; cursor uncapped)

# Review framework — task 356

**Date:** 2026-10-14
**Host task:** kanban/to-do/356-ask-composer-settings-popover-showing-agent-access-status-and-a-configure-permissions-link/
**Diff scope:** branch task/356-ask-composer-settings-popover-showing-agent-access vs master (commit a21068bf5)
**Plan / brief:** AskSettingsMenu in the Ask composer — credential status (active/expired/absent), renew via New conversation, Configure permissions → Agent links; component + Playwright tests.
**Effort:** 2 (budget by-diff); roster axes: general
**Reviewer roster:** general
**Session IDs:** review oracle (claude-opus-5-5), session not reported

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`

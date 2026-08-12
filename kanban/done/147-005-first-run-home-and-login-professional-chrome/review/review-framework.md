# Review framework — task 147-005

**Date:** 2026-08-06
**Host task:** `kanban/in-progress/147-005-first-run-home-and-login-professional-chrome/`
**Diff scope:** commit `c4c90779` (`feat(web): first-run focused login + home auth strip (147-005)`) vs parent
**Plan / brief:** task.md Notes → Implementation plan; focused shell + Login rebuild + Home AuthorizeView + Try-it → TestPage + ChangePassword delete
**Effort:** 1 (general only)
**Reviewer roster:** general
**Session IDs:** implementer 019fd541-8433-7830-bf5e-7577d4fbc91a; plan 019fd53d-b410-7ed1-a82a-15fd8a03a461

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`
- Confirm locked design: no email/password, data-qa hooks, no TimeWarpPage chrome on login, Admin policy gated, ceremony not regressively restricted
